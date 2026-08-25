using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FinancialApplication.Application.DTOs.Subscription;
using FinancialApplication.Application.Interfaces;
using FinancialApplication.Domain.Domain.Entity;
using FinancialApplication.Domain.Domain.Enums;
using FinancialApplication.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FinancialApplication.Infrastructure.Services
{
    /// <summary>
    /// Implements ISubscriptionService — full subscription lifecycle management.
    /// 
    /// All write operations:
    ///   1. Run inside an EF Core transaction
    ///   2. Log a SubscriptionHistory entry
    ///   3. Invalidate the user's feature cache
    /// </summary>
    public class SubscriptionService : ISubscriptionService
    {
        private readonly AppDbContext _context;
        private readonly IFeatureAccessResolver _featureAccessResolver;
        private readonly IPaymentGateway _paymentGateway;
        private readonly ILogger<SubscriptionService> _logger;

        public SubscriptionService(
            AppDbContext context,
            IFeatureAccessResolver featureAccessResolver,
            IPaymentGateway paymentGateway,
            ILogger<SubscriptionService> logger)
        {
            _context                = context;
            _featureAccessResolver  = featureAccessResolver;
            _paymentGateway         = paymentGateway;
            _logger                 = logger;
        }

        // ─────────────────────────────────────────────────────────────────────
        // READ — Current Subscription
        // ─────────────────────────────────────────────────────────────────────

        public async Task<UserSubscriptionDto?> GetCurrentSubscriptionAsync(Guid userId)
        {
            var now = DateTime.UtcNow;
            var sub = await _context.UserSubscriptions
                .Include(us => us.Plan)
                .Where(us => us.UserId == userId &&
                            (us.Status == SubscriptionStatusEnum.Active ||
                             us.Status == SubscriptionStatusEnum.Trial ||
                             (us.Status == SubscriptionStatusEnum.Cancelled && us.EndDate > now)))
                .OrderByDescending(us => us.CreatedAt)
                .FirstOrDefaultAsync();

            return sub == null ? null : MapToDto(sub);
        }

        // ─────────────────────────────────────────────────────────────────────
        // READ — User Features (delegates to FeatureAccessResolver)
        // ─────────────────────────────────────────────────────────────────────

        public async Task<List<string>> GetUserFeaturesAsync(Guid userId)
        {
            var features = await _featureAccessResolver.GetUserFeaturesAsync(userId);
            return features.ToList();
        }

        // ─────────────────────────────────────────────────────────────────────
        // SUBSCRIBE — Create a new subscription
        // ─────────────────────────────────────────────────────────────────────

        public async Task<UserSubscriptionDto> SubscribeAsync(Guid userId, SubscribeRequest request)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Validate: user must not have an active paid subscription
                var existing = await _context.UserSubscriptions
                    .Include(us => us.Plan)
                    .Where(us => us.UserId == userId &&
                                (us.Status == SubscriptionStatusEnum.Active ||
                                 us.Status == SubscriptionStatusEnum.Trial))
                    .FirstOrDefaultAsync();

                var targetPlan = await _context.Plans
                    .FirstOrDefaultAsync(p => p.Id == request.PlanId && p.IsActive);

                if (targetPlan == null)
                    throw new KeyNotFoundException($"Plan with ID {request.PlanId} not found or inactive.");

                // If user has an existing active subscription on a non-Free plan, reject
                if (existing != null && !existing.Plan.IsDefault)
                    throw new InvalidOperationException(
                        "You already have an active subscription. Use the upgrade endpoint to change plans.");

                var now = DateTime.UtcNow;

                // If user is on the free/default plan, expire it before creating the new one
                if (existing != null && existing.Plan.IsDefault)
                {
                    existing.Status    = SubscriptionStatusEnum.Expired;
                    existing.EndDate   = now;
                    existing.UpdatedAt = now;
                }

                var subscription = new UserSubscription
                {
                    UserId       = userId,
                    PlanId       = targetPlan.Id,
                    Status       = targetPlan.TrialDays > 0
                                        ? SubscriptionStatusEnum.Trial
                                        : SubscriptionStatusEnum.Active,
                    BillingCycle = request.BillingCycle,
                    StartDate    = now,
                    EndDate      = CalculateEndDate(now, request.BillingCycle),
                    TrialEndDate = targetPlan.TrialDays > 0
                                        ? now.AddDays(targetPlan.TrialDays)
                                        : null,
                    NextRenewalDate = request.BillingCycle != BillingCycleEnum.Lifetime
                                        ? CalculateEndDate(now, request.BillingCycle)
                                        : null,
                    AutoRenew    = request.BillingCycle != BillingCycleEnum.Lifetime,
                    CreatedAt    = now,
                    UpdatedAt    = now
                };

                _context.UserSubscriptions.Add(subscription);

                // Log history
                _context.SubscriptionHistories.Add(new SubscriptionHistory
                {
                    UserId         = userId,
                    SubscriptionId = subscription.Id,
                    Action         = SubscriptionActionEnum.Created,
                    FromPlanId     = existing?.PlanId,
                    ToPlanId       = targetPlan.Id,
                    Notes          = $"Subscribed to {targetPlan.Name} ({request.BillingCycle})",
                    PerformedBy    = "User",
                    CreatedAt      = now
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Invalidate cache so features reflect immediately
                _featureAccessResolver.InvalidateUserCache(userId);

                _logger.LogInformation(
                    "User {UserId} subscribed to plan {PlanName} ({BillingCycle})",
                    userId, targetPlan.Name, request.BillingCycle);

                // Reload with Plan navigation for DTO mapping
                await _context.Entry(subscription).Reference(s => s.Plan).LoadAsync();
                return MapToDto(subscription);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // UPGRADE — Immediate plan change to a higher plan
        // ─────────────────────────────────────────────────────────────────────

        public async Task<UserSubscriptionDto> UpgradeAsync(Guid userId, UpgradeRequest request)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var current = await GetActiveSubscriptionOrThrow(userId);
                var currentPlan = await _context.Plans.FindAsync(current.PlanId)
                    ?? throw new InvalidOperationException("Current plan not found.");

                var targetPlan = await _context.Plans
                    .FirstOrDefaultAsync(p => p.Id == request.TargetPlanId && p.IsActive)
                    ?? throw new KeyNotFoundException($"Target plan with ID {request.TargetPlanId} not found or inactive.");

                // Validate: target must be higher than current (by SortOrder)
                if (targetPlan.SortOrder <= currentPlan.SortOrder)
                    throw new InvalidOperationException(
                        $"Cannot upgrade from '{currentPlan.Name}' (order {currentPlan.SortOrder}) " +
                        $"to '{targetPlan.Name}' (order {targetPlan.SortOrder}). " +
                        "Target plan must be higher. Use the downgrade endpoint instead.");

                var now          = DateTime.UtcNow;
                var fromPlanId   = current.PlanId;

                // ── Calculate upgrade price ──────────────────────────────────
                var upgradeAmount = request.BillingCycle == BillingCycleEnum.Annual
                    ? targetPlan.AnnualPrice ?? (targetPlan.MonthlyPrice * 12)
                    : targetPlan.MonthlyPrice;

                // ── Process payment via gateway ─────────────────────────────
                if (upgradeAmount > 0)
                {
                    var paymentResult = await _paymentGateway.ProcessPaymentAsync(
                        new Application.DTOs.Subscription.PaymentRequest
                        {
                            UserId         = userId,
                            SubscriptionId = current.Id,
                            Amount         = upgradeAmount,
                            Currency       = targetPlan.Currency,
                            PaymentMethod  = request.PaymentMethod,
                            Description    = $"Upgrade from {currentPlan.Name} to {targetPlan.Name}"
                        });

                    if (!paymentResult.Success)
                        throw new InvalidOperationException(
                            $"Payment failed: {paymentResult.ErrorMessage}");

                    // Record the payment
                    var payment = new Payment
                    {
                        UserId          = userId,
                        SubscriptionId  = current.Id,
                        Amount          = upgradeAmount,
                        Currency        = targetPlan.Currency,
                        Status          = Domain.Domain.Enums.PaymentStatusEnum.Completed,
                        PaymentMethod   = request.PaymentMethod,
                        TransactionRef  = paymentResult.TransactionRef,
                        GatewayResponse = paymentResult.GatewayResponse,
                        PaidAt          = now,
                        CreatedAt       = now
                    };
                    _context.Payments.Add(payment);
                }

                // ── Update the subscription in place ────────────────────────
                current.PlanId          = targetPlan.Id;
                current.BillingCycle    = request.BillingCycle;
                current.Status          = SubscriptionStatusEnum.Active;
                current.StartDate       = now;
                current.EndDate         = CalculateEndDate(now, request.BillingCycle);
                current.NextRenewalDate = request.BillingCycle != BillingCycleEnum.Lifetime
                                            ? CalculateEndDate(now, request.BillingCycle)
                                            : null;
                current.AutoRenew       = request.BillingCycle != BillingCycleEnum.Lifetime;
                current.ScheduledPlanId = null;   // clear any scheduled downgrade
                current.TrialEndDate    = null;    // no trial on upgrade
                current.UpdatedAt       = now;

                _context.SubscriptionHistories.Add(new SubscriptionHistory
                {
                    UserId         = userId,
                    SubscriptionId = current.Id,
                    Action         = SubscriptionActionEnum.Upgraded,
                    FromPlanId     = fromPlanId,
                    ToPlanId       = targetPlan.Id,
                    Notes          = $"Upgraded from {currentPlan.Name} to {targetPlan.Name}",
                    PerformedBy    = "User",
                    CreatedAt      = now
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _featureAccessResolver.InvalidateUserCache(userId);

                _logger.LogInformation(
                    "User {UserId} upgraded from {OldPlan} to {NewPlan}",
                    userId, currentPlan.Name, targetPlan.Name);

                await _context.Entry(current).Reference(s => s.Plan).LoadAsync();
                return MapToDto(current);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // DOWNGRADE — Schedule plan change to a lower plan at end of period
        // ─────────────────────────────────────────────────────────────────────

        public async Task<UserSubscriptionDto> DowngradeAsync(Guid userId, DowngradeRequest request)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var current = await GetActiveSubscriptionOrThrow(userId);
                var currentPlan = await _context.Plans.FindAsync(current.PlanId)
                    ?? throw new InvalidOperationException("Current plan not found.");

                var targetPlan = await _context.Plans
                    .FirstOrDefaultAsync(p => p.Id == request.TargetPlanId && p.IsActive)
                    ?? throw new KeyNotFoundException($"Target plan with ID {request.TargetPlanId} not found or inactive.");

                // Note: No block on highest-tier plan — the SortOrder check below handles it.
                // Users can downgrade from any plan to a lower-tier plan.

                // Validate: target must be lower than current (by SortOrder)
                if (targetPlan.SortOrder >= currentPlan.SortOrder)
                    throw new InvalidOperationException(
                        $"Cannot downgrade from '{currentPlan.Name}' (order {currentPlan.SortOrder}) " +
                        $"to '{targetPlan.Name}' (order {targetPlan.SortOrder}). " +
                        "Target plan must be lower. Use the upgrade endpoint instead.");

                var now = DateTime.UtcNow;

                // Schedule the downgrade for end of current billing period
                current.ScheduledPlanId = targetPlan.Id;
                current.UpdatedAt       = now;

                _context.SubscriptionHistories.Add(new SubscriptionHistory
                {
                    UserId         = userId,
                    SubscriptionId = current.Id,
                    Action         = SubscriptionActionEnum.Downgraded,
                    FromPlanId     = current.PlanId,
                    ToPlanId       = targetPlan.Id,
                    Notes          = $"Downgrade scheduled from {currentPlan.Name} to {targetPlan.Name} " +
                                     $"(effective {current.EndDate:yyyy-MM-dd})",
                    PerformedBy    = "User",
                    CreatedAt      = now
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation(
                    "User {UserId} scheduled downgrade from {OldPlan} to {NewPlan} at {EffectiveDate}",
                    userId, currentPlan.Name, targetPlan.Name, current.EndDate);

                await _context.Entry(current).Reference(s => s.Plan).LoadAsync();
                return MapToDto(current);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // CANCEL — Mark subscription as cancelled; user keeps access until EndDate
        // ─────────────────────────────────────────────────────────────────────

        public async Task<bool> CancelAsync(Guid userId, CancelRequest request)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var current = await GetActiveSubscriptionOrThrow(userId);

                if (current.Status == SubscriptionStatusEnum.Cancelled)
                    throw new InvalidOperationException("This subscription is already cancelled.");

                var now = DateTime.UtcNow;

                current.Status      = SubscriptionStatusEnum.Cancelled;
                current.CancelledAt = now;
                current.AutoRenew   = false;
                current.CancelReason = request.CancelReason;
                current.ScheduledPlanId = null;  // clear any scheduled downgrade
                current.UpdatedAt   = now;

                _context.SubscriptionHistories.Add(new SubscriptionHistory
                {
                    UserId         = userId,
                    SubscriptionId = current.Id,
                    Action         = SubscriptionActionEnum.Cancelled,
                    FromPlanId     = current.PlanId,
                    ToPlanId       = null,
                    Notes          = string.IsNullOrWhiteSpace(request.CancelReason)
                                        ? "Subscription cancelled by user."
                                        : $"Cancelled: {request.CancelReason}",
                    PerformedBy    = "User",
                    CreatedAt      = now
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Note: we do NOT invalidate cache immediately — user keeps access until EndDate.
                // A background job (future phase) will expire subscriptions past EndDate and invalidate then.
                // However, if EndDate has already passed, invalidate now.
                if (current.EndDate <= now)
                    _featureAccessResolver.InvalidateUserCache(userId);

                _logger.LogInformation("User {UserId} cancelled subscription {SubId}", userId, current.Id);

                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // REACTIVATE — Create a new subscription period from today
        // ─────────────────────────────────────────────────────────────────────

        public async Task<UserSubscriptionDto> ReactivateAsync(Guid userId)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Find the most recent cancelled or expired subscription
                var previous = await _context.UserSubscriptions
                    .Include(us => us.Plan)
                    .Where(us => us.UserId == userId &&
                                (us.Status == SubscriptionStatusEnum.Cancelled ||
                                 us.Status == SubscriptionStatusEnum.Expired))
                    .OrderByDescending(us => us.UpdatedAt)
                    .FirstOrDefaultAsync();

                if (previous == null)
                    throw new InvalidOperationException(
                        "No cancelled or expired subscription found to reactivate.");

                // Check no active subscription already exists
                var hasActive = await _context.UserSubscriptions
                    .AnyAsync(us => us.UserId == userId &&
                                   (us.Status == SubscriptionStatusEnum.Active ||
                                    us.Status == SubscriptionStatusEnum.Trial));

                if (hasActive)
                    throw new InvalidOperationException(
                        "Cannot reactivate — you already have an active subscription.");

                var now = DateTime.UtcNow;

                // Reactivate on the same plan and billing cycle
                var subscription = new UserSubscription
                {
                    UserId          = userId,
                    PlanId          = previous.PlanId,
                    Status          = SubscriptionStatusEnum.Active,
                    BillingCycle    = previous.BillingCycle,
                    StartDate       = now,
                    EndDate         = CalculateEndDate(now, previous.BillingCycle),
                    NextRenewalDate = previous.BillingCycle != BillingCycleEnum.Lifetime
                                        ? CalculateEndDate(now, previous.BillingCycle)
                                        : null,
                    AutoRenew       = previous.BillingCycle != BillingCycleEnum.Lifetime,
                    CreatedAt       = now,
                    UpdatedAt       = now
                };

                _context.UserSubscriptions.Add(subscription);

                _context.SubscriptionHistories.Add(new SubscriptionHistory
                {
                    UserId         = userId,
                    SubscriptionId = subscription.Id,
                    Action         = SubscriptionActionEnum.Reactivated,
                    FromPlanId     = previous.PlanId,
                    ToPlanId       = previous.PlanId,
                    Notes          = $"Reactivated subscription on {previous.Plan.Name}",
                    PerformedBy    = "User",
                    CreatedAt      = now
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _featureAccessResolver.InvalidateUserCache(userId);

                _logger.LogInformation(
                    "User {UserId} reactivated subscription on plan {PlanName}",
                    userId, previous.Plan.Name);

                await _context.Entry(subscription).Reference(s => s.Plan).LoadAsync();
                return MapToDto(subscription);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // READ — Subscription History
        // ─────────────────────────────────────────────────────────────────────

        public async Task<List<SubscriptionHistoryDto>> GetHistoryAsync(Guid userId)
        {
            var history = await _context.SubscriptionHistories
                .Where(h => h.UserId == userId)
                .OrderByDescending(h => h.CreatedAt)
                .Take(2)
                .ToListAsync();

            // Pre-load plan names for FromPlanId / ToPlanId
            var planIds = history
                .SelectMany(h => new[] { h.FromPlanId, h.ToPlanId })
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .Distinct()
                .ToList();

            var planNames = await _context.Plans
                .Where(p => planIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.Name);

            return history.Select(h => new SubscriptionHistoryDto
            {
                Id             = h.Id,
                UserId         = h.UserId,
                SubscriptionId = h.SubscriptionId,
                Action         = h.Action,
                FromPlanId     = h.FromPlanId,
                FromPlanName   = h.FromPlanId.HasValue && planNames.ContainsKey(h.FromPlanId.Value)
                                    ? planNames[h.FromPlanId.Value]
                                    : null,
                ToPlanId       = h.ToPlanId,
                ToPlanName     = h.ToPlanId.HasValue && planNames.ContainsKey(h.ToPlanId.Value)
                                    ? planNames[h.ToPlanId.Value]
                                    : null,
                Notes          = h.Notes,
                PerformedBy    = h.PerformedBy,
                CreatedAt      = h.CreatedAt
            }).ToList();
        }

        // ─────────────────────────────────────────────────────────────────────
        // PRIVATE HELPERS
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the user's active/trial subscription, or throws if none exists.
        /// </summary>
        private async Task<UserSubscription> GetActiveSubscriptionOrThrow(Guid userId)
        {
            var sub = await _context.UserSubscriptions
                .Where(us => us.UserId == userId &&
                            (us.Status == SubscriptionStatusEnum.Active ||
                             us.Status == SubscriptionStatusEnum.Trial))
                .FirstOrDefaultAsync();

            return sub ?? throw new InvalidOperationException(
                "No active subscription found. Subscribe to a plan first.");
        }

        /// <summary>
        /// Calculates the subscription end date based on billing cycle.
        /// </summary>
        private static DateTime CalculateEndDate(DateTime startDate, BillingCycleEnum billingCycle)
        {
            return billingCycle switch
            {
                BillingCycleEnum.Monthly  => startDate.AddMonths(1),
                BillingCycleEnum.Annual   => startDate.AddYears(1),
                BillingCycleEnum.Lifetime => new DateTime(2099, 12, 31, 23, 59, 59, DateTimeKind.Utc),
                _                         => startDate.AddMonths(1) // fallback
            };
        }

        // ─────────────────────────────────────────────────────────────────────
        // CREATE SUBSCRIPTION FOR NEW USER (Signup Flow)
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Creates an initial subscription for a newly registered user.
        /// If selectedPlanId is null, assigns the default (free) plan.
        /// No payment is required — this is for signup plan selection only.
        /// </summary>
        public async Task<UserSubscriptionDto> CreateSubscriptionForNewUserAsync(Guid userId, Guid? selectedPlanId)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Check if user already has any active subscription
                var existing = await _context.UserSubscriptions
                    .AnyAsync(us => us.UserId == userId &&
                                   (us.Status == SubscriptionStatusEnum.Active ||
                                    us.Status == SubscriptionStatusEnum.Trial));

                if (existing)
                {
                    _logger.LogWarning("User {UserId} already has an active subscription, skipping signup subscription creation", userId);
                    await transaction.RollbackAsync();
                    var existingSub = await GetCurrentSubscriptionAsync(userId);
                    return existingSub!;
                }

                // Resolve the plan: selected or default
                Domain.Domain.Entity.Plan? plan = null;
                if (selectedPlanId.HasValue)
                {
                    plan = await _context.Plans
                        .FirstOrDefaultAsync(p => p.Id == selectedPlanId.Value && p.IsActive);
                }

                // Fallback to default plan if selected plan not found or not provided
                plan ??= await _context.Plans
                    .FirstOrDefaultAsync(p => p.IsDefault && p.IsActive);

                if (plan == null)
                {
                    _logger.LogWarning("No active plan found for signup subscription (selectedPlanId={SelectedPlanId})", selectedPlanId);
                    await transaction.RollbackAsync();
                    throw new InvalidOperationException("No plan available for subscription.");
                }

                var now = DateTime.UtcNow;
                var billingCycle = BillingCycleEnum.Monthly;

                var subscription = new UserSubscription
                {
                    UserId       = userId,
                    PlanId       = plan.Id,
                    Status       = plan.TrialDays > 0
                                        ? SubscriptionStatusEnum.Trial
                                        : SubscriptionStatusEnum.Active,
                    BillingCycle = billingCycle,
                    StartDate    = now,
                    EndDate      = CalculateEndDate(now, billingCycle),
                    TrialEndDate = plan.TrialDays > 0
                                        ? now.AddDays(plan.TrialDays)
                                        : null,
                    NextRenewalDate = CalculateEndDate(now, billingCycle),
                    AutoRenew    = true,
                    CreatedAt    = now,
                    UpdatedAt    = now
                };

                _context.UserSubscriptions.Add(subscription);

                // Log history
                _context.SubscriptionHistories.Add(new SubscriptionHistory
                {
                    UserId         = userId,
                    SubscriptionId = subscription.Id,
                    Action         = SubscriptionActionEnum.Created,
                    FromPlanId     = null,
                    ToPlanId       = plan.Id,
                    Notes          = $"Signup subscription: {plan.Name} ({billingCycle})",
                    PerformedBy    = "System",
                    CreatedAt      = now
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _featureAccessResolver.InvalidateUserCache(userId);

                _logger.LogInformation(
                    "Created signup subscription for user {UserId} on plan {PlanName}",
                    userId, plan.Name);

                await _context.Entry(subscription).Reference(s => s.Plan).LoadAsync();
                return MapToDto(subscription);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // PRIVATE HELPERS
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Maps a UserSubscription entity to its DTO.
        /// Assumes Plan navigation is loaded.
        /// </summary>
        private static UserSubscriptionDto MapToDto(UserSubscription sub) => new()
        {
            Id              = sub.Id,
            UserId          = sub.UserId,
            PlanId          = sub.PlanId,
            PlanName        = sub.Plan?.Name ?? string.Empty,
            PlanSlug        = sub.Plan?.Slug ?? string.Empty,
            Status          = sub.Status,
            BillingCycle    = sub.BillingCycle,
            StartDate       = sub.StartDate,
            EndDate         = sub.EndDate,
            TrialEndDate    = sub.TrialEndDate,
            NextRenewalDate = sub.NextRenewalDate,
            CancelledAt     = sub.CancelledAt,
            CancelReason    = sub.CancelReason,
            AutoRenew       = sub.AutoRenew,
            ScheduledPlanId = sub.ScheduledPlanId,
            CreatedAt       = sub.CreatedAt,
            UpdatedAt       = sub.UpdatedAt
        };
    }
}
