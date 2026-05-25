using System;
using System.Collections.Generic;
using System.Text;

namespace FinancialApp.Application.DTOs
{
        public class UserDto
        {
            public Guid Id { get; set; }
            public string Username { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Role { get; set; } = string.Empty;
            public int RoleId { get; set; }
        }
    
}
