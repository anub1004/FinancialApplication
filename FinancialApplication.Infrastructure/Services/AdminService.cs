using FinancialApp.Infrastructure.Security;
using FinancialApplication.Application.DTOs.Admin;
using FinancialApplication.Application.Interfaces;
using FinancialApplication.Domain.Domain.Entity;
using FinancialApplication.Domain.Domain.Enums;
using FinancialApplication.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinancialApplication.Infrastructure.Services
{
    public class AdminService : IAdminService
    {
        private readonly AppDbContext _context;
        private readonly IJwtTokenGenerator _tokenGenerator;
        private readonly RefreshTokenGenerator _refreshTokenGenerator;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IConfiguration _configuration;
        public AdminService(AppDbContext context, IJwtTokenGenerator tokenGenerator, RefreshTokenGenerator refreshTokenGenerator, IPasswordHasher passwordHasher, IConfiguration configuration)
        {
            _context = context;
            _tokenGenerator = tokenGenerator;
            _refreshTokenGenerator = refreshTokenGenerator;
            _passwordHasher = passwordHasher;
            _configuration = configuration;
        }

        // ════════════════════════════════════════════════════════════════════
        // EXISTING METHODS (preserved as-is)
        // ════════════════════════════════════════════════════════════════════

        public async Task<string> AssignRoleAsync(Guid userId, string roleName)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                throw new KeyNotFoundException($"User with ID {userId} not found.");
            }

            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName && r.IsActive);
            if (role == null)
            {
                throw new InvalidOperationException($"Role '{roleName}' does not exist or is inactive.");
            }

            user.RoleId = role.Id;
            user.UpdatedAt = DateTime.UtcNow;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return $"User '{user.Username}' assigned role '{roleName}' successfully.";
        }
        public async Task<string> RevokeRoleAsync(Guid userId, string roleName)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                throw new KeyNotFoundException($"User with ID {userId} not found.");
            }

            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName && r.IsActive);
            if (role == null)
            {
                throw new InvalidOperationException($"Role '{roleName}' does not exist or is inactive.");
            }

            // Verify the user actually has the role being revoked
            if (user.RoleId != role.Id)
            {
                throw new InvalidOperationException($"User '{user.Username}' does not have role '{roleName}'.");
            }

            // Prevent revoking the default "User" role (RoleId = 1) — it's the base role
            if (role.Id == 1)
            {
                throw new InvalidOperationException("Cannot revoke the default 'User' role.");
            }

            // Reset to default "User" role (RoleId = 1), NOT 0 which would cause FK violation
            user.RoleId = 1;
            user.UpdatedAt = DateTime.UtcNow;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return $"Role '{roleName}' revoked from user '{user.Username}'. User reset to default 'User' role.";
        }
        public async Task<string> DeactivateUserAsync(Guid userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                throw new KeyNotFoundException($"User with ID {userId} not found.");
            }
            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return $"User '{user.Username}' deactivated successfully.";
        }
        public async Task<string> ActivateUserAsync(Guid userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                throw new KeyNotFoundException($"User with ID {userId} not found.");
            }
            user.IsActive = true;
            user.UpdatedAt = DateTime.UtcNow;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return $"User '{user.Username}' activated successfully.";
        }

        // ════════════════════════════════════════════════════════════════════
        // NEW — User Management Methods
        // ════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Returns a paginated, searchable, filterable, sortable list of all users.
        /// </summary>
        public async Task<(List<AdminUserDto> Users, int TotalCount)> GetAllUsersAsync(
            string? search, string? role, bool? isActive,
            string sortBy, string sortOrder, int page, int pageSize)
        {
            var query = _context.Users
                .Include(u => u.Role)
                .AsQueryable();

            // ── Search by username or email ───────────────────────────────────
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(u =>
                    u.Username.ToLower().Contains(term) ||
                    u.Email.ToLower().Contains(term));
            }

            // ── Filter by role name ───────────────────────────────────────────
            if (!string.IsNullOrWhiteSpace(role))
            {
                query = query.Where(u => u.Role.Name == role);
            }

            // ── Filter by active status ───────────────────────────────────────
            if (isActive.HasValue)
            {
                query = query.Where(u => u.IsActive == isActive.Value);
            }

            // ── Total count before pagination ─────────────────────────────────
            var totalCount = await query.CountAsync();

            // ── Sorting ───────────────────────────────────────────────────────
            var isDescending = string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase);

            query = sortBy?.ToLower() switch
            {
                "username"  => isDescending ? query.OrderByDescending(u => u.Username)  : query.OrderBy(u => u.Username),
                "email"     => isDescending ? query.OrderByDescending(u => u.Email)     : query.OrderBy(u => u.Email),
                "role"      => isDescending ? query.OrderByDescending(u => u.Role.Name) : query.OrderBy(u => u.Role.Name),
                "isactive"  => isDescending ? query.OrderByDescending(u => u.IsActive)  : query.OrderBy(u => u.IsActive),
                "updatedat" => isDescending ? query.OrderByDescending(u => u.UpdatedAt) : query.OrderBy(u => u.UpdatedAt),
                _           => isDescending ? query.OrderByDescending(u => u.CreatedAt) : query.OrderBy(u => u.CreatedAt),
            };

            // ── Pagination ────────────────────────────────────────────────────
            var users = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // ── Get active subscription info for these users ──────────────────
            var userIds = users.Select(u => u.Id).ToList();

            var activeSubscriptions = await _context.UserSubscriptions
                .Include(us => us.Plan)
                .Where(us => userIds.Contains(us.UserId) &&
                             (us.Status == SubscriptionStatusEnum.Active ||
                              us.Status == SubscriptionStatusEnum.Trial))
                .GroupBy(us => us.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    // Pick the most recently created active/trial subscription
                    PlanName = g.OrderByDescending(us => us.CreatedAt).First().Plan.Name,
                    Status = g.OrderByDescending(us => us.CreatedAt).First().Status.ToString()
                })
                .ToListAsync();

            var subLookup = activeSubscriptions.ToDictionary(x => x.UserId);

            // ── Map to DTOs ───────────────────────────────────────────────────
            var dtos = users.Select(u =>
            {
                subLookup.TryGetValue(u.Id, out var sub);
                return new AdminUserDto
                {
                    Id                 = u.Id,
                    Username           = u.Username,
                    Email              = u.Email,
                    RoleId             = u.RoleId,
                    RoleName           = u.Role?.Name ?? "Unknown",
                    IsActive           = u.IsActive,
                    CreatedAt          = u.CreatedAt,
                    UpdatedAt          = u.UpdatedAt,
                    HasGoogleLogin     = !string.IsNullOrEmpty(u.GoogleId),
                    HasTotpConfigured  = u.IsTotpConfigured,
                    ProfilePicture     = u.ProfilePicture,
                    CurrentPlanName    = sub?.PlanName,
                    SubscriptionStatus = sub?.Status
                };
            }).ToList();

            return (dtos, totalCount);
        }

        /// <summary>
        /// Returns detailed information for a single user including subscription history and payment summary.
        /// </summary>
        public async Task<AdminUserDetailDto?> GetUserDetailAsync(Guid userId)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return null;

            // Load subscriptions
            var subscriptions = await _context.UserSubscriptions
                .Include(us => us.Plan)
                .Where(us => us.UserId == userId)
                .OrderByDescending(us => us.CreatedAt)
                .Select(us => new UserSubscriptionSummaryDto
                {
                    SubscriptionId = us.Id,
                    PlanName       = us.Plan.Name,
                    Status         = us.Status.ToString(),
                    BillingCycle   = us.BillingCycle.ToString(),
                    StartDate      = us.StartDate,
                    EndDate        = us.EndDate,
                    AutoRenew      = us.AutoRenew,
                    CancelledAt    = us.CancelledAt
                })
                .ToListAsync();

            // Payment summary
            var paymentInfo = await _context.Payments
                .Where(p => p.UserId == userId)
                .GroupBy(p => p.UserId)
                .Select(g => new
                {
                    Count = g.Count(),
                    Total = g.Sum(p => p.Amount)
                })
                .FirstOrDefaultAsync();

            return new AdminUserDetailDto
            {
                Id                = user.Id,
                Username          = user.Username,
                Email             = user.Email,
                RoleId            = user.RoleId,
                RoleName          = user.Role?.Name ?? "Unknown",
                IsActive          = user.IsActive,
                CreatedAt         = user.CreatedAt,
                UpdatedAt         = user.UpdatedAt,
                HasGoogleLogin    = !string.IsNullOrEmpty(user.GoogleId),
                HasTotpConfigured = user.IsTotpConfigured,
                ProfilePicture    = user.ProfilePicture,
                Subscriptions     = subscriptions,
                PaymentCount      = paymentInfo?.Count ?? 0,
                TotalPayments     = paymentInfo?.Total ?? 0m
            };
        }

        /// <summary>
        /// Returns aggregate user statistics for the admin dashboard.
        /// </summary>
        public async Task<UserManagementStatsDto> GetUserStatsAsync()
        {
            var now = DateTime.UtcNow;

            var totalUsers  = await _context.Users.CountAsync();
            var activeUsers = await _context.Users.CountAsync(u => u.IsActive);

            var usersByRole = await _context.Users
                .Include(u => u.Role)
                .GroupBy(u => u.Role.Name)
                .Select(g => new RoleCountDto
                {
                    RoleName = g.Key,
                    Count    = g.Count()
                })
                .ToListAsync();

            var newLast7  = await _context.Users.CountAsync(u => u.CreatedAt >= now.AddDays(-7));
            var newLast30 = await _context.Users.CountAsync(u => u.CreatedAt >= now.AddDays(-30));

            var withActiveSub = await _context.UserSubscriptions
                .Where(us => us.Status == SubscriptionStatusEnum.Active ||
                             us.Status == SubscriptionStatusEnum.Trial)
                .Select(us => us.UserId)
                .Distinct()
                .CountAsync();

            return new UserManagementStatsDto
            {
                TotalUsers                  = totalUsers,
                ActiveUsers                 = activeUsers,
                InactiveUsers               = totalUsers - activeUsers,
                UsersByRole                 = usersByRole,
                NewUsersLast7Days           = newLast7,
                NewUsersLast30Days          = newLast30,
                UsersWithActiveSubscriptions = withActiveSub,
                AsOf                        = now
            };
        }

        /// <summary>
        /// Toggles a user's active status (enable/disable).
        /// </summary>
        public async Task<string> UpdateUserStatusAsync(Guid userId, bool isActive)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                throw new KeyNotFoundException($"User with ID {userId} not found.");

            user.IsActive  = isActive;
            user.UpdatedAt = DateTime.UtcNow;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            var status = isActive ? "activated" : "deactivated";
            return $"User '{user.Username}' {status} successfully.";
        }

        /// <summary>
        /// Changes a user's role by role name.
        /// </summary>
        public async Task<string> UpdateUserRoleAsync(Guid userId, string roleName)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                throw new KeyNotFoundException($"User with ID {userId} not found.");

            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName && r.IsActive);
            if (role == null)
                throw new InvalidOperationException($"Role '{roleName}' does not exist or is inactive.");

            var previousRoleId = user.RoleId;
            user.RoleId    = role.Id;
            user.UpdatedAt = DateTime.UtcNow;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return $"User '{user.Username}' role changed to '{roleName}' successfully.";
        }

        /// <summary>
        /// Soft-deletes a user by setting IsActive = false.
        /// Preserves data integrity by not removing the record from the database.
        /// </summary>
        public async Task<string> DeleteUserAsync(Guid userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                throw new KeyNotFoundException($"User with ID {userId} not found.");

            // Soft delete — preserve referential integrity
            user.IsActive  = false;
            user.UpdatedAt = DateTime.UtcNow;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return $"User '{user.Username}' has been deleted (deactivated) successfully.";
        }
    }
}