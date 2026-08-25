using System;
using FinancialApplication.Domain.Domain.Enums;

namespace FinancialApplication.Application.DTOs.Subscription
{
    /// <summary>
    /// Data Transfer Object representing details of a user's subscription.
    /// </summary>
    public class UserSubscriptionDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid PlanId { get; set; }
        public string PlanName { get; set; } = string.Empty;
        public string PlanSlug { get; set; } = string.Empty;
        public SubscriptionStatusEnum Status { get; set; }
        public string StatusName => Status.ToString();
        public BillingCycleEnum BillingCycle { get; set; }
        public string BillingCycleName => BillingCycle.ToString();
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime? TrialEndDate { get; set; }
        public DateTime? NextRenewalDate { get; set; }
        public DateTime? CancelledAt { get; set; }
        public string? CancelReason { get; set; }
        public bool AutoRenew { get; set; }
        public Guid? ScheduledPlanId { get; set; }
        public string? ScheduledPlanName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
