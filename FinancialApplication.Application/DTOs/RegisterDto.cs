using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Text;

namespace FinancialApp.Application.DTOs
{
    public class RegisterDto
    {
            public string Username { get; set; } = string.Empty;

            [EmailAddress(ErrorMessage = "Invalid email address format."), MaxLength(100, ErrorMessage = "Email cannot exceed 100 characters."), Required]
            public string Email { get; set; } = string.Empty;
            [MaxLength(100, ErrorMessage = "Password cannot exceed 100 characters.")]
            public string Password { get; set; } = string.Empty;


}
}
