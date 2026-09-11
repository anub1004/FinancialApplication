using System.ComponentModel.DataAnnotations;

namespace FinancialApplication.Application.DTOs
{
    /// <summary>
    /// Request DTO for the forgot-password endpoint.
    /// Only requires the user's email address.
    /// </summary>
    public class ForgotPasswordRequestDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request DTO for the reset-password endpoint.
    /// Requires the email, reset token (from email link), and new password.
    /// </summary>
    public class ResetPasswordDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Token { get; set; } = string.Empty;

        [Required]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters long.")]
        public string NewPassword { get; set; } = string.Empty;
    }
}
