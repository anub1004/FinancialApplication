using System;
using System.ComponentModel.DataAnnotations;
using FinancialApplication.Domain.Domain.Enums;

namespace FinancialApplication.Application.DTOs.Subscription
{
    /// <summary>
    /// Request DTO for upgrading an active subscription to a higher plan immediately.
    /// </summary>
    public class UpgradeRequest
    {
        [Required]
        public Guid TargetPlanId { get; set; }

        [Required]
        public BillingCycleEnum BillingCycle { get; set; }

        [MaxLength(50)]
        public string? PaymentMethod { get; set; }
    }
}
