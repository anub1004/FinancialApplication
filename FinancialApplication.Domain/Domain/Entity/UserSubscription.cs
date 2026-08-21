using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FinancialApplication.Domain.Domain.Enums;

namespace FinancialApplication.Domain.Domain.Entity
{
    /// <summary>
    /// Represents a user's subscription to a plan.
    /// Only one active or trial subscription is allowed per user
    /// (enforced by a filtered unique index in Phase 2).
    /// Primary key: Id (GUID)
    /// Foreign keys: UserId, PlanId
    /// </summary>
    public class UserSubscription
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// The user who owns this subscription.
        /// </summary>
        [Required]
        public Guid UserId { get; set; }

        /// <summary>
        /// The plan the user is subscribed to.
        /// </summary>
        [Required]
        public Guid PlanId { get; set; }

        /// <summary>
        /// Current status of the subscription.
        /// Stored as string in DB (configured in Phase 2).
        /// </summary>
        [Required]
        public SubscriptionStatusEnum Status { get; set; }

        /// <summary>
        /// How often the user is billed (Monthly, Annual, Lifetime).
        /// Stored as string in DB (configured in Phase 2).
        /// </summary>
        [Required]
        public BillingCycleEnum BillingCycle { get; set; }

        /// <summary>
        /// When the subscription period started.
        /// </summary>
        [Required]
        public DateTime StartDate { get; set; }

        /// <summary>
        /// When the subscription period ends.
        /// Must be greater than StartDate.
        /// </summary>
        [Required]
        public DateTime EndDate { get; set; }

        /// <summary>
        /// When the trial period ends (null if no trial).
        /// </summary>
        public DateTime? TrialEndDate { get; set; }

        /// <summary>
        /// Next scheduled renewal date (null if auto-renew is off or subscription is not recurring).
        /// </summary>
        public DateTime? NextRenewalDate { get; set; }

        /// <summary>
        /// When the user cancelled the subscription (null if not cancelled).
        /// </summary>
        public DateTime? CancelledAt { get; set; }

        /// <summary>
        /// Reason provided by the user for cancellation.
        /// </summary>
        [MaxLength(500)]
        public string? CancelReason { get; set; }

        /// <summary>
        /// Whether the subscription auto-renews at the end of the billing period.
        /// </summary>
        [Required]
        public bool AutoRenew { get; set; } = true;

        /// <summary>
        /// The plan to switch to at the end of the current period (for scheduled downgrades).
        /// Null if no downgrade is scheduled.
        /// </summary>
        public Guid? ScheduledPlanId { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        [ForeignKey("PlanId")]
        public virtual Plan Plan { get; set; } = null!;

        public virtual ICollection<SubscriptionHistory> SubscriptionHistories { get; set; } = new List<SubscriptionHistory>();
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}
