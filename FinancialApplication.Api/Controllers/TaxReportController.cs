using System;
using System.Security.Claims;
using System.Threading.Tasks;
using FinancialApplication.Application.DTOs.Tax;
using FinancialApplication.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using FinancialApplication.Api.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace FinancialApplication.Api.Controllers
{
    /// <summary>
    /// User-facing tax report endpoints.
    /// Route: /api/tax
    /// Requires 'tax_reports' feature (Pro plan).
    /// </summary>
    [ApiController]
    [Route("api/tax")]
    [Authorize]
    [RequireFeature("tax_reports")]
    public class TaxReportController : ControllerBase
    {
        private readonly ITaxReportService _taxService;

        public TaxReportController(ITaxReportService taxService)
        {
            _taxService = taxService;
        }

        // ── POST /api/tax ───────────────────────────────────────────────────
        /// <summary>Add a new tax entry (income, deduction, or capital gain).</summary>
        [HttpPost]
        [ProducesResponseType(typeof(TaxEntryDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateTaxEntryDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var userId = GetUserId();
                var result = await _taxService.CreateAsync(userId, dto);
                return CreatedAtAction(nameof(GetAll), new { fy = dto.FinancialYear }, result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // ── GET /api/tax?fy=2025-26 ─────────────────────────────────────────
        /// <summary>Get all tax entries for a financial year.</summary>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll([FromQuery] string fy = "2025-26")
        {
            var userId = GetUserId();
            var entries = await _taxService.GetAllAsync(userId, fy);
            return Ok(new { items = entries, totalCount = entries.Count, financialYear = fy });
        }

        // ── PUT /api/tax/{id} ───────────────────────────────────────────────
        /// <summary>Update a tax entry.</summary>
        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(TaxEntryDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTaxEntryDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var userId = GetUserId();
                var result = await _taxService.UpdateAsync(userId, id, dto);
                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Tax entry not found." });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // ── DELETE /api/tax/{id} ────────────────────────────────────────────
        /// <summary>Delete a tax entry.</summary>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userId = GetUserId();
            var deleted = await _taxService.DeleteAsync(userId, id);
            if (!deleted) return NotFound(new { message = "Tax entry not found." });
            return NoContent();
        }

        // ── GET /api/tax/compute?fy=2025-26 ─────────────────────────────────
        /// <summary>
        /// Compute tax for a financial year under both Old and New regimes.
        /// Returns full slab breakdown, surcharge, cess, rebate, and recommendation.
        /// </summary>
        [HttpGet("compute")]
        [ProducesResponseType(typeof(TaxComputationDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> ComputeTax([FromQuery] string fy = "2025-26")
        {
            var userId = GetUserId();
            var result = await _taxService.ComputeTaxAsync(userId, fy);
            return Ok(result);
        }

        // ── GET /api/tax/report?fy=2025-26 ──────────────────────────────────
        /// <summary>
        /// Generate and download a tax computation report as a text file.
        /// </summary>
        [HttpGet("report")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> DownloadReport([FromQuery] string fy = "2025-26")
        {
            var userId = GetUserId();
            var pdfBytes = await _taxService.GenerateReportPdfAsync(userId, fy);
            return File(pdfBytes, "text/plain", $"Tax_Report_FY_{fy}.txt");
        }

        private Guid GetUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new UnauthorizedAccessException("User ID not found in token.");
            return Guid.Parse(claim);
        }
    }
}
