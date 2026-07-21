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

        /// <summary>
        /// Step 1 of login: validates credentials, returns TOTP challenge.
        /// No JWT is issued at this step.
        /// </summary>
        Task<object> LoginStep1Async(LoginUserDto request);

        /// <summary>
        /// Step 1 of Google login: validates Google token, returns TOTP challenge.
        /// No JWT is issued at this step.
        /// </summary>
        Task<object> GoogleLoginStep1Async(string idToken);

        /// <summary>
        /// Step 2: verifies the TOTP code and issues JWT tokens.
        /// Used after both normal login and Google login step 1.
        /// </summary>
        Task<AuthenticationResult> VerifyTotpAndLoginAsync(TotpVerifyDto request);

        Task<AuthenticationResult> LoginWithRecoveryCodeAsync(RecoveryLoginDto request);
        Task RequestEmailLoginCodeAsync(EmailLoginRequestDto request);
        Task<AuthenticationResult> LoginWithEmailCodeAsync(EmailLoginVerifyDto request);
        Task<AuthenticationResult> VerifySignupEmailOtpAsync(EmailLoginVerifyDto request);

        Task<AuthenticationResult> AuthenticateAsync(Guid userId, string email, string username, string role);

        Task<string> RefreshAccessTokenAsync(string refreshToken);

        Guid? ValidateAccessToken(string token);

        Task<bool> ValidateRefreshTokenAsync(Guid userId, string refreshToken);

        Task<bool> _Logout(Guid userId, string token);

        Task<AuthDto> CheckAuth(Guid userId, string token);

        ClaimsPrincipal ValidateToken(string token);

        Task<object> GetQrCodeWithRecoveryCodeAsync(Guid userId, string recoveryCode);
    }
}
