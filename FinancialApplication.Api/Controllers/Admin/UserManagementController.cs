using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FinancialApplication.Application.DTOs.Admin;
using FinancialApplication.Application.Interfaces;
using FinancialApplication.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinancialApplication.Api.Controllers.Admin
{
    /// <summary>
    /// Admin-only controller for comprehensive user management.
    /// Provides endpoints for listing, searching, filtering, enabling/disabling users,
    /// role management, user details, statistics, and CSV export.
    /// Route: /api/admin/users
    /// </summary>
    [ApiController]
    [Route("api/admin/users")]
    [Authorize(Policy = "AdminOnly")]
    public class UserManagementController : ControllerBase
    {
        private readonly IAdminService _adminService;
        private readonly AppDbContext _context;

        public UserManagementController(IAdminService adminService, AppDbContext context)
        {
            _adminService = adminService;
            _context = context;
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET /api/admin/users
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns a paginated list of all users with optional search, filter, and sort.
        /// Query params: search, role, isActive, sortBy, sortOrder, page, pageSize.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAllUsers(
            [FromQuery] string? search    = null,
            [FromQuery] string? role      = null,
            [FromQuery] bool?   isActive  = null,
            [FromQuery] string  sortBy    = "createdAt",
            [FromQuery] string  sortOrder = "desc",
            [FromQuery] int     page      = 1,
            [FromQuery] int     pageSize  = 20)
        {
            if (page < 1) page = 1;
            if (pageSize is < 1 or > 100) pageSize = 20;

            var (users, totalCount) = await _adminService.GetAllUsersAsync(
                search, role, isActive, sortBy, sortOrder, page, pageSize);

            return Ok(new
            {
                total      = totalCount,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                items      = users
            });
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET /api/admin/users/stats
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns aggregate user statistics for admin dashboard header cards.
        /// </summary>
        [HttpGet("stats")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetStats()
        {
            var stats = await _adminService.GetUserStatsAsync();
            return Ok(stats);
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET /api/admin/users/{id}
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns detailed information for a single user including subscriptions and payment summary.
        /// </summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetUserDetail(Guid id)
        {
            var detail = await _adminService.GetUserDetailAsync(id);
            if (detail == null)
                return NotFound(new { error = $"User with ID {id} not found." });

            return Ok(detail);
        }

        // ─────────────────────────────────────────────────────────────────────
        // PATCH /api/admin/users/{id}/status
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Enables or disables a user account.
        /// </summary>
        [HttpPatch("{id:guid}/status")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateUserStatus(Guid id, [FromBody] UpdateUserStatusRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _adminService.UpdateUserStatusAsync(id, request.IsActive);
                return Ok(new
                {
                    userId   = id,
                    isActive = request.IsActive,
                    message  = result
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // PATCH /api/admin/users/{id}/role
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Changes a user's role.
        /// </summary>
        [HttpPatch("{id:guid}/role")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateUserRole(Guid id, [FromBody] UpdateUserRoleRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _adminService.UpdateUserRoleAsync(id, request.RoleName);
                return Ok(new
                {
                    userId   = id,
                    roleName = request.RoleName,
                    message  = result
                });
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
        // DELETE /api/admin/users/{id}
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Soft-deletes a user (sets IsActive = false).
        /// </summary>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            try
            {
                var result = await _adminService.DeleteUserAsync(id);
                return Ok(new { userId = id, message = result });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET /api/admin/users/roles
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns all available roles (for dropdown menus in the frontend).
        /// </summary>
        [HttpGet("roles")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetRoles()
        {
            var roles = await _context.Roles
                .Where(r => r.IsActive)
                .OrderBy(r => r.Id)
                .Select(r => new
                {
                    r.Id,
                    r.Name,
                    r.IsActive
                })
                .ToListAsync();

            return Ok(roles);
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET /api/admin/users/export
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Exports all users as a CSV file for download.
        /// Supports the same search/filter params as GetAllUsers.
        /// </summary>
        [HttpGet("export")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> ExportUsers(
            [FromQuery] string? search   = null,
            [FromQuery] string? role     = null,
            [FromQuery] bool?   isActive = null)
        {
            // Fetch all matching users (no pagination for export)
            var (users, _) = await _adminService.GetAllUsersAsync(
                search, role, isActive,
                sortBy: "createdAt", sortOrder: "desc",
                page: 1, pageSize: int.MaxValue);

            var csv = new StringBuilder();
            csv.AppendLine("Id,Username,Email,Role,IsActive,HasGoogleLogin,HasTOTP,CurrentPlan,SubscriptionStatus,CreatedAt,UpdatedAt");

            foreach (var u in users)
            {
                csv.AppendLine(
                    $"{u.Id}," +
                    $"\"{EscapeCsv(u.Username)}\"," +
                    $"\"{EscapeCsv(u.Email)}\"," +
                    $"\"{EscapeCsv(u.RoleName)}\"," +
                    $"{u.IsActive}," +
                    $"{u.HasGoogleLogin}," +
                    $"{u.HasTotpConfigured}," +
                    $"\"{EscapeCsv(u.CurrentPlanName ?? "None")}\"," +
                    $"\"{EscapeCsv(u.SubscriptionStatus ?? "None")}\"," +
                    $"{u.CreatedAt:yyyy-MM-dd HH:mm:ss}," +
                    $"{u.UpdatedAt:yyyy-MM-dd HH:mm:ss}");
            }

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", $"users_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");
        }

        /// <summary>
        /// Escapes double quotes in CSV field values.
        /// </summary>
        private static string EscapeCsv(string value) =>
            value?.Replace("\"", "\"\"") ?? string.Empty;
    }
}
