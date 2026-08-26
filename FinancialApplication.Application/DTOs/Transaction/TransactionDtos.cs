using System;
using System.ComponentModel.DataAnnotations;
using FinancialApplication.Domain.Domain.Enums;

namespace FinancialApplication.Application.DTOs.Transaction
{
    // ── Create ───────────────────────────────────────────────────────────────
    public class CreateTransactionDto
    {
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0.")]
        public decimal Amount { get; set; }

        [Required]
        [MaxLength(100)]
        public string Category { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public DateTime TransactionDate { get; set; }

        [Required]
        public TransactionTypeEnum TransactionType { get; set; }

        [MaxLength(10)]
        public string Currency { get; set; } = "INR";

        [MaxLength(50)]
        public string? PaymentMethod { get; set; }

        public bool IsRecurring { get; set; } = false;
    }

    // ── Update ───────────────────────────────────────────────────────────────
    public class UpdateTransactionDto
    {
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0.")]
        public decimal? Amount { get; set; }

        [MaxLength(100)]
        public string? Category { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        public DateTime? TransactionDate { get; set; }

        public TransactionTypeEnum? TransactionType { get; set; }

        [MaxLength(10)]
        public string? Currency { get; set; }

        [MaxLength(50)]
        public string? PaymentMethod { get; set; }

        public bool? IsRecurring { get; set; }
    }

    // ── Response ─────────────────────────────────────────────────────────────
    public class TransactionDto
    {
        public Guid TransactionId { get; set; }
        public decimal Amount { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime TransactionDate { get; set; }
        public TransactionTypeEnum TransactionType { get; set; }
        public string TransactionTypeName { get; set; } = string.Empty;
        public string Currency { get; set; } = "INR";
        public string? PaymentMethod { get; set; }
        public bool IsRecurring { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    // ── Summary ──────────────────────────────────────────────────────────────
    public class TransactionSummaryDto
    {
        public decimal TotalIncome { get; set; }
        public decimal TotalExpense { get; set; }
        public decimal NetBalance { get; set; }
        public int TransactionCount { get; set; }
        public string Currency { get; set; } = "INR";
        public int Month { get; set; }
        public int Year { get; set; }
    }

    // ── Category Info ────────────────────────────────────────────────────────
    public class CategoryDto
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // "Income", "Expense", "Both"
        public string Icon { get; set; } = string.Empty;
        public bool IsCustom { get; set; }
    }
}
