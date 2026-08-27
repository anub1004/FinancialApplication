using System;
using System.Security.Claims;
using System.Threading.Tasks;
using FinancialApplication.Application.DTOs.Transaction;
using FinancialApplication.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using FinancialApplication.Api.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace FinancialApplication.Api.Controllers
{
    /// <summary>
    /// User-facing transaction endpoints for income/expense tracking.
    /// Route: /api/transactions
    /// All endpoints require authentication.
    /// Requires 'transactions' feature (Free+ plan).
    /// </summary>
    [ApiController]
    [Route("api/transactions")]
    [Authorize]
    [RequireFeature("transactions")]
    public class TransactionController : ControllerBase
    {
        private readonly ITransactionService _transactionService;

        public TransactionController(ITransactionService transactionService)
        {
            _transactionService = transactionService;
        }

        // ── POST /api/transactions ──────────────────────────────────────────
        /// <summary>Add a new income or expense transaction.</summary>
        [HttpPost]
        [ProducesResponseType(typeof(TransactionDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateTransactionDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = GetUserId();
            var result = await _transactionService.CreateAsync(userId, dto);
            return CreatedAtAction(nameof(GetById), new { id = result.TransactionId }, result);
        }

        // ── GET /api/transactions ───────────────────────────────────────────
        /// <summary>List all transactions with filters, search, sort, and pagination.</summary>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? category = null,
            [FromQuery] string? type = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] decimal? minAmount = null,
            [FromQuery] decimal? maxAmount = null,
            [FromQuery] string? search = null,
            [FromQuery] string sortBy = "TransactionDate",
            [FromQuery] string sortOrder = "desc",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var userId = GetUserId();
            var (items, totalCount) = await _transactionService.GetAllAsync(
                userId, category, type, fromDate, toDate,
                minAmount, maxAmount, search, sortBy, sortOrder, page, pageSize);

            return Ok(new
            {
                items,
                totalCount,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            });
        }

        // ── GET /api/transactions/{id} ──────────────────────────────────────
        /// <summary>Get a single transaction by ID.</summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(TransactionDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var userId = GetUserId();
            var result = await _transactionService.GetByIdAsync(userId, id);

            if (result == null) return NotFound(new { error = "Transaction not found." });
            return Ok(result);
        }

        // ── PUT /api/transactions/{id} ──────────────────────────────────────
        /// <summary>Update a transaction.</summary>
        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(TransactionDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTransactionDto dto)
        {
            try
            {
                var userId = GetUserId();
                var result = await _transactionService.UpdateAsync(userId, id, dto);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        // ── DELETE /api/transactions/{id} ───────────────────────────────────
        /// <summary>Delete a transaction.</summary>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userId = GetUserId();
            var deleted = await _transactionService.DeleteAsync(userId, id);

            if (!deleted) return NotFound(new { error = "Transaction not found." });
            return Ok(new { message = "Transaction deleted." });
        }

        // ── GET /api/transactions/summary ───────────────────────────────────
        /// <summary>Get monthly income/expense summary.</summary>
        [HttpGet("summary")]
        [ProducesResponseType(typeof(TransactionSummaryDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSummary(
            [FromQuery] int? month = null,
            [FromQuery] int? year = null)
        {
            var userId = GetUserId();
            var result = await _transactionService.GetMonthlySummaryAsync(userId, month, year);
            return Ok(result);
        }

        // ── GET /api/transactions/categories ────────────────────────────────
        /// <summary>Get available transaction categories (predefined + custom).</summary>
        [HttpGet("categories")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(System.Collections.Generic.List<CategoryDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _transactionService.GetCategoriesAsync();
            return Ok(categories);
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
