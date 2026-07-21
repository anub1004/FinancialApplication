using System.ComponentModel.DataAnnotations;

namespace FinancialApplication.Application.DTOs
{
    public class VerifyRecoveryCodeDto
    {
        [Required]
        public string RecoveryCode { get; set; } = string.Empty;
    }
}
