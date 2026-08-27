using System;
using System.Security.Claims;
using System.Threading.Tasks;
using FinancialApplication.Application.DTOs.Dashboard;
using FinancialApplication.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using FinancialApplication.Api.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace FinancialApplication.Api.Controllers
{
    /// <summary>
    /// Dashboard API providing aggregated financial data for the user's home screen.
    /// Route: /api/dashboard
    /// All endpoints require authentication.
    /// Requires 'dashboard' feature (Free+ plan).
    /// </summary>
    [ApiController]
    [Route("api/dashboard")]
    [Authorize]
    [RequireFeature("dashboard")]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        // ── GET /api/dashboard/summary ──────────────────────────────────────
        /// <summary>
        /// Main dashboard summary: monthly income/expense, balance, savings rate,
        /// investment snapshot, goal progress.
        /// </summary>
        [HttpGet("summary")]
        [ProducesResponseType(typeof(DashboardSummaryDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSummary(
            [FromQuery] int? month = null,
            [FromQuery] int? year = null)
        {
            var userId = GetUserId();
            var result = await _dashboardService.GetSummaryAsync(userId, month, year);
            return Ok(result);
        }

        // ── GET /api/dashboard/monthly-trend ────────────────────────────────
        /// <summary>
        /// Income vs Expense trend for the last N months (default 6).
        /// Use this for bar/line charts.
        /// </summary>
        [HttpGet("monthly-trend")]
        [ProducesResponseType(typeof(System.Collections.Generic.List<MonthlyTrendDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMonthlyTrend([FromQuery] int months = 6)
        {
            var userId = GetUserId();
            var result = await _dashboardService.GetMonthlyTrendAsync(userId, months);
            return Ok(result);
        }

        // ── GET /api/dashboard/category-breakdown ───────────────────────────
        /// <summary>
        /// Expense breakdown by category for a given month.
        /// Use this for pie/donut charts.
        /// </summary>
        [HttpGet("category-breakdown")]
        [ProducesResponseType(typeof(System.Collections.Generic.List<CategoryBreakdownDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCategoryBreakdown(
            [FromQuery] int? month = null,
            [FromQuery] int? year = null)
        {
            var userId = GetUserId();
            var result = await _dashboardService.GetCategoryBreakdownAsync(userId, month, year);
            return Ok(result);
        }

        // ── GET /api/dashboard/recent-activity ──────────────────────────────
        /// <summary>
        /// Recent activity feed combining transactions, investments, and goals.
        /// Sorted by most recent.
        /// </summary>
        [HttpGet("recent-activity")]
        [ProducesResponseType(typeof(System.Collections.Generic.List<RecentActivityDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetRecentActivity([FromQuery] int count = 10)
        {
            var userId = GetUserId();
            var result = await _dashboardService.GetRecentActivityAsync(userId, count);
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
