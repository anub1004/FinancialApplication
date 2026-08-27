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
using System.Security.Cryptography;
using System.Net;
using System.Net.Mail;
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
        private readonly ISubscriptionService _subscriptionService;

        public AuthService(
            AppDbContext context,
            IJwtTokenGenerator tokenGenerator,
            RefreshTokenGenerator refreshTokenGenerator,
            IPasswordHasher passwordHasher,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            ISubscriptionService subscriptionService)
        {
            _context = context;
            _tokenGenerator = tokenGenerator;
            _refreshTokenGenerator = refreshTokenGenerator;
            _passwordHasher = passwordHasher;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _subscriptionService = subscriptionService;
        }

        // Registration 
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

            // Generate TOTP secret at registration time
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

            // Generate email verification OTP code immediately at registration
            var emailCode = RandomNumberGenerator
    .GetInt32(10000000, 100000000)
    .ToString();
            var now = DateTime.UtcNow;
            _context.EmailLoginCodes.Add(new EmailLoginCode 
            { 
                UserId = user.Id, 
                CodeHash = HashRecoveryCode(emailCode), 
                ExpiresAt = now.AddMinutes(2) 
            });
            await _context.SaveChangesAsync();
            await SendEmailLoginCodeAsync(user.Email, emailCode);

            return new AuthenticationResultDto
            {
                AccessToken = string.Empty,
                RefreshToken = string.Empty,
                ExpiresAt = DateTime.MinValue,
                ExpiresIn = 0,
                Role = defaultRole.Name,
                TotpRequired = false,
                TotpSetupRequired = false,
                EmailOtpRequired = true,
                Email = user.Email,
                TotpSessionToken = GenerateTotpSessionToken(user.Id, request.SelectedPlanId)
            };
        }

        // Login Step 1: Validate credentials, return TOTP challenge
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

            // Generate a short-lived TOTP session token 
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

            // User already has TOTP -ask code
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

        // Google Login Step 1: Validate Google token, return TOTP 
        public async Task<object> GoogleLoginStep1Async(string idToken)
        {
            // Step 1: Verify the token with Google
            var client = _httpClientFactory.CreateClient("GoogleAuth");
            var response = await client.GetAsync($"tokeninfo?id_token={idToken}");

            if (!response.IsSuccessStatusCode)
                throw new UnauthorizedAccessException("Invalid Google token.");

            var json = await response.Content.ReadAsStringAsync();
            var googleUser = JsonSerializer.Deserialize<JsonElement>(json);

            // Step 2: Validate the audience -must match Client ID
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
                // Check if a user with this email already exists 
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
                    // Create a new user
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

            // Step 5: Return TOTP code instead of issuing JWT directly
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

        // Step 2: Verify TOTP code and issue JWT tokens
        public async Task<AuthenticationResultDto> VerifyTotpAndLoginAsync(TotpVerifyDto request)
        {
            // Validate the TOTP session token and extract selectedPlanId if present
            var (userId, selectedPlanId) = ValidateTotpSessionToken(request.TotpSessionToken);
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

            // Validate TOTP code
            if (!ValidateTotpCode(user.TotpSecret, request.TotpCode))
                throw new UnauthorizedAccessException("Invalid TOTP code. Please try again.");

            // Mark TOTP as configured if this is the first successful verification
            IReadOnlyList<string>? recoveryCodes = null;
            bool isFirstSetup = false;
            var hasUsableRecoveryCode = await _context.RecoveryCodes
                .AnyAsync(c => c.UserId == user.Id && c.UsedAt == null);
            if (!user.IsTotpConfigured || !hasUsableRecoveryCode)
            {
                isFirstSetup = true;
                user.IsTotpConfigured = true;
                user.UpdatedAt = DateTime.UtcNow;
                recoveryCodes = GenerateRecoveryCodes(user.Id);
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
            }

            // Auto-create subscription for new users (signup flow)
            // This runs on first TOTP setup, which only happens during registration
            if (isFirstSetup)
            {
                try
                {
                    await _subscriptionService.CreateSubscriptionForNewUserAsync(userId.Value, selectedPlanId);
                }
                catch (Exception ex)
                {
                    // Log but don't fail the login — subscription can be created later
                    System.Diagnostics.Debug.WriteLine($"Failed to create signup subscription: {ex.Message}");
                }
            }

            // Issue  JWT token
            var result = await AuthenticateAsync(
                user.Id,
                user.Email,
                user.Username,
                user.Role?.Name ?? "User"
            );
            result.RecoveryCodes = recoveryCodes;
            return result;
        }

        public async Task<AuthenticationResultDto> LoginWithRecoveryCodeAsync(RecoveryLoginDto request)
        {
            var user = await _context.Users.Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null || !user.IsActive || !_passwordHasher.VerifyPassword(request.Password, user.Password))
                throw new UnauthorizedAccessException("Invalid email, password, or recovery code.");

            var codeHash = HashRecoveryCode(request.RecoveryCode);
            var recoveryCode = await _context.RecoveryCodes
                .FirstOrDefaultAsync(c => c.UserId == user.Id && c.CodeHash == codeHash && c.UsedAt == null);
            if (recoveryCode == null)
                throw new UnauthorizedAccessException("Invalid email, password, or recovery code.");

            recoveryCode.UsedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return await AuthenticateAsync(user.Id, user.Email, user.Username, user.Role?.Name ?? "User");
        }

        public async Task RequestEmailLoginCodeAsync(EmailLoginRequestDto request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive);
            if (user == null) return;
            var now = DateTime.UtcNow;
            var activeCode = await _context.EmailLoginCodes
                .AnyAsync(c => c.UserId == user.Id && c.UsedAt == null && c.ExpiresAt > now);
            if (activeCode) return; // A code is already valid; do not send another before two minutes pass.
            var code = RandomNumberGenerator
    .GetInt32(10000000, 100000000)
    .ToString();

            _context.EmailLoginCodes.RemoveRange(_context.EmailLoginCodes.Where(c => c.UserId == user.Id && c.UsedAt == null));
            _context.EmailLoginCodes.Add(new EmailLoginCode { UserId = user.Id, CodeHash = HashRecoveryCode(code), ExpiresAt = now.AddMinutes(2) });
            await _context.SaveChangesAsync();
            await SendEmailLoginCodeAsync(user.Email, code);
        }

        public async Task<AuthenticationResultDto> LoginWithEmailCodeAsync(EmailLoginVerifyDto request)
        {
            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive);
            if (user == null) throw new UnauthorizedAccessException("Invalid or expired email verification code.");
            var code = await _context.EmailLoginCodes.FirstOrDefaultAsync(c => c.UserId == user.Id && c.CodeHash == HashRecoveryCode(request.Code) && c.UsedAt == null && c.ExpiresAt > DateTime.UtcNow);
            if (code == null) throw new UnauthorizedAccessException("Invalid or expired email verification code.");
            code.UsedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return await AuthenticateAsync(user.Id, user.Email, user.Username, user.Role?.Name ?? "User");
        }

        public async Task<AuthenticationResultDto> VerifySignupEmailOtpAsync(EmailLoginVerifyDto request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive);
            if (user == null) throw new UnauthorizedAccessException("Invalid or expired email verification code.");
            var code = await _context.EmailLoginCodes.FirstOrDefaultAsync(c => c.UserId == user.Id && c.CodeHash == HashRecoveryCode(request.Code) && c.UsedAt == null && c.ExpiresAt > DateTime.UtcNow);
            if (code == null) throw new UnauthorizedAccessException("Invalid or expired email verification code.");

            code.UsedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Extract selectedPlanId from the original session token (if provided)
            // so it survives through the email verification step into the TOTP step
            Guid? selectedPlanId = null;
            if (!string.IsNullOrEmpty(request.TotpSessionToken))
            {
                var (_, planId) = ValidateTotpSessionToken(request.TotpSessionToken);
                selectedPlanId = planId;
            }

            // Email verified! Return the TOTP QR setup data with selectedPlanId preserved
            var qrCodeBase64 = GenerateQrCodeBase64(user.Email, user.TotpSecret);
            var totpSessionToken = GenerateTotpSessionToken(user.Id, selectedPlanId);

            return new AuthenticationResultDto
            {
                AccessToken = string.Empty,
                RefreshToken = string.Empty,
                ExpiresAt = DateTime.MinValue,
                ExpiresIn = 0,
                TotpRequired = true,
                TotpSetupRequired = true,
                QrCodeBase64 = qrCodeBase64,
                ManualEntryKey = user.TotpSecret,
                TotpSessionToken = totpSessionToken
            };
        }

        // ─── Core authentication  ────────────────────────────────────
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

        private IReadOnlyList<string> GenerateRecoveryCodes(Guid userId)
        {
            const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var codes = new List<string>();
            for (var i = 0; i < 10; i++)
            {
                Span<byte> bytes = stackalloc byte[12];
                RandomNumberGenerator.Fill(bytes);
                var raw = new string(bytes.ToArray().Select(b => alphabet[b % alphabet.Length]).ToArray());
                var code = $"{raw[..4]}-{raw[4..8]}-{raw[8..12]}";
                codes.Add(code);
                _context.RecoveryCodes.Add(new RecoveryCode { UserId = userId, CodeHash = HashRecoveryCode(code) });
            }
            return codes;
        }

        private static string HashRecoveryCode(string code)
        {
            var normalized = code.Replace("-", string.Empty).Trim().ToUpperInvariant();
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));
        }

        private async Task SendEmailLoginCodeAsync(string recipient, string code)
        {
            var host = _configuration["Smtp:Host"]?.Trim();
            var from = _configuration["Smtp:From"]?.Trim();
            var username = _configuration["Smtp:Username"]?.Trim();
            var password = _configuration["Smtp:Password"]?.Trim();

            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(from)) 
                throw new InvalidOperationException("SMTP is not configured.");

            var port = int.TryParse(_configuration["Smtp:Port"], out var parsedPort) ? parsedPort : 587;
            var enableSsl = bool.TryParse(_configuration["Smtp:EnableSsl"], out var ssl) ? ssl : true;

            using var client = new SmtpClient(host, port)
            {
                EnableSsl = enableSsl,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(username, password)
            };

         
using var message = new MailMessage(from, recipient)
{
    Subject = "Your Financial Management Verification Code",
    IsBodyHtml = true,
    Body = $@"
    <div style='font-family: Arial, Helvetica, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #e5e7eb; border-radius: 8px; overflow: hidden;'>
        
        <div style='background-color: #2563eb; color: white; padding: 20px; text-align: center;'>
            <h2 style='margin: 0;'>Financial Management</h2>
        </div>

        <div style='padding: 24px; color: #374151; line-height: 1.6;'>
            <p>Hello,</p>

            <p>We received a request to sign in to your <strong>Financial Management</strong> account.</p>

            <p>Your verification code is:</p>

            <div style='text-align: center; margin: 30px 0;'>
                <span style='display: inline-block; font-size: 32px; font-weight: bold; letter-spacing: 8px; padding: 15px 30px; background-color: #f3f4f6; border: 1px solid #d1d5db; border-radius: 8px;'>
                    {code}
                </span>
            </div>

            <p>
                This code is valid for <strong>2 minutes</strong> and can be used only once.
            </p>

            <p>
                If you did not request this code, you can safely ignore this email. Your account will remain secure.
            </p>

            <hr style='border: none; border-top: 1px solid #e5e7eb; margin: 24px 0;' />

            <p style='font-size: 12px; color: #6b7280;'>
                This is an automated email. Please do not reply.
            </p>
        </div>
    </div>"
};

            await client.SendMailAsync(message);
        }

        /// <summary>
        /// Generates a short-lived (5-minute) JWT token that only authorizes TOTP verification.
        /// This is NOT a full auth token — it contains a special "purpose" claim.
        /// Optionally includes the selected plan ID for the signup flow.
        /// </summary>
        private string GenerateTotpSessionToken(Guid userId, Guid? selectedPlanId = null)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var secretKey = jwtSettings["Key"];
            var key = Encoding.ASCII.GetBytes(secretKey!);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim("purpose", "totp-verification")
            };

            // Add selected plan ID claim for signup flow
            if (selectedPlanId.HasValue)
            {
                claims.Add(new Claim("selectedPlanId", selectedPlanId.Value.ToString()));
            }

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
        /// Validates a TOTP session token and extracts the userId and optional selectedPlanId.
        /// Returns (null, null) if the token is invalid, expired, or not a TOTP session token.
        /// </summary>
        private (Guid? userId, Guid? selectedPlanId) ValidateTotpSessionToken(string token)
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
                    return (null, null);

                Guid? userId = null;
                var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (Guid.TryParse(userIdClaim, out var parsedUserId))
                    userId = parsedUserId;

                // Extract optional selectedPlanId claim
                Guid? selectedPlanId = null;
                var planIdClaim = principal.FindFirst("selectedPlanId")?.Value;
                if (!string.IsNullOrEmpty(planIdClaim) && Guid.TryParse(planIdClaim, out var parsedPlanId))
                    selectedPlanId = parsedPlanId;

                return (userId, selectedPlanId);
            }
            catch
            {
                return (null, null);
            }
        }

        public async Task<object> GetQrCodeWithRecoveryCodeAsync(Guid userId, string recoveryCode)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null || !user.IsActive)
                throw new UnauthorizedAccessException("User not found or inactive.");

            if (string.IsNullOrEmpty(user.TotpSecret))
            {
                user.TotpSecret = GenerateTotpSecret();
                user.IsTotpConfigured = false;
                await _context.SaveChangesAsync();
            }

            var codeHash = HashRecoveryCode(recoveryCode);
            var recoveryCodeEntity = await _context.RecoveryCodes
                .FirstOrDefaultAsync(c => c.UserId == userId && c.CodeHash == codeHash && c.UsedAt == null);
            if (recoveryCodeEntity == null)
                throw new UnauthorizedAccessException("Invalid recovery code.");

            // Consume the recovery code
            recoveryCodeEntity.UsedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var qrCodeBase64 = GenerateQrCodeBase64(user.Email, user.TotpSecret);
            return new
            {
                qrCodeBase64,
                manualEntryKey = user.TotpSecret
            };
        }
    }
}
