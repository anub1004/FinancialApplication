using System;
using System.Security.Claims;
using System.Threading.Tasks;
using FinancialApplication.Application.DTOs.Goal;
using FinancialApplication.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using FinancialApplication.Api.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace FinancialApplication.Api.Controllers
{
    /// <summary>
    /// User-facing financial goals endpoints.
    /// Route: /api/goals
    /// All endpoints require authentication.
    /// Requires 'goals_basic' feature (Free+ plan).
    /// </summary>
    [ApiController]
    [Route("api/goals")]
    [Authorize]
    [RequireFeature("goals_basic")]
    public class GoalController : ControllerBase
    {
        private readonly IGoalService _goalService;

        public GoalController(IGoalService goalService)
        {
            _goalService = goalService;
        }

        // ── POST /api/goals ─────────────────────────────────────────────────
        /// <summary>Create a new financial goal.</summary>
        [HttpPost]
        [ProducesResponseType(typeof(GoalDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateGoalDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = GetUserId();
            var result = await _goalService.CreateAsync(userId, dto);
            return CreatedAtAction(nameof(GetById), new { id = result.GoalId }, result);
        }

        // ── GET /api/goals ──────────────────────────────────────────────────
        /// <summary>Get all goals, optionally filtered by status.</summary>
        [HttpGet]
        [ProducesResponseType(typeof(System.Collections.Generic.List<GoalDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll([FromQuery] string? status = null)
        {
            var userId = GetUserId();
            var goals = await _goalService.GetAllAsync(userId, status);
            return Ok(goals);
        }

        // ── GET /api/goals/{id} ─────────────────────────────────────────────
        /// <summary>Get a single goal by ID.</summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(GoalDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var userId = GetUserId();
            var result = await _goalService.GetByIdAsync(userId, id);

            if (result == null) return NotFound(new { error = "Goal not found." });
            return Ok(result);
        }

        // ── PUT /api/goals/{id} ─────────────────────────────────────────────
        /// <summary>Update goal details.</summary>
        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(GoalDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateGoalDto dto)
        {
            try
            {
                var userId = GetUserId();
                var result = await _goalService.UpdateAsync(userId, id, dto);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        // ── DELETE /api/goals/{id} ──────────────────────────────────────────
        /// <summary>Delete a goal.</summary>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userId = GetUserId();
            var deleted = await _goalService.DeleteAsync(userId, id);

            if (!deleted) return NotFound(new { error = "Goal not found." });
            return Ok(new { message = "Goal deleted." });
        }

        // ── POST /api/goals/{id}/contribute ─────────────────────────────────
        /// <summary>Add money toward a goal. Auto-completes if target is reached.</summary>
        [HttpPost("{id:guid}/contribute")]
        [ProducesResponseType(typeof(GoalDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Contribute(Guid id, [FromBody] GoalContributionDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var userId = GetUserId();
                var result = await _goalService.ContributeAsync(userId, id, dto);
                return Ok(result);
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

        // ── POST /api/goals/{id}/status ─────────────────────────────────────
        /// <summary>Change goal status (Complete, Fail, InProgress, etc.).</summary>
        [HttpPost("{id:guid}/status")]
        [ProducesResponseType(typeof(GoalDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] GoalStatusUpdateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var userId = GetUserId();
                var result = await _goalService.UpdateStatusAsync(userId, id, dto);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        // ── Helper ──────────────────────────────────────────────────────────
        private Guid GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(claim) || !Guid.TryParse(claim, out var userId))
                throw new UnauthorizedAccessException("Invalid or missing user identity.");
            return userId;
        }
    }
}
