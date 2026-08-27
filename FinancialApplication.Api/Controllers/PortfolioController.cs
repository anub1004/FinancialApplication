using System;
using System.Security.Claims;
using System.Threading.Tasks;
using FinancialApplication.Application.DTOs.Portfolio;
using FinancialApplication.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using FinancialApplication.Api.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace FinancialApplication.Api.Controllers
{
    /// <summary>
    /// User-facing portfolio management endpoints.
    /// Route: /api/portfolio
    /// Requires 'portfolio_management' feature (Pro plan).
    /// </summary>
    [ApiController]
    [Route("api/portfolio")]
    [Authorize]
    [RequireFeature("portfolio_management")]
    public class PortfolioController : ControllerBase
    {
        private readonly IPortfolioService _portfolioService;

        public PortfolioController(IPortfolioService portfolioService)
        {
            _portfolioService = portfolioService;
        }

        // ── POST /api/portfolio ─────────────────────────────────────────────
        /// <summary>Add a new portfolio asset.</summary>
        [HttpPost]
        [ProducesResponseType(typeof(PortfolioAssetDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreatePortfolioAssetDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = GetUserId();
            var result = await _portfolioService.CreateAsync(userId, dto);
            return CreatedAtAction(nameof(GetById), new { id = result.PortfolioAssetId }, result);
        }

        // ── GET /api/portfolio ──────────────────────────────────────────────
        /// <summary>List all portfolio assets.</summary>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll()
        {
            var userId = GetUserId();
            var items = await _portfolioService.GetAllAsync(userId);
            return Ok(new { items, totalCount = items.Count });
        }

        // ── GET /api/portfolio/{id} ─────────────────────────────────────────
        /// <summary>Get a single portfolio asset by ID.</summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(PortfolioAssetDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var userId = GetUserId();
            var result = await _portfolioService.GetByIdAsync(userId, id);
            if (result == null) return NotFound(new { message = "Portfolio asset not found." });
            return Ok(result);
        }

        // ── PUT /api/portfolio/{id} ─────────────────────────────────────────
        /// <summary>Update a portfolio asset.</summary>
        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(PortfolioAssetDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePortfolioAssetDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var userId = GetUserId();
                var result = await _portfolioService.UpdateAsync(userId, id, dto);
                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Portfolio asset not found." });
            }
        }

        // ── DELETE /api/portfolio/{id} ──────────────────────────────────────
        /// <summary>Delete a portfolio asset.</summary>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userId = GetUserId();
            var deleted = await _portfolioService.DeleteAsync(userId, id);
            if (!deleted) return NotFound(new { message = "Portfolio asset not found." });
            return NoContent();
        }

        // ── GET /api/portfolio/summary ──────────────────────────────────────
        /// <summary>Get portfolio summary with allocation breakdown.</summary>
        [HttpGet("summary")]
        [ProducesResponseType(typeof(PortfolioSummaryDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSummary()
        {
            var userId = GetUserId();
            var summary = await _portfolioService.GetSummaryAsync(userId);
            return Ok(summary);
        }

        private Guid GetUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new UnauthorizedAccessException("User ID not found in token.");
            return Guid.Parse(claim);
        }
    }
}
