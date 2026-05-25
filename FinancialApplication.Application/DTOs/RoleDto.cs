using System;
using System.Collections.Generic;
using System.Text;

namespace FinancialApp.Application.DTOs
{
    
        public class RoleDto
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public bool IsActive { get; set; }
            public string CreatedBy { get; set; } = string.Empty;
            public DateTime CreatedAt { get; set; }
        }
    
}
