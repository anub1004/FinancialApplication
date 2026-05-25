using FinancialApplication.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace FinancialApp.Infrastructure.Security
{
    /// <summary>
    /// Generates JWT tokens for authentication and authorization.
    /// Implements the token generation pattern from the architecture.
    /// </summary>
    public class JwtTokenGenerator : IJwtTokenGenerator
    {
        private readonly IConfiguration _configuration;

        public JwtTokenGenerator(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Generates an access token with user claims.
        /// Access token is short-lived (15 minutes by default).
        /// </summary>
        /// <param name="userId">User ID (GUID)</param>
        /// <param name="email">User email address</param>
        /// <param name="username">User username</param>
        /// <param name="role">User role (Admin, Manager, Auditor, User)</param>
        /// <returns>JWT access token</returns>
        public string GenerateAccessToken(Guid userId, string email, string username, string role)
        {
            var key = _configuration["Jwt:Key"];
            var issuer = _configuration["Jwt:Issuer"];
            var audience = _configuration["Jwt:Audience"];
            var durationMinutes = Convert.ToInt32(_configuration["Jwt:ExpireMinutes"] ?? "15");

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            // Build claims from architecture requirements
            var claims = new List<Claim>
            {
                // Standard claims
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role, role),

                // JWT specific claims
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())
            };

            // Add permission claims based on role
            AddPermissionClaims(claims, role);

            var expires = DateTime.UtcNow.AddMinutes(durationMinutes);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expires,
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>
        /// Generates a refresh token (long-lived, 7 days by default).
        /// Refresh tokens are used to generate new access tokens.
        /// </summary>
        /// <param name="userId">User ID (GUID)</param>
        /// <returns>JWT refresh token</returns>
        public string GenerateRefreshToken(Guid userId)
        {
            var key = _configuration["Jwt:Key"];
            var issuer = _configuration["Jwt:Issuer"];
            var audience = _configuration["Jwt:Audience"];
            var refreshTokenExpireDays = Convert.ToInt32(_configuration["Jwt:RefreshTokenExpireDays"] ?? "7");

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("token_type", "refresh")
            };

            var expires = DateTime.UtcNow.AddDays(refreshTokenExpireDays);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expires,
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>
        /// Adds permission claims based on user role.
        /// These claims are used by authorization policies.
        /// </summary>
        private void AddPermissionClaims(List<Claim> claims, string role)
        {
            switch (role)
            {
                case "Admin":
                    AddAdminPermissions(claims);
                    break;
                case "Manager":
                    AddManagerPermissions(claims);
                    break;
                case "Auditor":
                    AddAuditorPermissions(claims);
                    break;
                case "User":
                    // Regular users have no special permissions beyond their role
                    break;
            }
        }

        /// <summary>
        /// Admin permissions: Full system access.
        /// </summary>
        private void AddAdminPermissions(List<Claim> claims)
        {
            claims.AddRange(new[]
            {
                new Claim("permission", "view_all_users"),
                new Claim("permission", "edit_all_users"),
                new Claim("permission", "delete_users"),
                new Claim("permission", "manage_roles"),
                new Claim("permission", "view_audit_logs"),
                new Claim("permission", "create_users")
            });
        }

        /// <summary>
        /// Manager permissions: View all, manage users, create users.
        /// </summary>
        private void AddManagerPermissions(List<Claim> claims)
        {
            claims.AddRange(new[]
            {
                new Claim("permission", "view_all_users"),
                new Claim("permission", "view_audit_logs"),
                new Claim("permission", "create_users")
            });
        }

        /// <summary>
        /// Auditor permissions: Read-only access and audit logs.
        /// </summary>
        private void AddAuditorPermissions(List<Claim> claims)
        {
            claims.AddRange(new[]
            {
                new Claim("permission", "view_all_users"),
                new Claim("permission", "view_audit_logs")
            });
        }

        /// <summary>
        /// Validates a JWT token and extracts the user ID from claims.
        /// </summary>
        /// <param name="token">JWT token to validate</param>
        /// <returns>User ID if valid, null if invalid</returns>
        public Guid? ValidateTokenAndGetUserId(string token)
        {
            try
            {
                var key = _configuration["Jwt:Key"];
                var issuer = _configuration["Jwt:Issuer"];
                var audience = _configuration["Jwt:Audience"];

                var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));

                var handler = new JwtSecurityTokenHandler();
                var principal = handler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = securityKey,
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return userId;
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Extracts claims from a JWT token without validation.
        /// Use only for token inspection, not security decisions.
        /// </summary>
        public ClaimsPrincipal GetPrincipalFromToken(string token)
        {
            var key = _configuration["Jwt:Key"];
            var issuer = _configuration["Jwt:Issuer"];
            var audience = _configuration["Jwt:Audience"];

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));

            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = securityKey,
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateLifetime = false
            };

            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);

            if (!(securityToken is JwtSecurityToken jwtSecurityToken) ||
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new SecurityTokenException("Invalid token");
            }

            return principal;
        }
    }
}

