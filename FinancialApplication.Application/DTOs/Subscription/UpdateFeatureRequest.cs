using System;
using System.ComponentModel.DataAnnotations;

namespace FinancialApplication.Application.DTOs.Subscription
{
    /// <summary>
    /// Request DTO for updating an existing feature.
    /// FeatureKey is omitted as it is immutable once created.
    /// </summary>
    public class UpdateFeatureRequest
    {
        [Required]
        [MaxLength(200)]
        public string DisplayName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(100)]
        public string? Category { get; set; }

        public bool IsActive { get; set; } = true;
        public int SortOrder { get; set; } = 0;
    }
}
