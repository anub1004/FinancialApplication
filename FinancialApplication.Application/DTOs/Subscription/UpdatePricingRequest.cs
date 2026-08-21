using System;
using System.ComponentModel.DataAnnotations;

namespace FinancialApplication.Application.DTOs.Subscription
{
    /// <summary>
    /// Request DTO for updating the pricing of a plan.
    /// This triggers a PlanPriceHistory entry for grandfathering.
    /// </summary>
    public class UpdatePricingRequest
    {
        [Required]
        [Range(0, 1000000, ErrorMessage = "Monthly price must be greater than or equal to 0.")]
        public decimal MonthlyPrice { get; set; }

        [Range(0, 10000000, ErrorMessage = "Annual price must be greater than or equal to 0.")]
        public decimal? AnnualPrice { get; set; }
    }
}
