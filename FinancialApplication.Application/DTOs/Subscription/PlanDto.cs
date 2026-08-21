using System;
using System.Collections.Generic;

namespace FinancialApplication.Application.DTOs.Subscription
{
    /// <summary>
    /// Data Transfer Object representing a plan with all properties and its assigned features.
    /// </summary>
    public class PlanDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal MonthlyPrice { get; set; }
        public decimal? AnnualPrice { get; set; }
        public string Currency { get; set; } = "INR";
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
        public bool IsDefault { get; set; }
        public int TrialDays { get; set; }
        public int? MaxUsers { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<FeatureSummaryDto> Features { get; set; } = new List<FeatureSummaryDto>();
    }
}
