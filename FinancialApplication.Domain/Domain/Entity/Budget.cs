using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinancialApplication.Domain.Domain.Entity
{
    /// <summary>
    /// Represents a user's budget for a specific category and month.
    /// Tracks spending limits and alert thresholds per category.
    /// Primary key: Id (GUID)
    /// Foreign key: UserId (references Users table)
    /// </summary>
    public class Budget
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid UserId { get; set; }

        /// <summary>
        /// Expense category this budget applies to (e.g., "Food", "Transport", "Entertainment").
        /// Must match the category names used in Transactions.
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// Maximum amount allowed for this category per month.
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal MonthlyLimit { get; set; }

        /// <summary>
        /// Percentage threshold at which an alert is triggered (e.g., 80 means alert at 80% spent).
        /// Must be between 1 and 100.
        /// </summary>
        [Required]
        [Range(1, 100)]
        public int AlertThresholdPercent { get; set; } = 80;

        /// <summary>
        /// The month this budget applies to (1-12).
        /// </summary>
        [Required]
        [Range(1, 12)]
        public int Month { get; set; }

        /// <summary>
        /// The year this budget applies to.
        /// </summary>
        [Required]
        public int Year { get; set; }

        /// <summary>
        /// Currency code (e.g., "INR", "USD"). Defaults to INR.
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
