using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FinancialApplication.Application.DTOs.Subscription;
using FinancialApplication.Application.Interfaces;
using FinancialApplication.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinancialApplication.Api.Controllers
{
    /// <summary>
    /// User-facing subscription endpoints.
    /// Route: /api/subscription
    ///
    /// All endpoints require authentication (except GET /plans which could be public).
    /// The UserId is extracted from the JWT ClaimTypes.NameIdentifier claim.
    /// </summary>
    [ApiController]
    [Route("api/subscription")]
    [Authorize]
    public class SubscriptionController : ControllerBase
    {
        private readonly ISubscriptionService _subscriptionService;
        private readonly IFeatureAccessResolver _featureAccessResolver;
        private readonly AppDbContext _context;

        public SubscriptionController(
            ISubscriptionService subscriptionService,
            IFeatureAccessResolver featureAccessResolver,
            AppDbContext context)
        {
            _subscriptionService   = subscriptionService;
            _featureAccessResolver = featureAccessResolver;
            _context               = context;
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET /api/subscription/current
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Returns the authenticated user's current active/trial subscription.</summary>
        [HttpGet("current")]
        [ProducesResponseType(typeof(UserSubscriptionDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCurrentSubscription()
        {
            var userId = GetUserId();
            var sub = await _subscriptionService.GetCurrentSubscriptionAsync(userId);

            if (sub == null)
                return NotFound(new { error = "No active subscription found." });

            return Ok(sub);
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET /api/subscription/my-features
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Returns the list of feature keys the authenticated user currently has access to.</summary>
        [HttpGet("my-features")]
        [ProducesResponseType(typeof(UserFeaturesResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetMyFeatures()
        {
            var userId   = GetUserId();
            var features = await _subscriptionService.GetUserFeaturesAsync(userId);

            // Also load plan info for the response
            var sub = await _subscriptionService.GetCurrentSubscriptionAsync(userId);

            return Ok(new UserFeaturesResponse
            {
                UserId      = userId,
                PlanName    = sub?.PlanName ?? "None",
                PlanSlug    = sub?.PlanSlug ?? "none",
                FeatureKeys = features
            });
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET /api/subscription/plans
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns all active plans available for subscription/upgrade.
        /// Includes each plan's features for the pricing comparison UI.
        /// </summary>
        [HttpGet("plans")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(List<PlanDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAvailablePlans()
        {
            var plans = await _context.Plans
                .Where(p => p.IsActive)
                .Include(p => p.PlanFeatures)
                    .ThenInclude(pf => pf.Feature)
                .OrderBy(p => p.SortOrder)
                .Select(p => new PlanDto
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
                                    .Where(pf => pf.Feature != null && pf.Feature.IsActive)
                                    .Select(pf => new FeatureSummaryDto
                                    {
                                        Id          = pf.Feature.Id,
                                        FeatureKey  = pf.Feature.FeatureKey,
                                        DisplayName = pf.Feature.DisplayName
                                    })
                                    .ToList()
                })
                .ToListAsync();

            return Ok(plans);
        }

        // ─────────────────────────────────────────────────────────────────────
        // POST /api/subscription/subscribe
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Subscribe to a plan. User must not have an active paid subscription.
        /// Users on the default Free plan will be auto-migrated.
        /// </summary>
        [HttpPost("subscribe")]
        [ProducesResponseType(typeof(UserSubscriptionDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Subscribe([FromBody] SubscribeRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var userId = GetUserId();
                var sub = await _subscriptionService.SubscribeAsync(userId, request);
                return CreatedAtAction(nameof(GetCurrentSubscription), null, sub);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // POST /api/subscription/upgrade
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Upgrade to a higher plan immediately. Resets the billing period.
        /// </summary>
        [HttpPost("upgrade")]
        [ProducesResponseType(typeof(UserSubscriptionDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Upgrade([FromBody] UpgradeRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var userId = GetUserId();
                var sub = await _subscriptionService.UpgradeAsync(userId, request);
                return Ok(sub);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // POST /api/subscription/downgrade
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Schedule a downgrade to a lower plan at the end of the current billing period.
        /// The user keeps their current features until the period ends.
        /// </summary>
        [HttpPost("downgrade")]
        [ProducesResponseType(typeof(UserSubscriptionDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Downgrade([FromBody] DowngradeRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var userId = GetUserId();
                var sub = await _subscriptionService.DowngradeAsync(userId, request);
                return Ok(sub);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // POST /api/subscription/cancel
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Cancel the current subscription. The user keeps access until the end of the billing period.
        /// </summary>
        [HttpPost("cancel")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Cancel([FromBody] CancelRequest request)
        {
            try
            {
                var userId = GetUserId();
                await _subscriptionService.CancelAsync(userId, request);
                return Ok(new { message = "Subscription cancelled. Access continues until the end of your billing period." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // POST /api/subscription/reactivate
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Reactivate a cancelled or expired subscription.
        /// Creates a new billing period starting today on the same plan.
        /// </summary>
        [HttpPost("reactivate")]
        [ProducesResponseType(typeof(UserSubscriptionDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Reactivate()
        {
            try
            {
                var userId = GetUserId();
                var sub = await _subscriptionService.ReactivateAsync(userId);
                return Ok(sub);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET /api/subscription/history
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Returns the authenticated user's subscription history, ordered newest first.</summary>
        [HttpGet("history")]
        [ProducesResponseType(typeof(List<SubscriptionHistoryDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetHistory()
        {
            var userId  = GetUserId();
            var history = await _subscriptionService.GetHistoryAsync(userId);
            return Ok(history);
        }

        // ─────────────────────────────────────────────────────────────────────
        // HELPER — Extract UserId from JWT claims
        // ─────────────────────────────────────────────────────────────────────

        private Guid GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(claim) || !Guid.TryParse(claim, out var userId))
                throw new UnauthorizedAccessException("Invalid or missing user identity.");

            return userId;
        }
    }
}
