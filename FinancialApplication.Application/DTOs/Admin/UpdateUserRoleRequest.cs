using System.ComponentModel.DataAnnotations;

namespace FinancialApplication.Application.DTOs.Admin
{
    /// <summary>
    /// Request body for changing a user's role by name.
    /// </summary>
    public class UpdateUserRoleRequest
    {
        [Required]
        [MaxLength(50)]
        public string RoleName { get; set; } = string.Empty;
    }
}
