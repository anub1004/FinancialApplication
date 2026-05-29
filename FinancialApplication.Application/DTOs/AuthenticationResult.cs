using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinancialApplication.Application.DTOs
{
    public class AuthenticationResult
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public DateTime ExpiresAt { get; set; }
        public int ExpiresIn { get; set; }
        public string Role { get; set; }
    }
}
