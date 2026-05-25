using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace FinancialApplication.Application.Interfaces
{
    public interface IJwtTokenGenerator
    {
        string GenerateAccessToken(Guid userId, string email, string username, string role);

        string GenerateRefreshToken(Guid userId);

        Guid? ValidateTokenAndGetUserId(string token);

        ClaimsPrincipal GetPrincipalFromToken(string token);
    }
}
