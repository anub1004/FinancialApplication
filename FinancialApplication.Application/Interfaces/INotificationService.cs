using FinancialApplication.Application.DTOs.Notification;
using FinancialApplication.Domain.Domain.Enums;

namespace FinancialApplication.Application.Interfaces
{
    /// <summary>
    /// Core notification service: creates, stores, and queries notifications.
    /// Used by admin controllers (broadcast) and by the background notification
    /// job (subscription lifecycle events).
    /// </summary>
    public interface INotificationService
    {
        // ── Admin broadcast ─────────────────────────────────────────────────

        /// <summary>
        /// Fan-out a broadcast message to all matching users (one DB row per user).
        /// Filtering is optional: pass null to target every active user.
        /// Returns a summary of how many recipients were notified.
        /// </summary>
        Task<BroadcastResultDto> SendAdminBroadcastAsync(
            SendBroadcastRequest request,
            Guid adminId);

        /// <summary>
        /// Enqueue a broadcast for background processing.
        /// Returns immediately with a queued acknowledgement (HTTP 202 Accepted).
        /// The actual fan-out is handled by <c>BroadcastWorker</c>.
        /// </summary>
        Task<BroadcastQueuedDto> EnqueueBroadcastAsync(
            SendBroadcastRequest request,
            Guid adminId);

        // ── Automated / system notifications ────────────────────────────────

        /// <summary>
        /// Create a single typed notification for a specific user.
        /// Used by the background subscription-notification job and by
        /// SubscriptionService lifecycle hooks.
        /// </summary>
        Task SendSubscriptionNotificationAsync(
            Guid userId,
            NotificationTypeEnum type,
            string title,
            string message,
            Guid? relatedEntityId = null);

        // ── User read-side ────────────────────────────────────────────────

        /// <summary>
        /// Paginated list of notifications for a user, newest first.
        /// </summary>
        Task<NotificationPagedResponse> GetUserNotificationsAsync(
            Guid userId,
            int page,
            int pageSize,
            bool? onlyUnread = null);

        /// <summary>
        /// Number of unread notifications for a user (used for badge counts).
        /// </summary>
        Task<int> GetUnreadCountAsync(Guid userId);

        /// <summary>
        /// Mark a single notification as read. Returns false if not found or
        /// does not belong to the requesting user.
        /// </summary>
        Task<bool> MarkAsReadAsync(Guid notificationId, Guid userId);

        /// <summary>
        /// Mark every unread notification as read for the specified user.
        /// </summary>
        Task MarkAllAsReadAsync(Guid userId);

        // ── Broadcast history (admin) ────────────────────────────────────

        /// <summary>
        /// Returns a summarised history of past admin broadcasts, grouped by
        /// title + CreatedAt, for the admin notification history tab.
        /// </summary>
        Task<List<BroadcastHistoryDto>> GetBroadcastHistoryAsync(int page, int pageSize);
    }
}
