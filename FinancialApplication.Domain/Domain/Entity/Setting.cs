using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinancialApplication.Domain.Domain.Entity
{
    public class Setting
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid UserId { get; set; }

        [EmailAddress]
        [MaxLength(100)]
        public string? Email { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(10)]
        public string Currency { get; set; } = "INR";

        [Required]
        [MaxLength(10)]
        public string DefaultFy { get; set; } = "2025-26";

        [Required]
        [MaxLength(20)]
        public string PreferredRegime { get; set; } = "New";

        [Required]
        [MaxLength(20)]
        public string AvatarColor { get; set; } = "#6366f1";

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

       
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
    }
}
