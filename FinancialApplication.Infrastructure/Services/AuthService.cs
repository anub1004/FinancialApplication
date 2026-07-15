using Azure.Core;
using FinancialApp.Application.DTOs;
using FinancialApp.Infrastructure.DTOs;
using FinancialApp.Infrastructure.Interfaces;
using FinancialApp.Infrastructure.Security;
using FinancialApplication.Application.DTOs;
using FinancialApplication.Application.Interfaces;
using FinancialApplication.Domain.Domain.Entity;
using FinancialApplication.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using AuthenticationResultDto = FinancialApplication.Application.DTOs.AuthenticationResult;
using OtpNet;
using QRCoder;

namespace FinancialApp.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IJwtTokenGenerator _tokenGenerator;
        private readonly RefreshTokenGenerator _refreshTokenGenerator;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;

        public AuthService(
            AppDbContext context,
            IJwtTokenGenerator tokenGenerator,
            RefreshTokenGenerator refreshTokenGenerator,
            IPasswordHasher passwordHasher,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _tokenGenerator = tokenGenerator;
            _refreshTokenGenerator = refreshTokenGenerator;
            _passwordHasher = passwordHasher;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
        }

        // ─── Registration ───────────────────────────────────────────────────────────
        public async Task<AuthenticationResultDto> RegisterAsync(RegisterUserDto request)
        {
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            {
                throw new InvalidOperationException("Email already exists.");
            }

            if (await _context.Users.AnyAsync(u => u.Username == request.Username))
            {
                throw new InvalidOperationException("Username already exists.");
            }

            var defaultRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "User" && r.IsActive);
            if (defaultRole == null)
            {
                throw new InvalidOperationException("Default user role is not configured.");
            }

            // Generate TOTP secret at registration time (mandatory for all users)
            var totpSecret = GenerateTotpSecret();

            var user = new User 
            {
                Username = request.Username,
                Email = request.Email,
                Password = _passwordHasher.HashPassword(request.Password),
                RoleId = defaultRole.Id, 
                IsActive = true,
                TotpSecret = totpSecret,
                IsTotpConfigured = false,
                CreatedAt = DateTime.UtcNow, 
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Return a special result that includes TOTP setup data
            // The user must verify TOTP before they can get a real JWT
            var qrCodeBase64 = GenerateQrCodeBase64(user.Email, totpSecret);
            var totpSessionToken = GenerateTotpSessionToken(user.Id);

            return new AuthenticationResultDto
            {
                AccessToken = string.Empty,  // No JWT until TOTP verified
                RefreshToken = string.Empty,
                ExpiresAt = DateTime.MinValue,
                ExpiresIn = 0,
                Role = defaultRole.Name,
                TotpRequired = true,
                TotpSetupRequired = true,
                QrCodeBase64 = qrCodeBase64,
                ManualEntryKey = totpSecret,
                TotpSessionToken = totpSessionToken
            };
        }

        // ─── Login Step 1: Validate credentials, return TOTP challenge ──────────────
        public async Task<object> LoginStep1Async(LoginUserDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
                throw new UnauthorizedAccessException("Email is required");

            if (string.IsNullOrWhiteSpace(request.Password))
                throw new UnauthorizedAccessException("Password is required");

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null)
                throw new UnauthorizedAccessException("Email does not exist");

            if (!user.IsActive)
                throw new UnauthorizedAccessException("Account is deactivated");

            if (!_passwordHasher.VerifyPassword(request.Password, user.Password))
                throw new UnauthorizedAccessException("Incorrect password");

            if (user.Role == null)
                throw new UnauthorizedAccessException("User role not assigned");

            // Generate a short-lived TOTP session token (not a full auth token)
            var totpSessionToken = GenerateTotpSessionToken(user.Id);

            // Check if user needs TOTP setup (first time)
            if (string.IsNullOrEmpty(user.TotpSecret))
            {
                // Generate and store a new TOTP secret
                var totpSecret = GenerateTotpSecret();
                user.TotpSecret = totpSecret;
                user.IsTotpConfigured = false;
                user.UpdatedAt = DateTime.UtcNow;
                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                var qrCodeBase64 = GenerateQrCodeBase64(user.Email, totpSecret);

                return new
                {
                    totpRequired = true,
                    totpSetupRequired = true,
                    qrCodeBase64,
                    manualEntryKey = totpSecret,
                    totpSessionToken,
                    email = user.Email
                };
            }

            // User already has TOTP configured — just ask for the code
            if (!user.IsTotpConfigured)
            {
                // Secret exists but never verified — re-show QR code
                var qrCodeBase64 = GenerateQrCodeBase64(user.Email, user.TotpSecret);
                return new
                {
                    totpRequired = true,
                    totpSetupRequired = true,
                    qrCodeBase64,
                    manualEntryKey = user.TotpSecret,
                    totpSessionToken,
                    email = user.Email
                };
            }

            return new
            {
                totpRequired = true,
                totpSetupRequired = false,
                totpSessionToken,
                email = user.Email
            };
        }

        // ─── Google Login Step 1: Validate Google token, return TOTP challenge ──────
        public async Task<object> GoogleLoginStep1Async(string idToken)
        {
            // Step 1: Verify the token with Google
            var client = _httpClientFactory.CreateClient("GoogleAuth");
            var response = await client.GetAsync($"tokeninfo?id_token={idToken}");

            if (!response.IsSuccessStatusCode)
                throw new UnauthorizedAccessException("Invalid Google token.");

            var json = await response.Content.ReadAsStringAsync();
            var googleUser = JsonSerializer.Deserialize<JsonElement>(json);

            // Step 2: Validate the audience (must match our Client ID)
            var expectedClientId = _configuration["Google:ClientId"];
            var audience = googleUser.GetProperty("aud").GetString();

            if (audience != expectedClientId)
                throw new UnauthorizedAccessException("Google token was not issued for this app.");

            // Step 3: Extract user info from Google's response
            var googleId = googleUser.GetProperty("sub").GetString()!;
            var email = googleUser.GetProperty("email").GetString()!;
            var name = googleUser.TryGetProperty("name", out var nameProp) ? nameProp.GetString() ?? email.Split('@')[0] : email.Split('@')[0];
            var picture = googleUser.TryGetProperty("picture", out var picProp) ? picProp.GetString() : null;

            // Step 4: Find or create the user
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.GoogleId == googleId);

            if (user == null)
            {
                // Check if a user with this email already exists (registered normally)
                user = await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.Email == email);

                if (user != null)
                {
                    // Link existing account to Google
                    user.GoogleId = googleId;
                    user.ProfilePicture = picture;
                    user.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    // Create a brand-new user
                    var defaultRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "User" && r.IsActive)
                        ?? throw new InvalidOperationException("Default user role is not configured.");

                    user = new User
                    {
                        Username = name,
                        Email = email,
                        Password = _passwordHasher.HashPassword(Guid.NewGuid().ToString()),
                        RoleId = defaultRole.Id,
                        GoogleId = googleId,
                        ProfilePicture = picture,
                        IsActive = true,
                        TotpSecret = GenerateTotpSecret(),
                        IsTotpConfigured = false,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.Users.Add(user);
                }

                await _context.SaveChangesAsync();
            }

            if (!user.IsActive)
                throw new UnauthorizedAccessException("Account is deactivated.");

            // Step 5: Return TOTP challenge (instead of issuing JWT directly)
            var totpSessionToken = GenerateTotpSessionToken(user.Id);

            if (string.IsNullOrEmpty(user.TotpSecret))
            {
                var totpSecret = GenerateTotpSecret();
                user.TotpSecret = totpSecret;
                user.IsTotpConfigured = false;
                user.UpdatedAt = DateTime.UtcNow;
                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                return new
                {
                    totpRequired = true,
                    totpSetupRequired = true,
                    qrCodeBase64 = GenerateQrCodeBase64(email, totpSecret),
                    manualEntryKey = totpSecret,
                    totpSessionToken,
                    email
                };
            }

            if (!user.IsTotpConfigured)
            {
                return new
                {
                    totpRequired = true,
                    totpSetupRequired = true,
                    qrCodeBase64 = GenerateQrCodeBase64(email, user.TotpSecret),
                    manualEntryKey = user.TotpSecret,
                    totpSessionToken,
                    email
                };
            }

            return new
            {
                totpRequired = true,
                totpSetupRequired = false,
                totpSessionToken,
                email
            };
        }

        // ─── Step 2: Verify TOTP code and issue real JWT tokens ─────────────────────
        public async Task<AuthenticationResultDto> VerifyTotpAndLoginAsync(TotpVerifyDto request)
        {
            // Validate the TOTP session token
            var userId = ValidateTotpSessionToken(request.TotpSessionToken);
            if (!userId.HasValue)
                throw new UnauthorizedAccessException("Invalid or expired TOTP session token. Please login again.");

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == userId.Value);

            if (user == null)
                throw new UnauthorizedAccessException("User not found.");

            if (!user.IsActive)
                throw new UnauthorizedAccessException("Account is deactivated.");

            if (string.IsNullOrEmpty(user.TotpSecret))
                throw new UnauthorizedAccessException("TOTP is not configured for this account. Please login again.");

            // Validate the TOTP code
            if (!ValidateTotpCode(user.TotpSecret, request.TotpCode))
                throw new UnauthorizedAccessException("Invalid TOTP code. Please try again.");

            // Mark TOTP as configured if this is the first successful verification
            if (!user.IsTotpConfigured)
            {
                user.IsTotpConfigured = true;
                user.UpdatedAt = DateTime.UtcNow;
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
            }

            // Issue real JWT tokens
            return await AuthenticateAsync(
                user.Id,
                user.Email,
                user.Username,
                user.Role?.Name ?? "User"
            );
        }

        // ─── Core authentication (JWT issuance) ────────────────────────────────────
        public async Task<AuthenticationResultDto> AuthenticateAsync(Guid userId, string email, string username, string role)
        {
            var accessToken = _tokenGenerator.GenerateAccessToken(userId, email, username, role);
            var refreshToken = _tokenGenerator.GenerateRefreshToken(userId);

            var expiresInMinutes = Convert.ToInt32(_configuration["Jwt:ExpireMinutes"] ?? "15");
            var expiresAt = DateTime.UtcNow.AddMinutes(expiresInMinutes);
            var expiresIn = (int)(expiresAt - DateTime.UtcNow).TotalSeconds;

            var refreshTokenExpireDays = Convert.ToInt32(_configuration["Jwt:RefreshTokenExpireDays"] ?? "7");
            var refreshTokenEntity = new RefreshToken
            {
                UserId = userId,
                Token = refreshToken,
                ExpiryDate = DateTime.UtcNow.AddDays(refreshTokenExpireDays),
                CreatedDate = DateTime.UtcNow
            };

            _context.RefreshTokens.Add(refreshTokenEntity);
            await _context.SaveChangesAsync();

            return new AuthenticationResultDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = expiresAt,
                ExpiresIn = expiresIn,
                Role = role
            };
        }

        public async Task<string> RefreshAccessTokenAsync(string refreshToken)
        {
            var userId = _tokenGenerator.ValidateTokenAndGetUserId(refreshToken);

            if (!userId.HasValue)
            {
                throw new InvalidOperationException("Invalid refresh token");
            }

            throw new NotImplementedException("Complete database integration for refresh tokens");
        }

        public Guid? ValidateAccessToken(string token)
        {
            return _tokenGenerator.ValidateTokenAndGetUserId(token);
        }

        public async Task<bool> ValidateRefreshTokenAsync(Guid userId, string refreshToken)
        {
            return await Task.FromResult(false);
        }

        public async Task<bool> _Logout(Guid userId, string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            var user = await _context.Users.FindAsync(userId);
            var refreshToken = await _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == token && rt.UserId == userId);
            if (user == null || refreshToken == null)
            {
                return false;
            }


            _context.RefreshTokens.Remove(refreshToken);
            user.UpdatedAt = DateTime.UtcNow;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return true;
        }
        public async Task<AuthDto> CheckAuth(Guid userId, string token)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null || !user.IsActive)
            {
                return null;
            }

            var validUserId = _tokenGenerator.ValidateTokenAndGetUserId(token);
            if (!validUserId.HasValue || validUserId.Value != userId)
            {
                return null;
            }

            return new AuthDto
            {
                UserId = user.Id,
                user = user.Username,
                role = user.Role != null ? user.Role.Name : "User"
            };
        }

        public ClaimsPrincipal ValidateToken(string token)
        {
            try
            {
                var jwtSettings = _configuration.GetSection("Jwt");
                var secretKey = jwtSettings["Key"];

                if (string.IsNullOrEmpty(secretKey) || secretKey.Length < 32)
                {
                    return null;
                }

                var key = Encoding.ASCII.GetBytes(secretKey);
                var tokenHandler = new JwtSecurityTokenHandler();

                var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidateAudience = true,
                    ValidAudience = jwtSettings["Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                return principal;
            }
            catch
            {
                return null;
            }
        }

        // ═══════════════════════════════════════════════════════════════════════════
        // TOTP Private Helper Methods
        // ═══════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Generates a random 20-byte TOTP secret and returns it as a Base32 string.
        /// </summary>
        private string GenerateTotpSecret()
        {
            var secretKey = KeyGeneration.GenerateRandomKey(20);
            return Base32Encoding.ToString(secretKey);
        }

        /// <summary>
        /// Generates a QR code image (Base64-encoded PNG) for the given email and TOTP secret.
        /// The QR code encodes an otpauth:// URI compatible with Google Authenticator, Authy, etc.
        /// </summary>
        private string GenerateQrCodeBase64(string email, string secret)
        {
            var issuer = _configuration["TwoFactor:Issuer"] ?? "FinancialApp";
            var otpauthUri = $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(email)}?secret={secret}&issuer={Uri.EscapeDataString(issuer)}&digits=6&period=30";

            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(otpauthUri, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);
            var pngBytes = qrCode.GetGraphic(5);

            return $"data:image/png;base64,{Convert.ToBase64String(pngBytes)}";
        }

        /// <summary>
        /// Validates a 6-digit TOTP code against the stored secret.
        /// Allows ±1 time step window (30 seconds each) to account for clock drift.
        /// </summary>
        private bool ValidateTotpCode(string secret, string code)
        {
            try
            {
                var secretBytes = Base32Encoding.ToBytes(secret);
                var totp = new Totp(secretBytes, step: 30, totpSize: 6);
                return totp.VerifyTotp(code, out _, new VerificationWindow(previous: 1, future: 1));
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Generates a short-lived (5-minute) JWT token that only authorizes TOTP verification.
        /// This is NOT a full auth token — it contains a special "purpose" claim.
        /// </summary>
        private string GenerateTotpSessionToken(Guid userId)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var secretKey = jwtSettings["Key"];
            var key = Encoding.ASCII.GetBytes(secretKey!);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim("purpose", "totp-verification")
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(5),
                Issuer = jwtSettings["Issuer"],
                Audience = jwtSettings["Audience"],
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        /// <summary>
        /// Validates a TOTP session token and extracts the userId.
        /// Returns null if the token is invalid, expired, or not a TOTP session token.
        /// </summary>
        private Guid? ValidateTotpSessionToken(string token)
        {
            try
            {
                var jwtSettings = _configuration.GetSection("Jwt");
                var secretKey = jwtSettings["Key"];
                var key = Encoding.ASCII.GetBytes(secretKey!);

                var tokenHandler = new JwtSecurityTokenHandler();
                var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidateAudience = true,
                    ValidAudience = jwtSettings["Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30)
                }, out SecurityToken validatedToken);

                // Verify this is a TOTP session token, not a regular auth token
                var purposeClaim = principal.FindFirst("purpose")?.Value;
                if (purposeClaim != "totp-verification")
                    return null;

                var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (Guid.TryParse(userIdClaim, out var userId))
                    return userId;

                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}
