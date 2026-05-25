using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FinancialApp.Infrastructure.DTOs;
using FinancialApp.Infrastructure.Interfaces;
using FinancialApp.Infrastructure.Security;
using AuthenticationResultDto = FinancialApplication.Application.DTOs.AuthenticationResult;
using FinancialApplication.Application.DTOs;
using FinancialApplication.Domain.Domain.Entity;
using FinancialApplication.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using FinancialApplication.Application.Interfaces;

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
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null || !user.IsActive || !_passwordHasher.VerifyPassword(request.Password, user.Password))
            {
                throw new UnauthorizedAccessException("Invalid email or password.");
            }

            return await AuthenticateAsync(user.Id, user.Email, user.Username, user.Role.Name);
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
                ExpiresIn = expiresIn
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

        public async Task<bool> Logout(Guid userId, string token)
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
    }
}
