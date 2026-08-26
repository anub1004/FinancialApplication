using System;
using System.ComponentModel.DataAnnotations;

namespace FinancialApplication.Application.DTOs.Investment
{
    // ── Create ───────────────────────────────────────────────────────────────
    public class CreateInvestmentDto
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0.")]
        public decimal Amount { get; set; }

        public decimal? CurrentValue { get; set; }

        [Required]
        [MaxLength(50)]
        public string InvestmentType { get; set; } = string.Empty;

        [Required]
        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        [MaxLength(10)]
        public string Currency { get; set; } = "INR";

        [MaxLength(1000)]
        public string? Notes { get; set; }
    }

    // ── Update ───────────────────────────────────────────────────────────────
    public class UpdateInvestmentDto
    {
        [MaxLength(200)]
        public string? Name { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal? Amount { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? CurrentValue { get; set; }

        [MaxLength(50)]
        public string? InvestmentType { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        [MaxLength(50)]
        public string? Status { get; set; }

        [MaxLength(10)]
        public string? Currency { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }
    }

    // ── Response ─────────────────────────────────────────────────────────────
    public class InvestmentDto
    {
        public Guid InvestmentId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal CurrentValue { get; set; }
        public string InvestmentType { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal? Returns { get; set; }
        public decimal? ReturnPercentage { get; set; }
        public string Currency { get; set; } = "INR";
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    // ── Portfolio Summary ────────────────────────────────────────────────────
    public class InvestmentSummaryDto
    {
        public decimal TotalInvested { get; set; }
        public decimal TotalCurrentValue { get; set; }
        public decimal TotalReturns { get; set; }
        public decimal OverallReturnPercentage { get; set; }
        public int ActiveCount { get; set; }
        public int TotalCount { get; set; }
        public string Currency { get; set; } = "INR";
        public List<InvestmentTypeBreakdownDto> ByType { get; set; } = new();
    }

    public class InvestmentTypeBreakdownDto
    {
        public string InvestmentType { get; set; } = string.Empty;
        public decimal TotalInvested { get; set; }
        public decimal TotalCurrentValue { get; set; }
        public int Count { get; set; }
    }
}
