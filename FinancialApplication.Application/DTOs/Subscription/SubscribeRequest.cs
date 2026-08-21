using System;
using System.ComponentModel.DataAnnotations;
using FinancialApplication.Domain.Domain.Enums;

namespace FinancialApplication.Application.DTOs.Subscription
{
    /// <summary>
    /// Request DTO for subscribing to a plan.
    /// </summary>
    public class SubscribeRequest
    {
        [Required]
        public Guid PlanId { get; set; }

        [Required]
        public BillingCycleEnum BillingCycle { get; set; }

        [MaxLength(50)]
        public string? PaymentMethod { get; set; }
    }
}
