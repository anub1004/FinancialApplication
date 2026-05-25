using System;

namespace FinancialApp.Infrastructure.Security
{
    /// <summary>
    /// Password hashing service using PBKDF2.
    /// Implements secure password hashing per security best practices.
    /// </summary>
    public interface IPasswordHasher
    {
        /// <summary>
        /// Hashes a password using PBKDF2-SHA256.
        /// </summary>
        string HashPassword(string password);

        /// <summary>
        /// Verifies a password against a hash.
        /// </summary>
        bool VerifyPassword(string password, string hash);
    }

    /// <summary>
    /// Implementation using PBKDF2-SHA256 (built-in .NET).
    /// Uses 10,000 iterations with 128-bit salt.
    /// </summary>
    public class PasswordHasher : IPasswordHasher
    {
        private const int Iterations = 10000;
        private const int SaltSize = 128 / 8; // 128 bits
        private const int HashSize = 256 / 8; // 256 bits

        /// <summary>
        /// Hashes a password using PBKDF2-SHA256.
        /// Returns format: Version$IterationsHex$Base64(Salt+Hash)
        /// </summary>
        public string HashPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password cannot be empty", nameof(password));

            if (password.Length < 8)
                throw new ArgumentException("Password must be at least 8 characters", nameof(password));

            // Generate random salt
            using (var rng = new System.Security.Cryptography.RNGCryptoServiceProvider())
            {
                byte[] salt = new byte[SaltSize];
                rng.GetBytes(salt);

                // Hash password with PBKDF2-SHA256
                var pbkdf2 = new System.Security.Cryptography.Rfc2898DeriveBytes(
                    password,
                    salt,
                    Iterations,
                    System.Security.Cryptography.HashAlgorithmName.SHA256);

                byte[] hash = pbkdf2.GetBytes(HashSize);

                // Combine version, iterations, salt, and hash
                byte[] hashBytes = new byte[1 + 4 + SaltSize + HashSize];
                hashBytes[0] = 1; // Version

                // Store iterations as 4-byte value
                BitConverter.GetBytes(Iterations).CopyTo(hashBytes, 1);
                System.Buffer.BlockCopy(salt, 0, hashBytes, 5, SaltSize);
                System.Buffer.BlockCopy(hash, 0, hashBytes, 5 + SaltSize, HashSize);

                return Convert.ToBase64String(hashBytes);
            }
        }

        /// <summary>
        /// Verifies a password against a PBKDF2 hash.
        /// </summary>
        public bool VerifyPassword(string password, string hash)
        {
            if (string.IsNullOrWhiteSpace(password))
                return false;

            if (string.IsNullOrWhiteSpace(hash))
                return false;

            try
            {
                byte[] hashBytes = Convert.FromBase64String(hash);

                // Minimum length: version(1) + iterations(4) + salt(16) + hash(32) = 53
                if (hashBytes.Length < 53)
                    return false;

                // Check version
                if (hashBytes[0] != 1)
                    return false;

                // Extract iterations
                int storedIterations = BitConverter.ToInt32(hashBytes, 1);

                // Extract salt
                byte[] salt = new byte[SaltSize];
                System.Buffer.BlockCopy(hashBytes, 5, salt, 0, SaltSize);

                // Hash the provided password with stored salt and iterations
                var pbkdf2 = new System.Security.Cryptography.Rfc2898DeriveBytes(
                    password,
                    salt,
                    storedIterations,
                    System.Security.Cryptography.HashAlgorithmName.SHA256);

                byte[] computedHash = pbkdf2.GetBytes(HashSize);

                // Compare stored hash with computed hash
                for (int i = 0; i < HashSize; i++)
                {
                    if (hashBytes[5 + SaltSize + i] != computedHash[i])
                        return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
