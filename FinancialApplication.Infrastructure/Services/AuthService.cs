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
using System;
using System.Collections.Generic;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using AuthenticationResultDto = FinancialApplication.Application.DTOs.AuthenticationResult;

namespace FinancialApp.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IJwtTokenGenerator _tokenGenerator;
        private readonly RefreshTokenGenerator _refreshTokenGenerator;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IConfiguration _configuration;

        public AuthService(
            AppDbContext context,
            IJwtTokenGenerator tokenGenerator,
            RefreshTokenGenerator refreshTokenGenerator,
            IPasswordHasher passwordHasher,
            IConfiguration configuration)
        {
            _context = context;
            _tokenGenerator = tokenGenerator;
            _refreshTokenGenerator = refreshTokenGenerator;
            _passwordHasher = passwordHasher;
            _configuration = configuration;
        }

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

            var user = new User 
            {
                Username = request.Username,
                Email = request.Email,
                Password = _passwordHasher.HashPassword(request.Password),
                RoleId = defaultRole.Id,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return await AuthenticateAsync(user.Id, user.Email, user.Username, defaultRole.Name);
        }

        public async Task<AuthenticationResultDto> LoginAsync(LoginUserDto request)
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

            return await AuthenticateAsync(
                user.Id,
                user.Email,
                user.Username,
                user.Role.Name
                
                
                
            );
        }

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
    }
}
