using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinancialApplication.Domain.Domain.Entity
{
    /// <summary>
    /// Represents a single asset in a user's investment portfolio.
    /// Primary key: PortfolioAssetId (GUID)
    /// Foreign key: UserId (references Users table)
    /// </summary>
    public class PortfolioAsset
    {
        [Key]
        public Guid PortfolioAssetId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid UserId { get; set; }

        /// <summary>
        /// Name/label of the asset (e.g., "Nifty 50 Index Fund", "HDFC Bank").
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Asset category: Equity, Mutual Fund, Gold, Fixed Income, International, Crypto, Real Estate.
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string AssetType { get; set; } = string.Empty;

        /// <summary>
        /// Total amount invested in this asset.
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal InvestedAmount { get; set; }

        /// <summary>
        /// Current market value of the asset.
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal CurrentValue { get; set; }

        /// <summary>
        /// Allocation percentage within the portfolio (auto-calculated).
        /// </summary>
        [Column(TypeName = "decimal(5,2)")]
        public decimal? AllocationPercentage { get; set; }

        /// <summary>
        /// Hex color for UI display (e.g., "#6366f1").
        /// </summary>
        [MaxLength(10)]
        public string Color { get; set; } = "#6366f1";

        /// <summary>
        /// Optional notes about the asset.
        /// </summary>
        [MaxLength(500)]
        public string? Notes { get; set; }

        /// <summary>
        /// Date the asset was purchased.
        /// </summary>
        [Required]
        public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;

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
