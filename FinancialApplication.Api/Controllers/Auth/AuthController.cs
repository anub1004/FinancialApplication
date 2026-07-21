using FinancialApp.Infrastructure.DTOs;
using FinancialApp.Infrastructure.Interfaces;
using FinancialApplication.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FinancialApplication.Api.Controllers.Auth
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// Register a new user. Returns TOTP setup data (QR code) — no JWT issued until TOTP verified.
        /// </summary>
        [HttpPost("register")]
        [ProducesResponseType(typeof(AuthenticationResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<AuthenticationResult>> Register([FromBody] RegisterUserDto request)
        {
            try
            {
                var result = await _authService.RegisterAsync(request);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Login Step 1: Validates credentials and returns a TOTP challenge.
        /// If the user hasn't set up TOTP yet, a QR code is included in the response.
        /// No JWT tokens are issued at this step.
        /// </summary>
        [HttpPost("login")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginUserDto request)
        {
            try
            {
                var result = await _authService.LoginStep1Async(request);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new 
                { 
                    isAuthenticated = false, 
                    message = ex.Message 
                });
            }
        }

        /// <summary>
        /// Login Step 2: Verifies the TOTP code and issues JWT tokens.
        /// This endpoint is called after login or google-login returns a TOTP challenge.
        /// </summary>
        [HttpPost("verify-totp")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> VerifyTotp([FromBody] TotpVerifyDto request)
        {
            try
            {
                var result = await _authService.VerifyTotpAndLoginAsync(request);

                // Set auth cookies (same as the previous login flow)
                Response.Cookies.Append("authToken", result.AccessToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Expires = DateTime.UtcNow.AddHours(1)
                });

                var refreshTokenExpireDays = Convert.ToInt32(HttpContext.RequestServices
                    .GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>()["Jwt:RefreshTokenExpireDays"] ?? "7");
                Response.Cookies.Append("refreshToken", result.RefreshToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Expires = DateTime.UtcNow.AddDays(refreshTokenExpireDays)
                });

                return Ok(new
                {
                    isAuthenticated = true,
                    token = result.AccessToken,
                    refreshtoken = result.RefreshToken,
                    expiresIn = result.ExpiresIn,
                    role = result.Role,
                    recoveryCodes = result.RecoveryCodes,
                    message = "Login successful"
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new
                {
                    isAuthenticated = false,
                    message = ex.Message
                });
            }
        }

        [HttpPost("recovery-login")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> RecoveryLogin([FromBody] RecoveryLoginDto request)
        {
            try
            {
                var result = await _authService.LoginWithRecoveryCodeAsync(request);
                Response.Cookies.Append("authToken", result.AccessToken, new CookieOptions
                {
                    HttpOnly = true, Secure = true, SameSite = SameSiteMode.None, Expires = DateTime.UtcNow.AddHours(1)
                });
                Response.Cookies.Append("refreshToken", result.RefreshToken, new CookieOptions
                {
                    HttpOnly = true, Secure = true, SameSite = SameSiteMode.None, Expires = DateTime.UtcNow.AddDays(7)
                });
                return Ok(new { isAuthenticated = true, token = result.AccessToken, refreshtoken = result.RefreshToken, expiresIn = result.ExpiresIn, role = result.Role, message = "Login successful" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { isAuthenticated = false, message = ex.Message });
            }
        }

        [HttpPost("request-email-recovery")]
        public async Task<IActionResult> RequestEmailLoginCode([FromBody] EmailLoginRequestDto request)
        {
            try { await _authService.RequestEmailLoginCodeAsync(request); }
            catch (InvalidOperationException ex) { return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = ex.Message }); }
            catch (System.Net.Mail.SmtpException) { return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Email service is temporarily unavailable." }); }
            return Ok(new { message = "If the account exists, a sign-in code has been sent." });
        }

        [HttpPost("email-verification-login")]
        public async Task<IActionResult> EmailVerificationLogin([FromBody] EmailLoginVerifyDto request)
        {
            try
            {
                var result = await _authService.LoginWithEmailCodeAsync(request);
                Response.Cookies.Append("authToken", result.AccessToken, new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.None, Expires = DateTime.UtcNow.AddHours(1) });
                Response.Cookies.Append("refreshToken", result.RefreshToken, new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.None, Expires = DateTime.UtcNow.AddDays(7) });
                return Ok(new { isAuthenticated = true, token = result.AccessToken, refreshtoken = result.RefreshToken, expiresIn = result.ExpiresIn, role = result.Role });
            }
            catch (UnauthorizedAccessException ex) { return Unauthorized(new { isAuthenticated = false, message = ex.Message }); }
        }

        [HttpPost("verify-signup-email-otp")]
        public async Task<IActionResult> VerifySignupEmailOtp([FromBody] EmailLoginVerifyDto request)
        {
            try
            {
                var result = await _authService.VerifySignupEmailOtpAsync(request);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Google Login Step 1: Validates Google token and returns a TOTP challenge.
        /// No JWT tokens are issued at this step.
        /// </summary>
        [HttpPost("google-login")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginDto request)
        {
            try
            {
                var result = await _authService.GoogleLoginStep1Async(request.IdToken);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { isAuthenticated = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { isAuthenticated = false, message = ex.Message });
            }
        }

        [Authorize]
        [HttpPost("logout")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Logout([FromBody] LogoutRequestDto request)
        {
            try
            {
             
                if (User?.Identity == null || !User.Identity.IsAuthenticated)
                {
                    return Unauthorized(new 
                    { 
                        message = "User is not authenticated. Ensure you include the Authorization: Bearer <token> header.",
                        debugInfo = "User.Identity.IsAuthenticated = false"
                    });
                }

                
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrWhiteSpace(userIdClaim))
                {
                    return Unauthorized(new 
                    { 
                        message = "User ID claim not found in token.",
                        debugInfo = $"Claims found: {string.Join(", ", User.Claims.Select(c => c.Type))}"
                    });
                }

                if (!Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new 
                    { 
                        message = "Invalid user ID format in token.",
                        debugInfo = $"Received: {userIdClaim}"
                    });
                }    
                if (string.IsNullOrWhiteSpace(request?.Token))
                {
                    return BadRequest(new 
                    { 
                        message = "Refresh token is required in request body.",
                        debugInfo = $"Request.Token is: {(request?.Token ?? "null")}"
                    });
                }  
                var result = await _authService._Logout(userId, request.Token);
                if (!result)
                {
                    return Unauthorized(new 
                    { 
                        message = "Logout failed. Invalid or expired refresh token.",
                        debugInfo = $"UserId: {userId}, Token exists in DB: false"
                    });
                }
                Response.Cookies.Delete("authToken", new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None
                });

                Response.Cookies.Delete("refreshToken", new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None
                });

                return Ok(new 
                { 
                    message = "Logged out successfully",
                    isAuthenticated = false
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new 
                { 
                    message = $"Logout failed: {ex.Message}",
                    debugInfo = ex.StackTrace
                });
            }
        }
         [HttpGet("checkauth")]
         [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
         public IActionResult CheckAuth()
         {
             try
             {
                 var token = Request.Cookies["authToken"] ?? 
                            Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

                 if (string.IsNullOrEmpty(token))
                     return Ok(new { isAuthenticated = false });

                 var claims = _authService.ValidateToken(token);
                 if (claims == null)
                     return Ok(new { isAuthenticated = false });

                 return Ok(new
                 {
                     isAuthenticated = true,
                     user = claims.FindFirst(ClaimTypes.Email)?.Value,
                     userId = claims.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                     role = claims.FindFirst(ClaimTypes.Role)?.Value
                 });
             }
             catch
             {
                 return Ok(new { isAuthenticated = false });
             }
         }

          [Authorize]
          [HttpPost("verify-recovery-code-for-qr")]
          [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
          [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
          [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
          public async Task<IActionResult> VerifyRecoveryCodeForQr([FromBody] VerifyRecoveryCodeDto request)
          {
              try
              {
                  var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                  if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                  {
                      return Unauthorized(new { message = "User ID claim not found or invalid in token." });
                  }

                  var result = await _authService.GetQrCodeWithRecoveryCodeAsync(userId, request.RecoveryCode);
                  return Ok(result);
              }
              catch (UnauthorizedAccessException ex)
              {
                  return Unauthorized(new { message = ex.Message });
              }
              catch (Exception ex)
              {
                  return BadRequest(new { message = ex.Message });
              }
          }
    }
}
