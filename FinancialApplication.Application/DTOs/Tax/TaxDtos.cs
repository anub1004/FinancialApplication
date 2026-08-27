using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FinancialApplication.Application.DTOs.Tax
{
    // ── Create ───────────────────────────────────────────────────────────────
    public class CreateTaxEntryDto
    {
        [Required, MaxLength(10)]
        public string FinancialYear { get; set; } = "2025-26";

        [Required, MaxLength(100)]
        public string Category { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required, Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0.")]
        public decimal Amount { get; set; }

        /// <summary>
        /// Must be one of: "income", "deduction", "capital_gain"
        /// </summary>
        [Required, MaxLength(20)]
        public string EntryType { get; set; } = string.Empty;

        /// <summary>
        /// Optional section reference (e.g., "80C", "80D", "10(14)")
        /// </summary>
        [MaxLength(20)]
        public string? Section { get; set; }
    }

    // ── Update ───────────────────────────────────────────────────────────────
    public class UpdateTaxEntryDto
    {
        [MaxLength(100)]
        public string? Category { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal? Amount { get; set; }

        [MaxLength(20)]
        public string? EntryType { get; set; }

        [MaxLength(20)]
        public string? Section { get; set; }
    }

    // ── Response ─────────────────────────────────────────────────────────────
    public class TaxEntryDto
    {
        public Guid TaxEntryId { get; set; }
        public string FinancialYear { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string EntryType { get; set; } = string.Empty;
        public string? Section { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    // ── Tax Computation Result ───────────────────────────────────────────────
    public class TaxComputationDto
    {
        public string FinancialYear { get; set; } = string.Empty;

        // ── Income breakdown ──
        public decimal GrossIncome { get; set; }
        public decimal CapitalGains { get; set; }
        public decimal ShortTermCapitalGains { get; set; }
        public decimal LongTermCapitalGains { get; set; }
        public decimal TotalDeductions { get; set; }
        public List<TaxEntryDto> Entries { get; set; } = new();

        // ── New Regime ──
        public TaxRegimeResult NewRegime { get; set; } = new();

        // ── Old Regime ──
        public TaxRegimeResult OldRegime { get; set; } = new();

        /// <summary>
        /// "new" or "old" — whichever results in lower tax.
        /// </summary>
        public string RecommendedRegime { get; set; } = "new";
        public decimal TaxSaved { get; set; }
    }

    public class TaxRegimeResult
    {
        public string RegimeName { get; set; } = string.Empty;
        public decimal StandardDeduction { get; set; }
        public decimal TotalDeductions { get; set; }
        public decimal TaxableIncome { get; set; }
        public decimal IncomeTax { get; set; }
        public decimal StcgTax { get; set; }
        public decimal LtcgTax { get; set; }
        public decimal Surcharge { get; set; }
        public decimal HealthEducationCess { get; set; }
        public decimal Rebate87A { get; set; }
        public decimal TotalTax { get; set; }
        public List<TaxSlabDetailDto> SlabBreakdown { get; set; } = new();
    }

    public class TaxSlabDetailDto
    {
        public string Slab { get; set; } = string.Empty;
        public decimal Rate { get; set; }
        public decimal TaxableAmount { get; set; }
        public decimal Tax { get; set; }
    }
}
