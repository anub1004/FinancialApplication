using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinancialApplication.Application.DTOs
{
    public class AuthenticationResult
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public DateTime ExpiresAt { get; set; }
        public int ExpiresIn { get; set; }
        public string Role { get; set; }

        // ── TOTP-related properties (populated during registration / first login) ──
        /// <summary>
        /// If true, the user must verify a TOTP code before receiving JWT tokens.
        /// </summary>
        public bool TotpRequired { get; set; } = false;

        /// <summary>
        /// If true, the user needs to scan a QR code to set up their authenticator app.
        /// </summary>
        public bool TotpSetupRequired { get; set; } = false;

        /// <summary>
        /// Base64-encoded PNG of the QR code for authenticator app setup.
        /// Only populated when TotpSetupRequired is true.
        /// </summary>
        public string? QrCodeBase64 { get; set; }

        /// <summary>
        /// The TOTP secret in Base32 format for manual entry into authenticator app.
        /// Only populated when TotpSetupRequired is true.
        /// </summary>
        public string? ManualEntryKey { get; set; }

        /// <summary>
        /// Short-lived session token for the TOTP verification step.
        /// Only populated when TotpRequired is true.
        /// </summary>
        public string? TotpSessionToken { get; set; }
    }
}
