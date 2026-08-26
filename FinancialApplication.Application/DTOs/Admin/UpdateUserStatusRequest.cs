using System.ComponentModel.DataAnnotations;

namespace FinancialApplication.Application.DTOs.Admin
{
    /// <summary>
    /// Request body for toggling a user's active status (enable/disable).
    /// </summary>
    public class UpdateUserStatusRequest
    {
        [Required]
        public bool IsActive { get; set; }
    }
}
