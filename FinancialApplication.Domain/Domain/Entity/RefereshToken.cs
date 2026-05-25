using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinancialApplication.Domain.Domain.Entity
{
    /// <summary>
    /// Represents a refresh token for JWT token renewal.
    /// Primary key: RefreshTokenId (GUID)
    /// Foreign key: UserId (references Users table)
    /// </summary>
    public class RefreshToken
    {
        [Key]
        public Guid RefreshTokenId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid UserId { get; set; }

        [Required]
        [MaxLength(500)]
        public string Token { get; set; } = string.Empty;

        [Required]
        public DateTime ExpiryDate { get; set; }

        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}
