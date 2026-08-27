using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinancialApplication.Domain.Domain.Entity
{
    /// <summary>
    /// Represents a single tax entry (income, deduction, or capital gain)
    /// used for tax computation and report generation.
    /// Primary key: TaxEntryId (GUID)
    /// Foreign key: UserId (references Users table)
    /// </summary>
    public class TaxEntry
    {
        [Key]
        public Guid TaxEntryId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid UserId { get; set; }

        /// <summary>
        /// Financial year in "YYYY-YY" format (e.g., "2025-26").
        /// </summary>
        [Required]
        [MaxLength(10)]
        public string FinancialYear { get; set; } = string.Empty;

        /// <summary>
        /// Category label (e.g., "Salary", "Section 80C", "Short-term Capital Gains").
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// Descriptive text (e.g., "Gross salary income", "PPF, ELSS, LIC").
        /// </summary>
        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Monetary amount for this entry.
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        /// <summary>
        /// Entry type: "income", "deduction", or "capital_gain".
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string EntryType { get; set; } = string.Empty;

        /// <summary>
        /// Optional tax section reference (e.g., "80C", "80D", "10(14)").
        /// </summary>
        [MaxLength(20)]
        public string? Section { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
    }
}
