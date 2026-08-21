using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinancialApplication.Domain.Domain.Entity
{
    /// <summary>
    /// Junction table linking Plans to Features (many-to-many).
    /// A plan can have many features, and a feature can belong to many plans.
    /// Primary key: Id (GUID)
    /// Foreign keys: PlanId, FeatureId
    /// </summary>
    public class PlanFeature
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// The plan that includes this feature.
        /// </summary>
        [Required]
        public Guid PlanId { get; set; }

        /// <summary>
        /// The feature included in this plan.
        /// </summary>
        [Required]
        public Guid FeatureId { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("PlanId")]
        public virtual Plan Plan { get; set; } = null!;

        [ForeignKey("FeatureId")]
        public virtual Feature Feature { get; set; } = null!;
    }
}
