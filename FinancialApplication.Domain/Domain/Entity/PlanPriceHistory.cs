using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinancialApplication.Domain.Domain.Entity
{
    /// <summary>
    /// Records price snapshots for plans over time.
    /// Enables grandfathering: existing subscribers keep their locked-in price
    /// until renewal, even if admin changes the plan's pricing.
    /// Primary key: Id (GUID)
    /// Foreign key: PlanId
    /// </summary>
    public class PlanPriceHistory
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// The plan whose price was changed.
        /// </summary>
        [Required]
        public Guid PlanId { get; set; }

        /// <summary>
        /// Monthly price at this point in time.
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal MonthlyPrice { get; set; }

        /// <summary>
        /// Annual price at this point in time.
        /// Null if annual billing was not offered.
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal? AnnualPrice { get; set; }

        /// <summary>
        /// When this price became effective.
        /// </summary>
        [Required]
        public DateTime EffectiveFrom { get; set; }

        /// <summary>
        /// When this price was superseded by a new price.
        /// Null if this is the currently active price.
        /// </summary>
        public DateTime? EffectiveTo { get; set; }

        /// <summary>
        /// The admin user who changed the price.
        /// </summary>
        [Required]
        public Guid ChangedBy { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("PlanId")]
        public virtual Plan Plan { get; set; } = null!;
    }
}
