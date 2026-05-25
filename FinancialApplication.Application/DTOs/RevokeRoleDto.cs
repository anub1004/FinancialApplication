using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinancialApplication.Application.DTOs
{
    public class RevokeRoleDto
    {
        public Guid UserId { get; set; }
        public string RoleName { get; set; }
        
    }
}
