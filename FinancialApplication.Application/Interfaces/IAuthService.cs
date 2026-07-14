using FinancialApp.Infrastructure.DTOs;
using FinancialApplication.Application.DTOs;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FinancialApp.Infrastructure.Interfaces
{
    public interface IAuthService
    {
        Task<AuthenticationResult> RegisterAsync(RegisterUserDto request);

        Task<AuthenticationResult> LoginAsync(LoginUserDto request);

        Task<AuthenticationResult> AuthenticateAsync(Guid userId, string email, string username, string role);

        Task<string> RefreshAccessTokenAsync(string refreshToken);

        Guid? ValidateAccessToken(string token);

        Task<bool> ValidateRefreshTokenAsync(Guid userId, string refreshToken);

        Task<bool> _Logout(Guid userId, string token);

        Task<AuthDto> CheckAuth(Guid userId, string token);

        ClaimsPrincipal ValidateToken(string token);

        Task<AuthenticationResult> GoogleLoginAsync(string idToken);
    }
}
