using System;
using FinancialApplication.Domain.Domain.Enums;

namespace FinancialApplication.Application.DTOs.Subscription
{
    /// <summary>
    /// Data Transfer Object representing a subscription history entry.
    /// </summary>
    public class SubscriptionHistoryDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid SubscriptionId { get; set; }
        public SubscriptionActionEnum Action { get; set; }
        public string ActionName => Action.ToString();
        public Guid? FromPlanId { get; set; }
        public string? FromPlanName { get; set; }
        public Guid? ToPlanId { get; set; }
        public string? ToPlanName { get; set; }
        public string? Notes { get; set; }
        public string PerformedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
