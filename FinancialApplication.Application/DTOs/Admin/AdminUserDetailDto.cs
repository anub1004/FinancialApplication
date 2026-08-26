using System;
using System.Collections.Generic;

namespace FinancialApplication.Application.DTOs.Admin
{
    /// <summary>
    /// Detailed single-user view for admin user detail modal / expanded row.
    /// Extends the table data with full subscription history and payment summary.
    /// </summary>
    public class AdminUserDetailDto
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        // Role info
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;

        // Account status
        public bool IsActive { get; set; }

        // Timestamps
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Security / login method indicators
        public bool HasGoogleLogin { get; set; }
        public bool HasTotpConfigured { get; set; }
        public string? ProfilePicture { get; set; }

        // Full subscription list
        public List<UserSubscriptionSummaryDto> Subscriptions { get; set; } = new();

        // Payment summary
        public int PaymentCount { get; set; }
        public decimal TotalPayments { get; set; }
    }

    /// <summary>
    /// Lightweight subscription record for the user detail view.
    /// </summary>
    public class UserSubscriptionSummaryDto
    {
        public Guid SubscriptionId { get; set; }
        public string PlanName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string BillingCycle { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool AutoRenew { get; set; }
        public DateTime? CancelledAt { get; set; }
    }
}
