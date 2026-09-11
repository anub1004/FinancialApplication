using FinancialApplication.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FinancialApplication.Api.Controllers
{
    /// <summary>
    /// User-facing notification endpoints.
    /// All routes require an authenticated user; data is scoped to the caller's userId.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        // ── Helpers ────────────────────────────────────────────────────────

        private Guid GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? User.FindFirstValue("sub")
                        ?? User.FindFirstValue("id");

            if (string.IsNullOrWhiteSpace(claim) || !Guid.TryParse(claim, out var userId))
                throw new UnauthorizedAccessException("Unable to identify current user.");

            return userId;
        }

        // ────────────────────────────────────────────────────────────────────
        // GET /api/notification
        // ────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns a paginated list of the caller's notifications, newest first.
        /// </summary>
        /// <param name="page">1-based page number (default 1).</param>
        /// <param name="pageSize">Items per page, 1–50 (default 20).</param>
        /// <param name="onlyUnread">If true, returns only unread notifications.</param>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetNotifications(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] bool? onlyUnread = null)
        {
            var userId = GetCurrentUserId();
            var result = await _notificationService
                .GetUserNotificationsAsync(userId, page, pageSize, onlyUnread);
            return Ok(result);
        }

        // ────────────────────────────────────────────────────────────────────
        // GET /api/notification/unread-count
        // ────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the number of unread notifications for the badge count in the UI.
        /// </summary>
        [HttpGet("unread-count")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = GetCurrentUserId();
            var count = await _notificationService.GetUnreadCountAsync(userId);
            return Ok(new { unreadCount = count });
        }

        // ────────────────────────────────────────────────────────────────────
        // PUT /api/notification/{id}/read
        // ────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Marks a single notification as read. Idempotent — safe to call repeatedly.
        /// Returns 404 if the notification does not exist or belongs to another user.
        /// </summary>
        [HttpPut("{id:guid}/read")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> MarkAsRead(Guid id)
        {
            var userId = GetCurrentUserId();
            var success = await _notificationService.MarkAsReadAsync(id, userId);

            if (!success)
                return NotFound(new { message = "Notification not found or access denied." });

            return Ok(new { message = "Notification marked as read." });
        }

        // ────────────────────────────────────────────────────────────────────
        // PUT /api/notification/read-all
        // ────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Marks ALL unread notifications for the caller as read in one call.
        /// </summary>
        [HttpPut("read-all")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = GetCurrentUserId();
            await _notificationService.MarkAllAsReadAsync(userId);
            return Ok(new { message = "All notifications marked as read." });
        }
    }
}
