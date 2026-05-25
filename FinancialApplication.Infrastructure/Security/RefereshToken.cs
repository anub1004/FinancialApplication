using System;
using System.Security.Cryptography;

namespace FinancialApp.Infrastructure.Security
{
    /// <summary>
    /// Generates cryptographically secure refresh tokens.
    /// Refresh tokens are long-lived tokens used to obtain new access tokens.
    /// </summary>
    public class RefreshTokenGenerator
    {
        /// <summary>
        /// Generates a cryptographically secure random refresh token.
        /// Uses 64 bytes (512 bits) of randomness for strong security.
        /// </summary>
        /// <returns>Base64-encoded refresh token</returns>
        public string GenerateRefreshToken()
        {
            var randomBytes = new byte[64]; // 512 bits of randomness

            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }

            return Convert.ToBase64String(randomBytes);
        }

        /// <summary>
        /// Validates a refresh token (basic format check).
        /// For full validation, check expiration in database.
        /// </summary>
        /// <param name="token">Token to validate</param>
        /// <returns>True if token format is valid</returns>
        public bool IsValidTokenFormat(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return false;

            try
            {
                // Try to parse as base64
                Convert.FromBase64String(token);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}