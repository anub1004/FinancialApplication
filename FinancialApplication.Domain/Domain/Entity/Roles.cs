using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinancialApplication.Domain.Domain.Entity
{
    /// <summary>
    /// Represents a role in the system (Admin, Manager, Auditor, User).
    /// Used for role-based access control (RBAC).
    /// Primary key: Id (int)
    /// </summary>
    [Table("Roles")]
    public class Role
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public bool IsActive { get; set; } = true;

        // Navigation property
        public virtual ICollection<User> Users { get; set; } = new List<User>();
    }
}
