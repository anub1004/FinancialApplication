using FinancialApplication.Domain.Domain.Entity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks; 

namespace FinancialApplication.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {

        }
       public DbSet<FinancialApplication.Domain.Domain.Entity.User> Users { get; set; }
         public DbSet<FinancialApplication.Domain.Domain.Entity.Transaction> Transactions { get; set; }
        public DbSet<FinancialApplication.Domain.Domain.Entity.Investment> Investments { get; set; }
        public DbSet<FinancialApplication.Domain.Domain.Entity.Goal> Goals { get; set; }
        public DbSet<FinancialApplication.Domain.Domain.Entity.Role> Roles { get; set; }
        public DbSet<FinancialApplication.Domain.Domain.Entity.RefreshToken> RefreshTokens { get; set; }
        public DbSet<FinancialApplication.Domain.Domain.Entity.AuditLog> AuditLogs { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ROLE CONFIG
            modelBuilder.Entity<Role>(entity =>
            {
                entity.HasKey(r => r.Id);

                entity.HasIndex(r => r.Name).IsUnique();

                entity.Property(r => r.Name).IsRequired().HasMaxLength(50);
                entity.Property(r => r.IsActive).HasDefaultValue(true);
           
               

                // Seed initial roles
                entity.HasData(
   new Role { Id = 1, Name = "User", IsActive = true, },
   new Role { Id = 2, Name = "Admin", IsActive = true,  },
   new Role { Id = 3, Name = "Manager", IsActive = true,  },
   new Role { Id = 4, Name = "Auditor", IsActive = true,  }
);
            });

            // USER CONFIG
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id);

                entity.HasIndex(u => u.Email).IsUnique(); // important
                entity.HasIndex(u => u.Username).IsUnique();

                entity.HasOne(u => u.Role)
                      .WithMany(r => r.Users)
                      .HasForeignKey(u => u.RoleId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // REFRESH TOKEN CONFIG
            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.HasKey(r => r.RefreshTokenId);

                entity.HasOne<User>()
                      .WithMany()
                      .HasForeignKey(r => r.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasKey(a => a.AuditLogId);

                entity.HasOne(a => a.User)
                      .WithMany()
                      .HasForeignKey(a => a.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

        }
    }

}

