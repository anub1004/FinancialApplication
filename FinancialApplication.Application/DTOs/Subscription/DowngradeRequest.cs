using System;
using System.ComponentModel.DataAnnotations;

namespace FinancialApplication.Application.DTOs.Subscription
{
    /// <summary>
    /// Request DTO for scheduling a downgrade to a lower plan at the end of the current billing period.
    /// </summary>
    public class DowngradeRequest
    {
        [Required]
        public Guid TargetPlanId { get; set; }
    }
}
