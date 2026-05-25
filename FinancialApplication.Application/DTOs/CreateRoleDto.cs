using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;

namespace FinancialApp.Application.DTOs
{
     public class CreateRoleDto
        {
            [Required]
            [MaxLength(50, ErrorMessage = "Role name cannot exceed 50 characters.")]
            public string Name { get; set; } = string.Empty;
        }
    
}
