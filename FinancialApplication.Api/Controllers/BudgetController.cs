using System;
using System.Threading.Tasks;
using FinancialApplication.Application.DTOs.Budget;
using FinancialApplication.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinancialApplication.Api.Controllers
{
    /// <summary>
    /// Budget management controller.
    /// Allows users to set spending limits per category per month,
    /// track actual spending against budgets, and get alerts when approaching limits.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BudgetController : ControllerBase
    {
        private readonly IBudgetService _budgetService;

        public BudgetController(IBudgetService budgetService)
        {
            _budgetService = budgetService;
        }

        /// <summary>
        /// Creates a new budget for a category/month.
        /// </summary>
        [HttpPost("{userId:guid}")]
        public async Task<IActionResult> Create(Guid userId, [FromBody] CreateBudgetDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _budgetService.CreateAsync(userId, dto);
                return CreatedAtAction(nameof(GetById), new { userId, id = result.Id }, result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Gets a specific budget by ID.
        /// </summary>
        [HttpGet("{userId:guid}/{id:guid}")]
        public async Task<IActionResult> GetById(Guid userId, Guid id)
        {
            var result = await _budgetService.GetByIdAsync(userId, id);
            if (result == null)
                return NotFound(new { error = $"Budget with ID {id} not found." });

            return Ok(result);
        }

        /// <summary>
        /// Gets all budgets for a user for a given month/year.
        /// Defaults to the current month if not specified.
        /// </summary>
        [HttpGet("{userId:guid}")]
        public async Task<IActionResult> GetAll(Guid userId, [FromQuery] int? month, [FromQuery] int? year)
        {
            var result = await _budgetService.GetAllAsync(userId, month, year);
            return Ok(result);
        }

        /// <summary>
        /// Updates a budget's limit, alert threshold, or currency.
        /// </summary>
        [HttpPut("{userId:guid}/{id:guid}")]
        public async Task<IActionResult> Update(Guid userId, Guid id, [FromBody] UpdateBudgetDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _budgetService.UpdateAsync(userId, id, dto);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Deletes a budget.
        /// </summary>
        [HttpDelete("{userId:guid}/{id:guid}")]
        public async Task<IActionResult> Delete(Guid userId, Guid id)
        {
            var success = await _budgetService.DeleteAsync(userId, id);
            if (!success)
                return NotFound(new { error = $"Budget with ID {id} not found." });

            return NoContent();
        }

        /// <summary>
        /// Gets budget alerts — categories that are approaching or exceeding their limits.
        /// </summary>
        [HttpGet("{userId:guid}/alerts")]
        public async Task<IActionResult> GetAlerts(Guid userId, [FromQuery] int? month, [FromQuery] int? year)
        {
            var alerts = await _budgetService.GetAlertsAsync(userId, month, year);
            return Ok(alerts);
        }

        /// <summary>
        /// Gets overall budget summary for a given month.
        /// </summary>
        [HttpGet("{userId:guid}/summary")]
        public async Task<IActionResult> GetSummary(Guid userId, [FromQuery] int? month, [FromQuery] int? year)
        {
            var summary = await _budgetService.GetSummaryAsync(userId, month, year);
            return Ok(summary);
        }
    }
}
