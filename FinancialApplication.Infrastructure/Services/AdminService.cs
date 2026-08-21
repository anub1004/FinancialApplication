using FinancialApp.Infrastructure.Security;
using FinancialApplication.Application.Interfaces;
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
    }
}