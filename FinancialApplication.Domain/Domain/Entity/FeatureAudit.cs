using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinancialApplication.Domain.Domain.Entity
{
    /// <summary>
    /// Tracks all admin actions on features.
    /// Records who changed what and when, with old/new state snapshots.
    /// Primary key: Id (GUID)
    /// Foreign key: FeatureId
    /// </summary>
    public class FeatureAudit
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// The feature that was modified.
        /// </summary>
        [Required]
        public Guid FeatureId { get; set; }

        /// <summary>
        /// The action performed (e.g., "Created", "Updated", "Enabled", "Disabled", "Deleted").
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// JSON snapshot of the feature's state before the change.
        /// Null for creation actions.
        /// </summary>
        public string? OldValues { get; set; }

        /// <summary>
        /// JSON snapshot of the feature's state after the change.
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
        [ForeignKey("FeatureId")]
        public virtual Feature Feature { get; set; } = null!;
    }
}
