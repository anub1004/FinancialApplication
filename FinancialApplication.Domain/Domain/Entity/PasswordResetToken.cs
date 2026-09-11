using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinancialApplication.Domain.Domain.Entity
{
    /// <summary>
    /// Represents a time-limited password reset token.
    /// Tokens expire after a configurable duration and can only be used once.
    /// Primary key: Id (GUID)
    /// Foreign key: UserId (references Users table)
    /// </summary>
    public class PasswordResetToken
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid UserId { get; set; }

        /// <summary>
        /// The hashed reset token sent to the user's email.
        /// Stored hashed for security — the plain token is only sent via email.
        /// </summary>
        [Required]
        [MaxLength(256)]
        public string TokenHash { get; set; } = string.Empty;

        /// <summary>
        /// When this token expires. Typically 1 hour from creation.
        /// </summary>
        [Required]
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// Whether this token has already been used to reset a password.
        /// Prevents token reuse.
        /// </summary>
        [Required]
        public bool IsUsed { get; set; } = false;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
    }
}
