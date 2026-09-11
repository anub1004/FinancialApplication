using FinancialApplication.Domain.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinancialApplication.Domain.Domain.Entity
{
    /// <summary>
    /// A notification record delivered to a single user.
    /// Admin broadcasts fan-out to one row per targeted user, allowing
    /// individual read-state tracking and deletion.
    /// </summary>
    public class Notification
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Owning user. All notifications are stored per-user so that
        /// read state can be tracked individually.
        /// </summary>
        [Required]
        public Guid UserId { get; set; }

        /// <summary>
        /// Short headline shown in the notification bell / list header.
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Full notification body text.
        /// </summary>
        [Required]
        [MaxLength(2000)]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Classifies what triggered this notification.
        /// </summary>
        [Required]
        public NotificationTypeEnum Type { get; set; }

        /// <summary>
        /// True once the user has opened / dismissed the notification.
        /// </summary>
        [Required]
        public bool IsRead { get; set; } = false;

        /// <summary>
        /// True when this row was created as part of an admin broadcast
        /// (fan-out). Allows history queries to distinguish broadcast vs
        /// system-generated notifications.
        /// </summary>
        [Required]
        public bool IsGlobal { get; set; } = false;

        /// <summary>
        /// Optional reference to the subscription, payment or other entity
        /// that triggered this notification. Useful for deep-linking in the UI.
        /// </summary>
        public Guid? RelatedEntityId { get; set; }

        /// <summary>
        /// UTC timestamp when the notification was created / dispatched.
        /// </summary>
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// UTC timestamp when the user first read the notification.
        /// Null if still unread.
        /// </summary>
        public DateTime? ReadAt { get; set; }

        // ── Navigation ─────────────────────────────────────────────────────
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
    }
}
