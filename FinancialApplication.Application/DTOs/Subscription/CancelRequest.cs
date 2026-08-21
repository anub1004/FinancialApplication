using System;
using System.ComponentModel.DataAnnotations;

namespace FinancialApplication.Application.DTOs.Subscription
{
    /// <summary>
    /// Request DTO for cancelling a subscription.
    /// </summary>
    public class CancelRequest
    {
        [MaxLength(500)]
        public string? CancelReason { get; set; }
    }
}
