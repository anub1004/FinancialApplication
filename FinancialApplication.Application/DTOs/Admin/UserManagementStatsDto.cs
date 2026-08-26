using System;
using System.Collections.Generic;

namespace FinancialApplication.Application.DTOs.Admin
{
    /// <summary>
    /// Aggregate statistics for the admin dashboard user management header cards.
    /// Provides counts by status, role, and recent signup trends.
    /// </summary>
    public class UserManagementStatsDto
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int InactiveUsers { get; set; }

        /// <summary>
        /// Breakdown of users by role (role name → count).
        /// </summary>
        public List<RoleCountDto> UsersByRole { get; set; } = new();

        public int NewUsersLast7Days { get; set; }
        public int NewUsersLast30Days { get; set; }

        /// <summary>
        /// Number of users who currently have an active or trial subscription.
        /// </summary>
        public int UsersWithActiveSubscriptions { get; set; }

        public DateTime AsOf { get; set; } = DateTime.UtcNow;
    }

    public class RoleCountDto
    {
        public string RoleName { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}
