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
        public DbSet<RecoveryCode> RecoveryCodes { get; set; }
        public DbSet<EmailLoginCode> EmailLoginCodes { get; set; }
        public DbSet<FinancialApplication.Domain.Domain.Entity.AuditLog> AuditLogs { get; set; }
        public DbSet<FinancialApplication.Domain.Domain.Entity.FinanceNewsArticle> FinanceNewsArticles { get; set; }
        public DbSet<FinancialApplication.Domain.Domain.Entity.TodayNewsArticle> TodayNewsArticles { get; set; }
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

                entity.HasIndex(u => u.GoogleId)
                      .IsUnique()
                      .HasFilter("[GoogleId] IS NOT NULL");

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

            modelBuilder.Entity<RecoveryCode>(entity =>
            {
                entity.HasKey(r => r.Id);
                entity.HasIndex(r => new { r.UserId, r.CodeHash }).IsUnique();
                entity.HasOne(r => r.User)
                      .WithMany(u => u.RecoveryCodes)
                      .HasForeignKey(r => r.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<EmailLoginCode>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.HasIndex(c => new { c.UserId, c.CodeHash });
                entity.HasOne(c => c.User).WithMany(u => u.EmailLoginCodes).HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasKey(a => a.AuditLogId);

                entity.HasOne(a => a.User)
                      .WithMany()
                      .HasForeignKey(a => a.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // FINANCE NEWS ARTICLE CONFIG
            modelBuilder.Entity<FinanceNewsArticle>(entity =>
            {
                entity.HasKey(n => n.Id);

                entity.Property(n => n.JsonData)
                      .IsRequired()
                      .HasColumnType("nvarchar(max)");

                entity.Property(n => n.ArticleCount)
                      .HasDefaultValue(0);

                entity.Property(n => n.CreatedAt)
                      .HasDefaultValueSql("GETUTCDATE()");

                entity.HasIndex(n => n.CreatedAt);
            });

            // TODAY NEWS ARTICLE CONFIG
            modelBuilder.Entity<TodayNewsArticle>(entity =>
            {
                entity.HasKey(n => n.Id);

                entity.Property(n => n.JsonData)
                      .IsRequired()
                      .HasColumnType("nvarchar(max)");

                entity.Property(n => n.ArticleCount)
                      .HasDefaultValue(0);

                entity.Property(n => n.CreatedAt)
                      .HasDefaultValueSql("GETUTCDATE()");

                entity.HasIndex(n => n.CreatedAt);
            });

        }
    }

}

