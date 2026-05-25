using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinancialApplication.Application.Interfaces
{
    public interface IAuditService
    {
        /// <summary>
        /// Logs an action to the audit trail.
        /// </summary>
        Task LogActionAsync(
            Guid userId,
            string action,
            string entityName,
            string entityId,
            string details = null);

        /// <summary>
        /// Logs a user login.
        /// </summary>
        Task LogLoginAsync(Guid userId, string username);

        /// <summary>
        /// Logs a user logout.
        /// </summary>
        Task LogLogoutAsync(Guid userId, string username);

        /// <summary>
        /// Logs a failed login attempt.
        /// </summary>
        Task LogFailedLoginAsync(string username, string reason);

        /// <summary>
        /// Logs an authorization failure.
        /// </summary>
        Task LogAuthorizationFailureAsync(Guid userId, string action, string resource, string reason);
    }
}
