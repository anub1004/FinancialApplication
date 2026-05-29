using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinancialApplication.Application.DTOs
{
    public class AuthDto
    {
        public Guid UserId { get; set; }
        public string user { get; set; }
        public string role { get; set; }
    }
}
