using System.ComponentModel.DataAnnotations;

namespace FinancialApp.Infrastructure.DTOs
{
    public class LoginUserDto
    {
        
        [EmailAddress(ErrorMessage = "Invalid email address format."), MaxLength(100, ErrorMessage = "Email cannot exceed 100 characters."), Required]
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
       
    }
}
