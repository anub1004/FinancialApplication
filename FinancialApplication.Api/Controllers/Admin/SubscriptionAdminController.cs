using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using FinancialApplication.Application.DTOs.Subscription;
using FinancialApplication.Application.Interfaces;
using FinancialApplication.Domain.Domain.Enums;
using FinancialApplication.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinancialApplication.Api.Controllers.Admin
{
    /// <summary>
    /// Admin-only controller providing a read/management overview of all user subscriptions.
    /// Route: /api/admin/subscriptions
    ///
    /// Lifecycle operations (subscribe, upgrade, cancel etc.) live in the user-facing
    /// SubscriptionController introduced in Phase 7.
    /// </summary>
    [ApiController]
    [Route("api/admin/subscriptions")]
    [Authorize(Policy = "AdminOnly")]
    public class SubscriptionAdminController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IFeatureAccessResolver _featureAccessResolver;

        public SubscriptionAdminController(
            AppDbContext context,
            IFeatureAccessResolver featureAccessResolver)
        {
            _context                = context;
            _featureAccessResolver  = featureAccessResolver;
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET /api/admin/subscriptions
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns a paginated list of all user subscriptions with optional filters.
        /// Query params: status (enum name, e.g. "Active"), planId (Guid), page (int), pageSize (int).
        /// </summary>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAllSubscriptions(
            [FromQuery] string? status    = null,
            [FromQuery] Guid?   planId    = null,
            [FromQuery] int     page      = 1,
            [FromQuery] int     pageSize  = 20)
        {
            if (page < 1) page = 1;
            if (pageSize is < 1 or > 100) pageSize = 20;

            var query = _context.UserSubscriptions
                .Include(us => us.Plan)
                .AsQueryable();

            // Filter by status
            if (!string.IsNullOrWhiteSpace(status) &&
                Enum.TryParse<SubscriptionStatusEnum>(status, ignoreCase: true, out var parsedStatus))
            {
                query = query.Where(us => us.Status == parsedStatus);
            }

            // Filter by plan
            if (planId.HasValue)
                query = query.Where(us => us.PlanId == planId.Value);

            var total = await query.CountAsync();

            var items = await query
                .OrderByDescending(us => us.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(us => new
                {
                    us.Id,
                    us.UserId,
                    us.PlanId,
                    PlanName      = us.Plan.Name,
                    PlanSlug      = us.Plan.Slug,
                    Status        = us.Status.ToString(),
                    BillingCycle  = us.BillingCycle.ToString(),
                    us.StartDate,
                    us.EndDate,
                    us.TrialEndDate,
                    us.NextRenewalDate,
                    us.CancelledAt,
                    us.CancelReason,
                    us.AutoRenew,
                    us.CreatedAt,
                    us.UpdatedAt
                })
                .ToListAsync();

            return Ok(new
            {
                total,
                page,
                pageSize,
                totalPages  = (int)Math.Ceiling((double)total / pageSize),
                items
            });
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET /api/admin/subscriptions/stats
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns aggregate statistics for admin dashboard:
        ///   - totalActive, totalTrial, totalExpired, totalCancelled
        ///   - subscriptionsByPlan (planName, count)
        ///   - recentSubscriptions (last 30 days count)
        /// </summary>
        [HttpGet("stats")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetStats()
        {
            var now = DateTime.UtcNow;

            var counts = await _context.UserSubscriptions
                .GroupBy(us => us.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            var byPlan = await _context.UserSubscriptions
                .Include(us => us.Plan)
                .Where(us => us.Status == SubscriptionStatusEnum.Active ||
                             us.Status == SubscriptionStatusEnum.Trial)
                .GroupBy(us => new { us.PlanId, us.Plan.Name })
                .Select(g => new { planName = g.Key.Name, activeCount = g.Count() })
                .ToListAsync();

            var recentCount = await _context.UserSubscriptions
                .CountAsync(us => us.CreatedAt >= now.AddDays(-30));

            int GetCount(SubscriptionStatusEnum s) =>
                counts.FirstOrDefault(c => c.Status == s)?.Count ?? 0;

            return Ok(new
            {
                totalActive       = GetCount(SubscriptionStatusEnum.Active),
                totalTrial        = GetCount(SubscriptionStatusEnum.Trial),
                totalExpired      = GetCount(SubscriptionStatusEnum.Expired),
                totalCancelled    = GetCount(SubscriptionStatusEnum.Cancelled),
                totalPastDue      = GetCount(SubscriptionStatusEnum.PastDue),
                totalSuspended    = GetCount(SubscriptionStatusEnum.Suspended),
                subscriptionsByPlan    = byPlan,
                recentSubscriptions30d = recentCount,
                asOf              = now
            });
        }

        // ─────────────────────────────────────────────────────────────────────
        // PATCH /api/admin/subscriptions/{id}/status
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Allows an admin to manually override a subscription's status.
        /// Also invalidates the affected user's feature cache.
        /// </summary>
        [HttpPatch("{id:guid}/status")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ChangeStatus(Guid id, [FromBody] ChangeSubscriptionStatusRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var subscription = await _context.UserSubscriptions.FindAsync(id);
            if (subscription == null)
                return NotFound(new { error = $"Subscription with ID {id} not found." });

            if (!Enum.TryParse<SubscriptionStatusEnum>(request.Status, ignoreCase: true, out var newStatus))
                return BadRequest(new
                {
                    error          = $"Invalid status value '{request.Status}'.",
                    allowedValues  = Enum.GetNames(typeof(SubscriptionStatusEnum))
                });

            var previousStatus  = subscription.Status;
            subscription.Status = newStatus;
            subscription.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Invalidate this user's cached feature set since their status changed
            _featureAccessResolver.InvalidateUserCache(subscription.UserId);

            return Ok(new
            {
                id,
                userId          = subscription.UserId,
                previousStatus  = previousStatus.ToString(),
                newStatus       = newStatus.ToString(),
                message         = "Subscription status updated successfully."
            });
        }
    }

    /// <summary>Request body for admin status override.</summary>
    public class ChangeSubscriptionStatusRequest
    {
        /// <summary>
        /// Target status name (case-insensitive).
        /// Allowed: Active, Trial, Expired, Cancelled, PastDue, Suspended
        /// </summary>
        [Required]
        public string Status { get; set; } = string.Empty;
    }
}
