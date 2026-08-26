using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FinancialApplication.Application.DTOs.Admin;

namespace FinancialApplication.Application.Interfaces
{
   public interface IAdminService
    {
        // Existing role management
        Task<string> AssignRoleAsync(Guid userId, string roleName);
        Task<string> RevokeRoleAsync(Guid userId, string roleName);
        Task<string> DeactivateUserAsync(Guid userId);
        Task<string> ActivateUserAsync(Guid userId);

        // ── User Management (new) ────────────────────────────────────────────
        Task<(List<AdminUserDto> Users, int TotalCount)> GetAllUsersAsync(
            string? search, string? role, bool? isActive,
            string sortBy, string sortOrder, int page, int pageSize);

        Task<AdminUserDetailDto?> GetUserDetailAsync(Guid userId);
        Task<UserManagementStatsDto> GetUserStatsAsync();
        Task<string> UpdateUserStatusAsync(Guid userId, bool isActive);
        Task<string> UpdateUserRoleAsync(Guid userId, string roleName);
        Task<string> DeleteUserAsync(Guid userId);
    }
}

