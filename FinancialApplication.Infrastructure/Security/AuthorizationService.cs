using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace FinancialApp.Infrastructure.Security
{
    /// <summary>
    /// Authorization service for checking user permissions and roles.
    /// Implements the code-based authorization per architecture.
    /// No permission table needed - all permissions defined in code.
    /// </summary>
    public interface IAuthorizationService
    {
        /// <summary>
        /// Gets the current user ID from HTTP context.
        /// </summary>
        Guid GetCurrentUserId();

        /// <summary>
        /// Gets the current user role from claims.
        /// </summary>
        string GetCurrentUserRole();

        /// <summary>
        /// Checks if current user is Admin.
        /// </summary>
        bool IsAdmin();

        /// <summary>
        /// Checks if current user is Manager.
        /// </summary>
        bool IsManager();

        /// <summary>
        /// Checks if current user is Auditor.
        /// </summary>
        bool IsAuditor();

        /// <summary>
        /// Checks if current user is Admin or Manager.
        /// </summary>
        bool IsAdminOrManager();

        /// <summary>
        /// Checks if user can view another user's data.
        /// Own data or Admin/Manager/Auditor role.
        /// </summary>
        bool CanViewUserData(Guid userId);

        /// <summary>
        /// Checks if user can edit another user's data.
        /// Own data or Admin/Manager role.
        /// </summary>
        bool CanEditUserData(Guid userId);

        /// <summary>
        /// Throws if user cannot view the specified user's data.
        /// </summary>
        void EnsureCanViewUserData(Guid userId);

        /// <summary>
        /// Throws if user cannot edit the specified user's data.
        /// </summary>
        void EnsureCanEditUserData(Guid userId);

        /// <summary>
        /// Checks if user has a specific permission claim.
        /// </summary>
        bool HasPermission(string permission);

        /// <summary>
        /// Gets all permission claims for the current user.
        /// </summary>
        IEnumerable<string> GetPermissions();
    }

    /// <summary>
    /// Custom exception for authorization failures.
    /// </summary>
    public class AuthorizationException : Exception
    {
        public AuthorizationException(string message) : base(message) { }
    }

    /// <summary>
    /// Implementation of authorization service.
    /// </summary>
    public class AuthorizationService : IAuthorizationService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthorizationService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Gets current user ID from NameIdentifier claim.
        /// </summary>
        public Guid GetCurrentUserId()
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User
                .FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
            {
                throw new AuthorizationException("User ID claim not found");
            }

            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                throw new AuthorizationException("Invalid user ID format");
            }

            return userId;
        }

        /// <summary>
        /// Gets current user role from Role claim.
        /// </summary>
        public string GetCurrentUserRole()
        {
            return _httpContextAccessor.HttpContext?.User
                .FindFirst(ClaimTypes.Role)?.Value ?? "User";
        }

        /// <summary>
        /// Checks if current user is Admin.
        /// </summary>
        public bool IsAdmin()
        {
            return GetCurrentUserRole() == "Admin";
        }

        /// <summary>
        /// Checks if current user is Manager.
        /// </summary>
        public bool IsManager()
        {
            return GetCurrentUserRole() == "Manager";
        }

        /// <summary>
        /// Checks if current user is Auditor.
        /// </summary>
        public bool IsAuditor()
        {
            return GetCurrentUserRole() == "Auditor";
        }

        /// <summary>
        /// Checks if current user is Admin or Manager.
        /// </summary>
        public bool IsAdminOrManager()
        {
            var role = GetCurrentUserRole();
            return role == "Admin" || role == "Manager";
        }

        /// <summary>
        /// Checks if user can view another user's data.
        /// Rules:
        /// - Can always view own data
        /// - Admin/Manager/Auditor can view all data
        /// </summary>
        public bool CanViewUserData(Guid userId)
        {
            var currentUserId = GetCurrentUserId();
            var role = GetCurrentUserRole();

            // Can view own data
            if (currentUserId == userId)
                return true;

            // Admin, Manager, Auditor can view all
            return new[] { "Admin", "Manager", "Auditor" }.Contains(role);
        }

        /// <summary>
        /// Checks if user can edit another user's data.
        /// Rules:
        /// - Can always edit own data
        /// - Admin can edit all data
        /// - Manager can edit all data
        /// - Regular users cannot edit other users' data
        /// </summary>
        public bool CanEditUserData(Guid userId)
        {
            var currentUserId = GetCurrentUserId();
            var role = GetCurrentUserRole();

            // Can edit own data
            if (currentUserId == userId)
                return true;

            // Only Admin and Manager can edit other users' data
            return role == "Admin" || role == "Manager";
        }

        /// <summary>
        /// Throws exception if user cannot view data.
        /// </summary>
        public void EnsureCanViewUserData(Guid userId)
        {
            if (!CanViewUserData(userId))
            {
                throw new AuthorizationException(
                    $"You don't have permission to view data for user {userId}");
            }
        }

        /// <summary>
        /// Throws exception if user cannot edit data.
        /// </summary>
        public void EnsureCanEditUserData(Guid userId)
        {
            if (!CanEditUserData(userId))
            {
                throw new AuthorizationException(
                    $"You don't have permission to edit data for user {userId}");
            }
        }

        /// <summary>
        /// Checks if user has a specific permission claim.
        /// </summary>
        public bool HasPermission(string permission)
        {
            var user = _httpContextAccessor.HttpContext?.User;
            return user?.FindAll("permission")
                .Any(c => c.Value == permission) ?? false;
        }

        /// <summary>
        /// Gets all permission claims for current user.
        /// </summary>
        public IEnumerable<string> GetPermissions()
        {
            return _httpContextAccessor.HttpContext?.User
                .FindAll("permission")
                .Select(c => c.Value) ?? Enumerable.Empty<string>();
        }
    }
}
