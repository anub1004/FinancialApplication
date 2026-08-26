using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinancialApplication.Domain.Domain.Entity
{
    /// <summary>
    /// Represents an investment record.
    /// Primary key: InvestmentId (GUID)
    /// Foreign key: UserId (references Users table)
    /// </summary>
    public class Investment
    {
        [Key]
        public Guid InvestmentId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid UserId { get; set; }

        /// <summary>
        /// Name/label of the investment (e.g., "HDFC Equity Fund", "TCS Shares").
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        /// <summary>
        /// Current market value of the investment.
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal CurrentValue { get; set; }

        [Required]
        [MaxLength(50)]
        public string InvestmentType { get; set; } = string.Empty;

        [Required]
        public DateTime StartDate { get; set; } = DateTime.UtcNow;

        public DateTime? EndDate { get; set; }

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Active";

        /// <summary>
        /// Profit/Loss amount (CurrentValue - Amount).
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal? Returns { get; set; }

        /// <summary>
        /// Return on investment percentage.
        /// </summary>
        [Column(TypeName = "decimal(8,2)")]
        public decimal? ReturnPercentage { get; set; }

        /// <summary>
        /// Currency code (e.g., "INR", "USD"). Defaults to INR.
        /// </summary>
        [Required]
        [MaxLength(10)]
        public string Currency { get; set; } = "INR";

        [MaxLength(1000)]
        public string? Notes { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
    }
}
