using System.ComponentModel.DataAnnotations;

namespace FinancialApplication.Application.DTOs
{
    public class EmailLoginRequestDto { [Required, EmailAddress] public string Email { get; set; } = string.Empty; }

    public class EmailLoginVerifyDto : EmailLoginRequestDto
    {
        [Required, StringLength(8, MinimumLength = 8), RegularExpression("^[0-9]{8}$")]
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Optional: the TOTP session token from the registration step.
        /// Used during signup email verification to preserve the selectedPlanId claim.
        /// </summary>
        public string? TotpSessionToken { get; set; }
    }
}
