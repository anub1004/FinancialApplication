using System.ComponentModel.DataAnnotations;

namespace FinancialApplication.Application.DTOs
{
    /// <summary>
    /// DTO for the second step of the two-step TOTP login flow.
    /// The user submits the 6-digit TOTP code along with the session token
    /// received from step 1 (login/register).
    /// </summary>
    public class TotpVerifyDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "TOTP code must be exactly 6 digits.")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "TOTP code must be exactly 6 digits.")]
        public string TotpCode { get; set; } = string.Empty;

        /// <summary>
        /// Short-lived session token issued by step 1 of the login flow.
        /// This is NOT a full JWT auth token — it only authorizes TOTP verification.
        /// </summary>
        [Required]
        public string TotpSessionToken { get; set; } = string.Empty;
    }
}
