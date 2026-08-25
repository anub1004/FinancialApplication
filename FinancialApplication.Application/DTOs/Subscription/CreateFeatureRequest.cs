using System;
using System.ComponentModel.DataAnnotations;

namespace FinancialApplication.Application.DTOs.Subscription
{
    /// <summary>
    /// Request DTO for creating a new feature.
    /// </summary>
    public class CreateFeatureRequest
    {
        [Required]
        [MaxLength(100)]
        [RegularExpression(@"^[a-z][a-z0-9_]*$", ErrorMessage = "FeatureKey must be in snake_case (lowercase letters, numbers, and underscores, starting with a letter).")]
        public string FeatureKey { get; set; } = string.Empty;

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
