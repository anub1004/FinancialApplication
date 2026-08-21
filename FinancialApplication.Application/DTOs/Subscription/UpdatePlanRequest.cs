using System;
using System.ComponentModel.DataAnnotations;

namespace FinancialApplication.Application.DTOs.Subscription
{
    /// <summary>
    /// Request DTO for updating an existing subscription plan.
    /// Name and Slug are omitted or handled carefully to avoid breaking paths,
    /// but usually Slug and Name are editable with unique check.
    /// Price updates should go through UpdatePricingRequest to record price history.
    /// </summary>
    public class UpdatePlanRequest
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        [RegularExpression(@"^[a-z0-9-]+$", ErrorMessage = "Slug must be URL-friendly (lowercase letters, numbers, and hyphens only).")]
        public string Slug { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Required]
        [MaxLength(10)]
        public string Currency { get; set; } = "INR";

        public int SortOrder { get; set; } = 0;
        public bool IsActive { get; set; } = true;

        [Range(0, 365, ErrorMessage = "Trial days must be between 0 and 365.")]
        public int TrialDays { get; set; } = 0;

        [Range(1, 100000, ErrorMessage = "Max users must be greater than or equal to 1.")]
        public int? MaxUsers { get; set; }
    }
}
