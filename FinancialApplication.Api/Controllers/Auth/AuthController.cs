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
        [HttpPost("login")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginUserDto request)
        {
            try
            {
                var result = await _authService.LoginAsync(request);

                // Set HTTP-only cookie with access token
                Response.Cookies.Append("authToken", result.AccessToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTime.UtcNow.AddHours(1)
                });

                // Set HTTP-only cookie with refresh token
                var refreshTokenExpireDays = Convert.ToInt32(HttpContext.RequestServices
                    .GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>()["Jwt:RefreshTokenExpireDays"] ?? "7");
                Response.Cookies.Append("refreshToken", result.RefreshToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTime.UtcNow.AddDays(refreshTokenExpireDays)
                });

                // Return only access token in response body
                return Ok(new
                {
                    isAuthenticated = true,
                    token = result.AccessToken,
                    refreshtoken=result.RefreshToken,
                    expiresIn = result.ExpiresIn,
                    role = result.Role,
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
                    SameSite = SameSiteMode.Strict
                });

                Response.Cookies.Delete("refreshToken", new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict
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
    }
}
