using FinancialApplication.Application.DTOs.Notification;
using FinancialApplication.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FinancialApplication.Api.Controllers.Admin
{
    /// <summary>
    /// Admin-only notification endpoints:
    ///   - POST /api/notificationadmin/broadcast     → send message to all (or filtered) users
    ///   - GET  /api/notificationadmin/history       → paginated list of past broadcasts
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "AdminOnly")]
    public class NotificationAdminController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationAdminController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        // ── Helpers ─────────────────────────────────────────────────────────

        private Guid GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? User.FindFirstValue("sub")
                        ?? User.FindFirstValue("id");

            if (string.IsNullOrWhiteSpace(claim) || !Guid.TryParse(claim, out var userId))
                throw new UnauthorizedAccessException("Unable to identify current admin user.");

            return userId;
        }

        // ────────────────────────────────────────────────────────────────────
        // POST /api/notificationadmin/broadcast
        // ────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Send a broadcast notification to all active users (or a filtered subset).
        ///
        /// <para>Filtering options (all optional):</para>
        /// <list type="bullet">
        ///   <item><description><c>targetPlanSlug</c> — only users on this plan (e.g. "pro", "basic").</description></item>
        ///   <item><description><c>targetRole</c>     — only users with this role name (e.g. "User", "Admin").</description></item>
        /// </list>
        ///
        /// <para>Returns a summary containing the number of recipients and dispatch timestamp.</para>
        /// </summary>
        [HttpPost("broadcast")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Broadcast([FromBody] SendBroadcastRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var adminId = GetCurrentUserId();
                var result = await _notificationService.SendAdminBroadcastAsync(request, adminId);

                if (result.RecipientsCount == 0)
                {
                    return Ok(new
                    {
                        success = false,
                        message = "No users matched the specified filter criteria. No notifications were sent.",
                        data = result
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = $"Notification successfully sent to {result.RecipientsCount} user(s).",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // ────────────────────────────────────────────────────────────────────
        // GET /api/notificationadmin/history
        // ────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns paginated history of past admin broadcasts.
        /// Each entry is grouped by title + message + minute bucket and shows
        /// how many users received it.
        /// </summary>
        [HttpGet("history")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetBroadcastHistory(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var history = await _notificationService.GetBroadcastHistoryAsync(page, pageSize);
            return Ok(new
            {
                page,
                pageSize,
                items = history
            });
        }
    }
}
