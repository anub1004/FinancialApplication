using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinancialApplication.Domain.Domain.Entity
{
    /// <summary>
    /// Tracks all admin actions on plans.
    /// Records pricing changes, feature assignments, and plan modifications.
    /// Primary key: Id (GUID)
    /// Foreign key: PlanId
    /// </summary>
    public class PlanAudit
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// The plan that was modified.
        /// </summary>
        [Required]
        public Guid PlanId { get; set; }

        /// <summary>
        /// The action performed (e.g., "Created", "Updated", "PriceChanged", "Disabled", "FeaturesModified").
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// JSON snapshot of the plan's state before the change.
        /// Null for creation actions.
        /// </summary>
        public string? OldValues { get; set; }

        /// <summary>
        /// JSON snapshot of the plan's state after the change.
        /// Null for deletion actions.
        /// </summary>
        public string? NewValues { get; set; }

        /// <summary>
        /// The admin user who performed this action.
        /// </summary>
        [Required]
        public Guid PerformedBy { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("PlanId")]
        public virtual Plan Plan { get; set; } = null!;
    }
}
