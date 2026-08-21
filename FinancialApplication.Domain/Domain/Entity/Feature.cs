using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FinancialApplication.Domain.Domain.Entity
{
    /// <summary>
    /// Represents a feature that can be assigned to subscription plans.
    /// Features use a stable FeatureKey (snake_case) for code references,
    /// and a user-friendly DisplayName for UI display.
    /// Primary key: Id (GUID)
    /// </summary>
    public class Feature
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Machine-readable, stable identifier for this feature (e.g., "export_pdf", "ai_suggestions").
        /// Must be snake_case, unique, and immutable once created.
        /// Used in code via [RequireFeature("export_pdf")] and useFeature("export_pdf").
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string FeatureKey { get; set; } = string.Empty;

        /// <summary>
        /// Human-readable name shown in UI (e.g., "Export PDF").
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Detailed description of the feature.
        /// </summary>
        [MaxLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// Category grouping for features (e.g., "Reports", "Analytics", "Core").
        /// Used for organizing features in the admin UI.
        /// </summary>
        [MaxLength(100)]
        public string? Category { get; set; }

        /// <summary>
        /// Whether the feature is currently active.
        /// Inactive features are not resolved even if assigned to a plan.
        /// </summary>
        [Required]
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Display order within a category.
        /// Lower values appear first.
        /// </summary>
        [Required]
        public int SortOrder { get; set; } = 0;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<PlanFeature> PlanFeatures { get; set; } = new List<PlanFeature>();
        public virtual ICollection<FeatureAudit> FeatureAudits { get; set; } = new List<FeatureAudit>();
    }
}
