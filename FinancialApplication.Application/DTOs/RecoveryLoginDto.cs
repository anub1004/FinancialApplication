using System.ComponentModel.DataAnnotations;

namespace FinancialApplication.Application.DTOs
{
    public class RecoveryLoginDto
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        [Required, StringLength(32, MinimumLength = 8)]
        public string RecoveryCode { get; set; } = string.Empty;
    }
}
