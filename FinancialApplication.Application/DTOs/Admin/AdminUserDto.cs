using System;

namespace FinancialApplication.Application.DTOs.Admin
{
    /// <summary>
    /// Represents a single user row in the admin user management table.
    /// Contains all fields needed for the tabular display including
    /// role, status, subscription info, and security settings.
    /// </summary>
    public class AdminUserDto
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

        // Current subscription summary (from active subscription, if any)
        public string? CurrentPlanName { get; set; }
        public string? SubscriptionStatus { get; set; }
    }
}
