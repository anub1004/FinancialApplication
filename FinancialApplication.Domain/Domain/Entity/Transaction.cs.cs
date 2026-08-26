using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FinancialApplication.Domain.Domain.Enums;

namespace FinancialApplication.Domain.Domain.Entity
{
    /// <summary>
    /// Represents a financial transaction (income or expense).
    /// Primary key: TransactionId (GUID)
    /// Foreign key: UserId (references Users table)
    /// </summary>
    public class Transaction
    {
        [Key]
        public Guid TransactionId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid UserId { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        [MaxLength(100)]
        public string Category { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

        [Required]
        public TransactionTypeEnum TransactionType { get; set; }

        /// <summary>
        /// Currency code (e.g., "INR", "USD", "EUR"). Defaults to INR.
        /// </summary>
        [Required]
        [MaxLength(10)]
        public string Currency { get; set; } = "INR";

        /// <summary>
        /// Payment method used (e.g., "Cash", "UPI", "Card", "NetBanking").
        /// </summary>
        [MaxLength(50)]
        public string? PaymentMethod { get; set; }

        /// <summary>
        /// Whether this is a recurring transaction (monthly rent, salary, etc.).
        /// </summary>
        public bool IsRecurring { get; set; } = false;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
    }
}
