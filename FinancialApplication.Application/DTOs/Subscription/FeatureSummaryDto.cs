using System;

namespace FinancialApplication.Application.DTOs.Subscription
{
    /// <summary>
    /// Data Transfer Object representing a summary of a feature.
    /// Useful for listing features in dropdowns or lightweight lists.
    /// </summary>
    public class FeatureSummaryDto
    {
        public Guid Id { get; set; }
        public string FeatureKey { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
    }
}
