using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinancialApplication.Domain.Domain.Entity
{
    public class EmailLoginCode
    {
        [Key] public Guid Id { get; set; } = Guid.NewGuid();
        [Required] public Guid UserId { get; set; }
        [Required, MaxLength(64)] public string CodeHash { get; set; } = string.Empty;
        [Required] public DateTime ExpiresAt { get; set; }
        public DateTime? UsedAt { get; set; }
        [ForeignKey(nameof(UserId))] public virtual User User { get; set; } = null!;
    }
}
