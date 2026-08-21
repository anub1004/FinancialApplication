using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FinancialApplication.Domain.Domain.Enums;

namespace FinancialApplication.Domain.Domain.Entity
{
    /// <summary>
    /// Immutable audit trail of all subscription lifecycle events.
    /// Records actions like Created, Upgraded, Downgraded, Renewed, Cancelled, Expired, Reactivated.
    /// Primary key: Id (GUID)
    /// Foreign keys: UserId, SubscriptionId, FromPlanId (optional), ToPlanId (optional)
    /// </summary>
    public class SubscriptionHistory
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// The user this history entry belongs to.
        /// </summary>
        [Required]
        public Guid UserId { get; set; }

        /// <summary>
        /// The subscription this history entry relates to.
        /// </summary>
        [Required]
        public Guid SubscriptionId { get; set; }

        /// <summary>
        /// The action that was performed on the subscription.
        /// Stored as string in DB (configured in Phase 2).
        /// </summary>
        [Required]
        public SubscriptionActionEnum Action { get; set; }

        /// <summary>
        /// The plan the user was on before this action (null for initial creation).
        /// </summary>
        public Guid? FromPlanId { get; set; }

        /// <summary>
        /// The plan the user moved to after this action (null for cancellation/expiry).
        /// </summary>
        public Guid? ToPlanId { get; set; }

        /// <summary>
        /// Additional notes about the action (e.g., cancellation reason, admin notes).
        /// </summary>
        [MaxLength(500)]
        public string? Notes { get; set; }

        /// <summary>
        /// Who performed this action: "User", "Admin", or "System".
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string PerformedBy { get; set; } = "System";

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        [ForeignKey("SubscriptionId")]
        public virtual UserSubscription UserSubscription { get; set; } = null!;
    }
}
