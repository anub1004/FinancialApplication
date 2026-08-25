using System;

namespace FinancialApplication.Application.DTOs.Subscription
{
    /// <summary>
    /// Data Transfer Object representing a feature with its full properties.
    /// </summary>
    public class FeatureDto
    {
        public Guid Id { get; set; }
        public string FeatureKey { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Category { get; set; }
        public bool IsActive { get; set; }
        public int SortOrder { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
