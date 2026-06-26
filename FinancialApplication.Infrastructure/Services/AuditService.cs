using FinancialApplication.Application.Interfaces;
using FinancialApplication.Domain.Domain.Entity;
using FinancialApplication.Infrastructure.Data;
using System;
using System.Threading.Tasks;

namespace FinancialApp.Infrastructure.Services
{
    public class AuditService : IAuditService
    {
        private readonly AppDbContext _context;

        public AuditService(AppDbContext context)
        {
            _context = context;
        }

        public async Task LogActionAsync(Guid userId, string action, string entityName, string entityId, string details = null)
        {
            var auditLog = new AuditLog
            {
                UserId = userId,
                Action = action,
                EntityName = entityName,
                EntityId = entityId,
                Timestamp = DateTime.UtcNow
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }

        public Task LogLoginAsync(Guid userId, string username)
        {
            return LogActionAsync(userId, "Login", "User", userId.ToString(), $"User {username} logged in.");
        }

        public Task LogLogoutAsync(Guid userId, string username)
        {
            
            return LogActionAsync(userId, "Logout", "User", userId.ToString(), $"User {username} logged out.");
        }

        public Task LogFailedLoginAsync(string username, string reason)
        {
            return LogActionAsync(Guid.Empty, "FailedLogin", "User", username, reason);
        }

        public Task LogAuthorizationFailureAsync(Guid userId, string action, string resource, string reason)
        {
            return LogActionAsync(userId, $"AuthorizationFailure:{action}", resource, resource, reason);
        }
    }
}
