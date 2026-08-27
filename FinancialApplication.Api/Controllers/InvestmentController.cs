using System;
using System.Security.Claims;
using System.Threading.Tasks;
using FinancialApplication.Application.DTOs.Investment;
using FinancialApplication.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using FinancialApplication.Api.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace FinancialApplication.Api.Controllers
{
    /// <summary>
    /// User-facing investment portfolio endpoints.
    /// Route: /api/investments
    /// All endpoints require authentication.
    /// Requires 'investment_tracking' feature (Basic+ plan).
    /// </summary>
    [ApiController]
    [Route("api/investments")]
    [Authorize]
    [RequireFeature("investment_tracking")]
    public class InvestmentController : ControllerBase
    {
        private readonly IInvestmentService _investmentService;

        public InvestmentController(IInvestmentService investmentService)
        {
            _investmentService = investmentService;
        }

        // ── POST /api/investments ───────────────────────────────────────────
        /// <summary>Add a new investment.</summary>
        [HttpPost]
        [ProducesResponseType(typeof(InvestmentDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateInvestmentDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = GetUserId();
            var result = await _investmentService.CreateAsync(userId, dto);
            return CreatedAtAction(nameof(GetById), new { id = result.InvestmentId }, result);
        }

        // ── GET /api/investments ────────────────────────────────────────────
        /// <summary>List all investments with filters, sort, and pagination.</summary>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? investmentType = null,
            [FromQuery] string? status = null,
            [FromQuery] string sortBy = "CreatedAt",
            [FromQuery] string sortOrder = "desc",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var userId = GetUserId();
            var (items, totalCount) = await _investmentService.GetAllAsync(
                userId, investmentType, status, sortBy, sortOrder, page, pageSize);

            return Ok(new
            {
                items,
                totalCount,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            });
        }

        // ── GET /api/investments/{id} ───────────────────────────────────────
        /// <summary>Get a single investment by ID.</summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(InvestmentDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var userId = GetUserId();
            var result = await _investmentService.GetByIdAsync(userId, id);

            if (result == null) return NotFound(new { error = "Investment not found." });
            return Ok(result);
        }

        // ── PUT /api/investments/{id} ───────────────────────────────────────
        /// <summary>Update an investment (e.g., update current value, close it).</summary>
        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(InvestmentDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateInvestmentDto dto)
        {
            try
            {
                var userId = GetUserId();
                var result = await _investmentService.UpdateAsync(userId, id, dto);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        // ── DELETE /api/investments/{id} ────────────────────────────────────
        /// <summary>Delete an investment.</summary>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userId = GetUserId();
            var deleted = await _investmentService.DeleteAsync(userId, id);

            if (!deleted) return NotFound(new { error = "Investment not found." });
            return Ok(new { message = "Investment deleted." });
        }

        // ── GET /api/investments/summary ────────────────────────────────────
        /// <summary>Get portfolio summary with total invested, current value, returns, and type breakdown.</summary>
        [HttpGet("summary")]
        [ProducesResponseType(typeof(InvestmentSummaryDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPortfolioSummary()
        {
            var userId = GetUserId();
            var result = await _investmentService.GetPortfolioSummaryAsync(userId);
            return Ok(result);
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
