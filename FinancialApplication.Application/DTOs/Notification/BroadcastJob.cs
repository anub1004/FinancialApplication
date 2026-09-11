namespace FinancialApplication.Application.DTOs.Notification
{
    /// <summary>
    /// Represents a broadcast job queued for background processing.
    /// Enqueued by the API controller, dequeued by <see cref="IBroadcastQueue"/>.
    /// </summary>
    public class BroadcastJob
    {
        /// <summary>Unique identifier for tracking this broadcast job.</summary>
        public Guid JobId { get; set; } = Guid.NewGuid();

        /// <summary>The original broadcast request from the admin.</summary>
        public SendBroadcastRequest Request { get; set; } = default!;

        /// <summary>The admin user ID who initiated the broadcast.</summary>
        public Guid AdminId { get; set; }

        /// <summary>When the job was enqueued.</summary>
        public DateTime EnqueuedAt { get; set; } = DateTime.UtcNow;
    }
}
