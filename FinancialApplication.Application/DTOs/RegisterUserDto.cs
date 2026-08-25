using System;
using System.ComponentModel.DataAnnotations;

namespace FinancialApplication.Application.DTOs
{
    public class RegisterUserDto
    {
        [Required]
        [MaxLength(50, ErrorMessage = "Username cannot exceed 50 characters.")]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress(ErrorMessage = "Invalid email address format.")]
        [MaxLength(100, ErrorMessage = "Email cannot exceed 100 characters.")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters long.")]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Optional plan ID selected during signup. Null means the default (free) plan is assigned.
        /// </summary>
        public Guid? SelectedPlanId { get; set; }
    }
}
