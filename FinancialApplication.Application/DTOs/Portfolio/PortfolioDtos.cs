using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FinancialApplication.Application.DTOs.Portfolio
{
    // ── Create ───────────────────────────────────────────────────────────────
    public class CreatePortfolioAssetDto
    {
        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string AssetType { get; set; } = string.Empty;

        [Required, Range(0.01, double.MaxValue, ErrorMessage = "Invested amount must be greater than 0.")]
        public decimal InvestedAmount { get; set; }

        [Required, Range(0, double.MaxValue)]
        public decimal CurrentValue { get; set; }

        [MaxLength(10)]
        public string Color { get; set; } = "#6366f1";

        [MaxLength(500)]
        public string? Notes { get; set; }

        [Required]
        public DateTime PurchaseDate { get; set; }

        [MaxLength(10)]
        public string Currency { get; set; } = "INR";
    }

    // ── Update ───────────────────────────────────────────────────────────────
    public class UpdatePortfolioAssetDto
    {
        [MaxLength(200)]
        public string? Name { get; set; }

        [MaxLength(50)]
        public string? AssetType { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal? InvestedAmount { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? CurrentValue { get; set; }

        [MaxLength(10)]
        public string? Color { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        public DateTime? PurchaseDate { get; set; }

        [MaxLength(10)]
        public string? Currency { get; set; }
    }

    // ── Response ─────────────────────────────────────────────────────────────
    public class PortfolioAssetDto
    {
        public Guid PortfolioAssetId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string AssetType { get; set; } = string.Empty;
        public decimal InvestedAmount { get; set; }
        public decimal CurrentValue { get; set; }
        public decimal AllocationPercentage { get; set; }
        public decimal ProfitLoss { get; set; }
        public decimal ReturnPercentage { get; set; }
        public string Color { get; set; } = "#6366f1";
        public string? Notes { get; set; }
        public DateTime PurchaseDate { get; set; }
        public string Currency { get; set; } = "INR";
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    // ── Summary ──────────────────────────────────────────────────────────────
    public class PortfolioSummaryDto
    {
        public decimal TotalInvested { get; set; }
        public decimal TotalCurrentValue { get; set; }
        public decimal TotalReturns { get; set; }
        public decimal OverallReturnPercentage { get; set; }
        public int AssetCount { get; set; }
        public string Currency { get; set; } = "INR";
        public List<AssetTypeBreakdownDto> ByType { get; set; } = new();
    }

    public class AssetTypeBreakdownDto
    {
        public string AssetType { get; set; } = string.Empty;
        public decimal TotalInvested { get; set; }
        public decimal TotalCurrentValue { get; set; }
        public decimal AllocationPercentage { get; set; }
        public int Count { get; set; }
    }
}
