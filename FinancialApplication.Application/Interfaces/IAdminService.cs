using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinancialApplication.Application.Interfaces
{
   public interface IAdminService
    {
        Task<string> AssignRoleAsync(Guid userId, string roleName);
       Task<string> RevokeRoleAsync(Guid userId,string roleName);

        Task<string> DeactivateUserAsync(Guid userId);
         Task<string> ActivateUserAsync(Guid userId);
    }
}
