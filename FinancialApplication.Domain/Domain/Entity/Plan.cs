using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinancialApplication.Domain.Domain.Entity
{
    /// <summary>
    /// Represents a subscription plan (e.g., Free, Basic, Advanced, Pro).
    /// Plans define pricing and are linked to features via PlanFeatures junction table.
    /// Primary key: Id (GUID)
    /// </summary>
    public class Plan
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Display name of the plan (e.g., "Pro", "Basic").
        /// Must be unique.
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// URL-friendly identifier (e.g., "basic", "pro").
        /// Must be unique, lowercase with hyphens only.
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Slug { get; set; } = string.Empty;

        /// <summary>
        /// Detailed description of what the plan offers.
        /// </summary>
        [MaxLength(1000)]
        public string? Description { get; set; }

        /// <summary>
        /// Monthly subscription price.
        /// Must be >= 0. Free plans have MonthlyPrice = 0.
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal MonthlyPrice { get; set; }

        /// <summary>
        /// Discounted annual price. Null if annual billing is not offered.
        /// Should be <= MonthlyPrice * 12 when provided.
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal? AnnualPrice { get; set; }

        /// <summary>
        /// Currency code for pricing (e.g., "INR", "USD").
        /// </summary>
        [Required]
        [MaxLength(10)]
        public string Currency { get; set; } = "INR";

        /// <summary>
        /// Display order for plans on the pricing page.
        /// Lower values appear first.
        /// </summary>
        [Required]
        public int SortOrder { get; set; } = 0;

        /// <summary>
        /// Whether the plan is currently available for new subscriptions.
        /// Soft-delete: set to false instead of deleting.
        /// </summary>
        [Required]
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Whether this is the default plan assigned to new users (e.g., Free plan).
        /// Only one plan should have IsDefault = true.
        /// </summary>
        [Required]
        public bool IsDefault { get; set; } = false;

        /// <summary>
        /// Number of trial days offered with this plan.
        /// 0 means no trial period.
        /// </summary>
        [Required]
        public int TrialDays { get; set; } = 0;

        /// <summary>
        /// Maximum number of users allowed (for future team/enterprise plans).
        /// Null means unlimited.
        /// </summary>
        public int? MaxUsers { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<PlanFeature> PlanFeatures { get; set; } = new List<PlanFeature>();
        public virtual ICollection<UserSubscription> UserSubscriptions { get; set; } = new List<UserSubscription>();
        public virtual ICollection<PlanAudit> PlanAudits { get; set; } = new List<PlanAudit>();
        public virtual ICollection<PlanPriceHistory> PlanPriceHistories { get; set; } = new List<PlanPriceHistory>();
    }
}
