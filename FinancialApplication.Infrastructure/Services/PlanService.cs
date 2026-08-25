using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FinancialApplication.Application.DTOs.Subscription;
using FinancialApplication.Application.Interfaces;
using FinancialApplication.Domain.Domain.Entity;
using FinancialApplication.Domain.Domain.Enums;
using FinancialApplication.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FinancialApplication.Infrastructure.Services
{
    /// <summary>
    /// Implements IPlanService — admin CRUD operations for Plans, PlanFeature
    /// mapping, pricing updates, and full PlanAudit / PlanPriceHistory logging.
    /// </summary>
    public class PlanService : IPlanService
    {
        private readonly AppDbContext _context;
        private readonly IFeatureAccessResolver _featureAccessResolver;

        public PlanService(AppDbContext context, IFeatureAccessResolver featureAccessResolver)
        {
            _context = context;
            _featureAccessResolver = featureAccessResolver;
        }

        // ─────────────────────────────────────────────────────────────────────
        // READ
        // ─────────────────────────────────────────────────────────────────────

        public async Task<List<PlanDto>> GetAllPlansAsync(bool includeInactive = false)
        {
            var query = _context.Plans
                .Include(p => p.PlanFeatures)
                    .ThenInclude(pf => pf.Feature)
                .AsQueryable();

            if (!includeInactive)
                query = query.Where(p => p.IsActive);

            var plans = await query
                .OrderBy(p => p.SortOrder)
                .ToListAsync();

            return plans.Select(MapToDto).ToList();
        }

        public async Task<PlanDto?> GetPlanByIdAsync(Guid id)
        {
            var plan = await _context.Plans
                .Include(p => p.PlanFeatures)
                    .ThenInclude(pf => pf.Feature)
                .FirstOrDefaultAsync(p => p.Id == id);

            return plan == null ? null : MapToDto(plan);
        }

        // ─────────────────────────────────────────────────────────────────────
        // CREATE
        // ─────────────────────────────────────────────────────────────────────

        public async Task<PlanDto> CreatePlanAsync(CreatePlanRequest request)
        {
            // Enforce unique Name and Slug
            var duplicateName = await _context.Plans
                .AnyAsync(p => p.Name.ToLower() == request.Name.ToLower());
            if (duplicateName)
                throw new InvalidOperationException($"A plan named '{request.Name}' already exists.");

            var duplicateSlug = await _context.Plans
                .AnyAsync(p => p.Slug.ToLower() == request.Slug.ToLower());
            if (duplicateSlug)
                throw new InvalidOperationException($"A plan with slug '{request.Slug}' already exists.");

            // Validate AnnualPrice ≤ MonthlyPrice * 12
            if (request.AnnualPrice.HasValue && request.AnnualPrice > request.MonthlyPrice * 12)
                throw new InvalidOperationException("Annual price must not exceed 12× the monthly price.");

            var plan = new Plan
            {
                Name         = request.Name,
                Slug         = request.Slug.ToLower(),
                Description  = request.Description,
                MonthlyPrice = request.MonthlyPrice,
                AnnualPrice  = request.AnnualPrice,
                Currency     = request.Currency,
                SortOrder    = request.SortOrder,
                IsActive     = request.IsActive,
                IsDefault    = request.IsDefault,
                TrialDays    = request.TrialDays,
                MaxUsers     = request.MaxUsers,
                CreatedAt    = DateTime.UtcNow,
                UpdatedAt    = DateTime.UtcNow
            };

            _context.Plans.Add(plan);

            // Seed initial price history record
            _context.PlanPriceHistories.Add(new PlanPriceHistory
            {
                PlanId        = plan.Id,
                MonthlyPrice  = plan.MonthlyPrice,
                AnnualPrice   = plan.AnnualPrice,
                EffectiveFrom = DateTime.UtcNow,
                EffectiveTo   = null,
                ChangedBy     = Guid.Empty,
                CreatedAt     = DateTime.UtcNow
            });

            // Audit: Created
            _context.PlanAudits.Add(new PlanAudit
            {
                PlanId      = plan.Id,
                Action      = "Created",
                OldValues   = null,
                NewValues   = SerializePlan(plan),
                PerformedBy = Guid.Empty,
                CreatedAt   = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            // Return with features (empty on creation)
            return MapToDto(plan);
        }

        // ─────────────────────────────────────────────────────────────────────
        // UPDATE
        // ─────────────────────────────────────────────────────────────────────

        public async Task<PlanDto> UpdatePlanAsync(Guid id, UpdatePlanRequest request)
        {
            var plan = await _context.Plans
                .Include(p => p.PlanFeatures)
                    .ThenInclude(pf => pf.Feature)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (plan == null)
                throw new KeyNotFoundException($"Plan with ID {id} not found.");

            // Check uniqueness only if Name/Slug changed
            if (!plan.Name.Equals(request.Name, StringComparison.OrdinalIgnoreCase))
            {
                var dup = await _context.Plans
                    .AnyAsync(p => p.Id != id && p.Name.ToLower() == request.Name.ToLower());
                if (dup) throw new InvalidOperationException($"A plan named '{request.Name}' already exists.");
            }

            if (!plan.Slug.Equals(request.Slug, StringComparison.OrdinalIgnoreCase))
            {
                var dup = await _context.Plans
                    .AnyAsync(p => p.Id != id && p.Slug.ToLower() == request.Slug.ToLower());
                if (dup) throw new InvalidOperationException($"A plan with slug '{request.Slug}' already exists.");
            }

            var oldSnapshot = SerializePlan(plan);

            plan.Name      = request.Name;
            plan.Slug      = request.Slug.ToLower();
            plan.Description = request.Description;
            plan.Currency  = request.Currency;
            plan.SortOrder = request.SortOrder;
            plan.IsActive  = request.IsActive;
            plan.TrialDays = request.TrialDays;
            plan.MaxUsers  = request.MaxUsers;
            plan.UpdatedAt = DateTime.UtcNow;

            _context.PlanAudits.Add(new PlanAudit
            {
                PlanId      = plan.Id,
                Action      = "Updated",
                OldValues   = oldSnapshot,
                NewValues   = SerializePlan(plan),
                PerformedBy = Guid.Empty,
                CreatedAt   = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return MapToDto(plan);
        }

        
        public async Task<bool> DeletePlanAsync(Guid id)
        {
            var plan = await _context.Plans.FindAsync(id);
            if (plan == null)
                throw new KeyNotFoundException($"Plan with ID {id} not found.");

            // Reject if active subscribers exist
            var hasActiveSubscribers = await _context.UserSubscriptions
                .AnyAsync(us => us.PlanId == id &&
                               (us.Status == SubscriptionStatusEnum.Active ||
                                us.Status == SubscriptionStatusEnum.Trial));

            if (hasActiveSubscribers)
                throw new InvalidOperationException(
                    $"Cannot deactivate plan '{plan.Name}' — it has active subscribers. " +
                    "Migrate them to another plan first.");

            var oldSnapshot = SerializePlan(plan);
            plan.IsActive  = false;
            plan.UpdatedAt = DateTime.UtcNow;

            _context.PlanAudits.Add(new PlanAudit
            {
                PlanId      = plan.Id,
                Action      = "Disabled",
                OldValues   = oldSnapshot,
                NewValues   = SerializePlan(plan),
                PerformedBy = Guid.Empty,
                CreatedAt   = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return true;
        }

        // ─────────────────────────────────────────────────────────────────────
        // PLAN FEATURE MANAGEMENT
        // ─────────────────────────────────────────────────────────────────────

        public async Task<bool> AssignFeatureToPlanAsync(Guid planId, Guid featureId)
        {
            var plan = await _context.Plans.FindAsync(planId);
            if (plan == null)
                throw new KeyNotFoundException($"Plan with ID {planId} not found.");

            var feature = await _context.Features.FindAsync(featureId);
            if (feature == null)
                throw new KeyNotFoundException($"Feature with ID {featureId} not found.");

            if (!feature.IsActive)
                throw new InvalidOperationException(
                    $"Cannot assign inactive feature '{feature.FeatureKey}' to a plan.");

            // Duplicate guard (DB unique constraint is the final safety net)
            var alreadyAssigned = await _context.PlanFeatures
                .AnyAsync(pf => pf.PlanId == planId && pf.FeatureId == featureId);
            if (alreadyAssigned)
                throw new InvalidOperationException(
                    $"Feature '{feature.FeatureKey}' is already assigned to plan '{plan.Name}'.");

            _context.PlanFeatures.Add(new PlanFeature
            {
                PlanId    = planId,
                FeatureId = featureId,
                CreatedAt = DateTime.UtcNow
            });

            _context.PlanAudits.Add(new PlanAudit
            {
                PlanId      = planId,
                Action      = "FeaturesModified",
                OldValues   = null,
                NewValues   = JsonSerializer.Serialize(new { Added = feature.FeatureKey }),
                PerformedBy = Guid.Empty,
                CreatedAt   = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            // Invalidate cached features for all users subscribed to this plan
            _featureAccessResolver.InvalidatePlanCache(planId);

            return true;
        }

        public async Task<bool> RemoveFeatureFromPlanAsync(Guid planId, Guid featureId)
        {
            var planFeature = await _context.PlanFeatures
                .FirstOrDefaultAsync(pf => pf.PlanId == planId && pf.FeatureId == featureId);

            if (planFeature == null)
                throw new KeyNotFoundException(
                    $"Feature {featureId} is not assigned to plan {planId}.");

            var feature = await _context.Features.FindAsync(featureId);

            _context.PlanFeatures.Remove(planFeature);

            _context.PlanAudits.Add(new PlanAudit
            {
                PlanId      = planId,
                Action      = "FeaturesModified",
                OldValues   = JsonSerializer.Serialize(new { Removed = feature?.FeatureKey ?? featureId.ToString() }),
                NewValues   = null,
                PerformedBy = Guid.Empty,
                CreatedAt   = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            // Invalidate cached features for all users subscribed to this plan
            _featureAccessResolver.InvalidatePlanCache(planId);

            return true;
        }

        // ─────────────────────────────────────────────────────────────────────
        // PRICING UPDATE
        // ─────────────────────────────────────────────────────────────────────

        public async Task<bool> UpdatePricingAsync(Guid planId, UpdatePricingRequest request)
        {
            var plan = await _context.Plans.FindAsync(planId);
            if (plan == null)
                throw new KeyNotFoundException($"Plan with ID {planId} not found.");

            if (request.AnnualPrice.HasValue && request.AnnualPrice > request.MonthlyPrice * 12)
                throw new InvalidOperationException("Annual price must not exceed 12× the monthly price.");

            var now = DateTime.UtcNow;

            // Close out the previous price history record
            var currentHistory = await _context.PlanPriceHistories
                .Where(ph => ph.PlanId == planId && ph.EffectiveTo == null)
                .FirstOrDefaultAsync();

            if (currentHistory != null)
                currentHistory.EffectiveTo = now;

            var oldSnapshot = SerializePlan(plan);

            plan.MonthlyPrice = request.MonthlyPrice;
            plan.AnnualPrice  = request.AnnualPrice;
            plan.UpdatedAt    = now;

            // Create new price history record
            _context.PlanPriceHistories.Add(new PlanPriceHistory
            {
                PlanId        = planId,
                MonthlyPrice  = request.MonthlyPrice,
                AnnualPrice   = request.AnnualPrice,
                EffectiveFrom = now,
                EffectiveTo   = null,
                ChangedBy     = Guid.Empty,
                CreatedAt     = now
            });

            _context.PlanAudits.Add(new PlanAudit
            {
                PlanId      = planId,
                Action      = "PriceChanged",
                OldValues   = oldSnapshot,
                NewValues   = SerializePlan(plan),
                PerformedBy = Guid.Empty,
                CreatedAt   = now
            });

            await _context.SaveChangesAsync();
            return true;
        }

        // ─────────────────────────────────────────────────────────────────────
        // PRIVATE HELPERS
        // ─────────────────────────────────────────────────────────────────────

        private static PlanDto MapToDto(Plan p) => new PlanDto
        {
            Id           = p.Id,
            Name         = p.Name,
            Slug         = p.Slug,
            Description  = p.Description,
            MonthlyPrice = p.MonthlyPrice,
            AnnualPrice  = p.AnnualPrice,
            Currency     = p.Currency,
            SortOrder    = p.SortOrder,
            IsActive     = p.IsActive,
            IsDefault    = p.IsDefault,
            TrialDays    = p.TrialDays,
            MaxUsers     = p.MaxUsers,
            CreatedAt    = p.CreatedAt,
            UpdatedAt    = p.UpdatedAt,
            Features     = p.PlanFeatures
                            .Where(pf => pf.Feature != null)
                            .Select(pf => new FeatureSummaryDto
                            {
                                Id          = pf.Feature.Id,
                                FeatureKey  = pf.Feature.FeatureKey,
                                DisplayName = pf.Feature.DisplayName
                            })
                            .ToList()
        };

        private static string SerializePlan(Plan p) =>
            JsonSerializer.Serialize(new
            {
                p.Name,
                p.Slug,
                p.Description,
                p.MonthlyPrice,
                p.AnnualPrice,
                p.Currency,
                p.SortOrder,
                p.IsActive,
                p.IsDefault,
                p.TrialDays,
                p.MaxUsers
            });
    }
}
