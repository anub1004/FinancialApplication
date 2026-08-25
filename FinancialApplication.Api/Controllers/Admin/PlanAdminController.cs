using System;
using System.Threading.Tasks;
using FinancialApplication.Application.DTOs.Subscription;
using FinancialApplication.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FinancialApplication.Api.Controllers.Admin
{
    /// <summary>
    /// Admin-only CRUD controller for managing subscription Plans and their feature assignments.
    /// Route: /api/admin/plans
    ///
    /// Soft-delete: DELETE sets IsActive=false; rejected if the plan has active subscribers.
    /// Pricing updates are separated into PUT /{id}/pricing so a PlanPriceHistory record is created.
    /// </summary>
    [ApiController]
    [Route("api/admin/plans")]
    [Authorize(Policy = "AdminOnly")]
    public class PlanAdminController : ControllerBase
    {
        private readonly IPlanService _planService;

        public PlanAdminController(IPlanService planService)
        {
            _planService = planService;
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET /api/admin/plans
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns all plans (active and inactive by default) with their assigned features.
        /// Pass ?includeInactive=false to exclude inactive plans.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(System.Collections.Generic.List<PlanDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAllPlans([FromQuery] bool includeInactive = true)
        {
            var plans = await _planService.GetAllPlansAsync(includeInactive);
            return Ok(plans);
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET /api/admin/plans/{id}
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Returns a single plan by ID, including its assigned features.</summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(PlanDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetPlanById(Guid id)
        {
            var plan = await _planService.GetPlanByIdAsync(id);
            if (plan == null)
                return NotFound(new { error = $"Plan with ID {id} not found." });
            return Ok(plan);
        }

        // ─────────────────────────────────────────────────────────────────────
        // POST /api/admin/plans
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Creates a new subscription plan. Name and Slug must be unique.</summary>
        [HttpPost]
        [ProducesResponseType(typeof(PlanDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreatePlan([FromBody] CreatePlanRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var plan = await _planService.CreatePlanAsync(request);
                return CreatedAtAction(nameof(GetPlanById), new { id = plan.Id }, plan);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // PUT /api/admin/plans/{id}
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Updates a plan's metadata (Name, Slug, Description, Currency, etc.).
        /// To update pricing use PUT /{id}/pricing so price history is recorded.
        /// </summary>
        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(PlanDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdatePlan(Guid id, [FromBody] UpdatePlanRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var plan = await _planService.UpdatePlanAsync(id, request);
                return Ok(plan);
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
        // DELETE /api/admin/plans/{id}
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Soft-deletes a plan (sets IsActive=false).
        /// Rejected with 400 if the plan has active/trial subscribers.
        /// </summary>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeletePlan(Guid id)
        {
            try
            {
                await _planService.DeletePlanAsync(id);
                return NoContent();
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
        // POST /api/admin/plans/{id}/features
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Assigns a feature to a plan. The feature must exist and be active.
        /// Also invalidates the plan's user feature cache.
        /// </summary>
        [HttpPost("{id:guid}/features")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AssignFeatureToPlan(Guid id, [FromBody] AssignFeatureRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                await _planService.AssignFeatureToPlanAsync(id, request.FeatureId);
                return Ok(new { message = "Feature successfully assigned to plan.", planId = id, featureId = request.FeatureId });
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
        // DELETE /api/admin/plans/{planId}/features/{featureId}
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Removes a feature from a plan.
        /// Also invalidates the plan's user feature cache.
        /// </summary>
        [HttpDelete("{planId:guid}/features/{featureId:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RemoveFeatureFromPlan(Guid planId, Guid featureId)
        {
            try
            {
                await _planService.RemoveFeatureFromPlanAsync(planId, featureId);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // PUT /api/admin/plans/{id}/pricing
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Updates plan pricing. Creates a PlanPriceHistory record and closes the previous one.
        /// Use this endpoint (not PUT /{id}) for pricing changes to maintain price history.
        /// </summary>
        [HttpPut("{id:guid}/pricing")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdatePricing(Guid id, [FromBody] UpdatePricingRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                await _planService.UpdatePricingAsync(id, request);
                return Ok(new { message = "Plan pricing updated successfully.", planId = id });
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
    }

    /// <summary>Request body for assigning a feature to a plan.</summary>
    public class AssignFeatureRequest
    {
        [System.ComponentModel.DataAnnotations.Required]
        public Guid FeatureId { get; set; }
    }
}
