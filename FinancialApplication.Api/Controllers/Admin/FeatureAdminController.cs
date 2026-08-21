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
    /// Admin-only CRUD controller for managing Features.
    /// Route: /api/admin/features
    /// 
    /// All endpoints require the AdminOnly policy.
    /// Soft-delete semantics: DELETE sets IsActive=false, does not remove the DB row.
    /// </summary>
    [ApiController]
    [Route("api/admin/features")]
    [Authorize(Policy = "AdminOnly")]
    public class FeatureAdminController : ControllerBase
    {
        private readonly IFeatureService _featureService;

        public FeatureAdminController(IFeatureService featureService)
        {
            _featureService = featureService;
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET /api/admin/features
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Returns all features (active and inactive), ordered by SortOrder.</summary>
        [HttpGet]
        [ProducesResponseType(typeof(System.Collections.Generic.List<FeatureDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAllFeatures()
        {
            var features = await _featureService.GetAllFeaturesAsync();
            return Ok(features);
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET /api/admin/features/{id}
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Returns a single feature by its ID.</summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(FeatureDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetFeatureById(Guid id)
        {
            var feature = await _featureService.GetFeatureByIdAsync(id);
            if (feature == null)
                return NotFound(new { error = $"Feature with ID {id} not found." });
            return Ok(feature);
        }

        // ─────────────────────────────────────────────────────────────────────
        // POST /api/admin/features
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Creates a new feature. FeatureKey must be snake_case and globally unique.</summary>
        [HttpPost]
        [ProducesResponseType(typeof(FeatureDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateFeature([FromBody] CreateFeatureRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var feature = await _featureService.CreateFeatureAsync(request);
                return CreatedAtAction(nameof(GetFeatureById), new { id = feature.Id }, feature);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // PUT /api/admin/features/{id}
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Updates an existing feature's display properties.</summary>
        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(FeatureDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateFeature(Guid id, [FromBody] UpdateFeatureRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var feature = await _featureService.UpdateFeatureAsync(id, request);
                return Ok(feature);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // DELETE /api/admin/features/{id}
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Soft-deletes a feature (sets IsActive=false). The DB row is preserved.</summary>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteFeature(Guid id)
        {
            try
            {
                await _featureService.DeleteFeatureAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // PATCH /api/admin/features/{id}/toggle
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Toggles a feature's IsActive flag.
        /// Also globally invalidates the feature cache (all users affected).
        /// </summary>
        [HttpPatch("{id:guid}/toggle")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ToggleFeature(Guid id)
        {
            try
            {
                var isNowActive = await _featureService.ToggleFeatureAsync(id);
                return Ok(new
                {
                    id,
                    isActive = isNowActive,
                    message  = isNowActive ? "Feature enabled." : "Feature disabled."
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }
    }
}
