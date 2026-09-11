using System.ComponentModel.DataAnnotations;

namespace FinancialApplication.Application.DTOs.Notification
{
    /// <summary>
    /// Request body for admin broadcast notifications.
    /// </summary>
    public class SendBroadcastRequest
    {
        /// <summary>Short title shown in the notification bell.</summary>
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        /// <summary>Full message body.</summary>
        [Required]
        [MaxLength(2000)]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// If set, only users currently on this plan slug receive the
        /// notification (e.g. "pro", "basic").
        /// Null = send to ALL active users.
        /// </summary>
        [MaxLength(100)]
        public string? TargetPlanSlug { get; set; }

        /// <summary>
        /// If set, only users with this role name receive the notification
        /// (e.g. "User", "Admin").
        /// Null = all roles.
        /// </summary>
        [MaxLength(50)]
        public string? TargetRole { get; set; }

        /// <summary>
        /// Number of notification rows inserted per database batch.
        /// Default: 100. Maximum: 500.
        /// Higher values = fewer round trips but larger transactions.
        /// </summary>
        [Range(1, 500)]
        public int BatchSize { get; set; } = 100;
    }

    /// <summary>
    /// Summary returned after a broadcast is dispatched.
    /// </summary>
    public class BroadcastResultDto
    {
        public int RecipientsCount { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime DispatchedAt { get; set; }
    }

    /// <summary>
    /// Admin history row for a broadcast event (one row per unique broadcast title+timestamp).
    /// </summary>
    public class BroadcastHistoryDto
    {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public int RecipientsCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Returned immediately (HTTP 202) when a broadcast is queued for background processing.
    /// </summary>
    public class BroadcastQueuedDto
    {
        public Guid JobId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Status { get; set; } = "Queued";
        public DateTime EnqueuedAt { get; set; }
    }
}
