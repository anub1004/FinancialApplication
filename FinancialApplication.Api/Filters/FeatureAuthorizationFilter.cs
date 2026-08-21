using System;
using System.Security.Claims;
using System.Threading.Tasks;
using FinancialApplication.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FinancialApplication.Api.Filters
{
    /// <summary>
    /// Action filter that enforces feature-based access control.
    /// Instantiated via <see cref="RequireFeatureAttribute"/> using the TypeFilter pattern so
    /// that DI services (IFeatureAccessResolver) are resolved from the request scope.
    /// 
    /// Returns:
    ///   401 Unauthorized — user is not authenticated
    ///   403 Forbidden   — user is authenticated but does not have the required feature
    /// </summary>
    public class FeatureAuthorizationFilter : IAsyncActionFilter
    {
        private readonly IFeatureAccessResolver _resolver;
        private readonly string _featureKey;

        public FeatureAuthorizationFilter(IFeatureAccessResolver resolver, string featureKey)
        {
            _resolver   = resolver;
            _featureKey = featureKey;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var user = context.HttpContext.User;

            // 401 — not authenticated
            if (user?.Identity == null || !user.Identity.IsAuthenticated)
            {
                context.Result = new UnauthorizedObjectResult(new
                {
                    error   = "Authentication required.",
                    feature = _featureKey
                });
                return;
            }

            // Extract UserId from ClaimTypes.NameIdentifier
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                context.Result = new UnauthorizedObjectResult(new
                {
                    error   = "Invalid or missing user identity claim.",
                    feature = _featureKey
                });
                return;
            }

            // 403 — authenticated but feature not available
            var hasFeature = await _resolver.HasFeatureAsync(userId, _featureKey);
            if (!hasFeature)
            {
                context.Result = new ObjectResult(new
                {
                    error      = "Feature not available on your current plan.",
                    featureKey = _featureKey,
                    upgradeUrl = "/plans"
                })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
                return;
            }

            await next();
        }
    }
}
