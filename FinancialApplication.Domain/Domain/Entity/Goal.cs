using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FinancialApplication.Domain.Domain.Enums;

namespace FinancialApplication.Domain.Domain.Entity
{
    /// <summary>
    /// Represents a financial goal set by a user.
    /// Primary key: GoalId (GUID)
    /// Foreign key: UserId (references Users table)
    /// </summary>
    public class Goal
    {
        [Key]
        public Guid GoalId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid UserId { get; set; }

        [Required]
        [MaxLength(255)]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Why this goal matters to the user.
        /// </summary>
        [MaxLength(1000)]
        public string? Description { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TargetAmount { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal CurrentAmount { get; set; } = 0;

        [Required]
        public DateTime Deadline { get; set; }

        [Required]
        public GoalStatusEnum Status { get; set; }

        /// <summary>
        /// Emoji icon for UI display (e.g., "🏠", "✈️", "🚗", "💰").
        /// </summary>
        [MaxLength(10)]
        public string? Icon { get; set; }

        /// <summary>
        /// Hex color for UI card (e.g., "#4F46E5").
        /// </summary>
        [MaxLength(20)]
        public string? Color { get; set; }

        /// <summary>
        /// Currency code for the goal amounts.
        /// </summary>
        [Required]
        [MaxLength(10)]
        public string Currency { get; set; } = "INR";

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
    }
}
