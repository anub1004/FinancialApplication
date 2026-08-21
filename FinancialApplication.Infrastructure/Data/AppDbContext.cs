using FinancialApplication.Domain.Domain.Entity;
using FinancialApplication.Domain.Domain.Enums;
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

        // ── Subscription System DbSets ──────────────────────────────────────────
        public DbSet<Plan> Plans { get; set; }
        public DbSet<Feature> Features { get; set; }
        public DbSet<PlanFeature> PlanFeatures { get; set; }
        public DbSet<UserSubscription> UserSubscriptions { get; set; }
        public DbSet<SubscriptionHistory> SubscriptionHistories { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<FeatureAudit> FeatureAudits { get; set; }
        public DbSet<PlanAudit> PlanAudits { get; set; }
        public DbSet<PlanPriceHistory> PlanPriceHistories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ════════════════════════════════════════════════════════════════════
            // EXISTING ENTITY CONFIGURATIONS (preserved exactly as-is)
            // ════════════════════════════════════════════════════════════════════

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

            // ════════════════════════════════════════════════════════════════════
            // SUBSCRIPTION SYSTEM CONFIGURATIONS
            // ════════════════════════════════════════════════════════════════════

            ConfigurePlan(modelBuilder);
            ConfigureFeature(modelBuilder);
            ConfigurePlanFeature(modelBuilder);
            ConfigureUserSubscription(modelBuilder);
            ConfigureSubscriptionHistory(modelBuilder);
            ConfigurePayment(modelBuilder);
            ConfigureInvoice(modelBuilder);
            ConfigureFeatureAudit(modelBuilder);
            ConfigurePlanAudit(modelBuilder);
            ConfigurePlanPriceHistory(modelBuilder);
        }

        // ════════════════════════════════════════════════════════════════════
        // PLAN CONFIGURATION
        // ════════════════════════════════════════════════════════════════════
        private static void ConfigurePlan(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Plan>(entity =>
            {
                entity.HasKey(p => p.Id);

                // Unique constraints
                entity.HasIndex(p => p.Name).IsUnique();
                entity.HasIndex(p => p.Slug).IsUnique();

                // Performance indexes
                entity.HasIndex(p => p.IsActive);
                entity.HasIndex(p => p.SortOrder);

                // Property configs
                entity.Property(p => p.Name).IsRequired().HasMaxLength(100);
                entity.Property(p => p.Slug).IsRequired().HasMaxLength(100);
                entity.Property(p => p.Description).HasMaxLength(1000);
                entity.Property(p => p.MonthlyPrice).HasColumnType("decimal(18,2)");
                entity.Property(p => p.AnnualPrice).HasColumnType("decimal(18,2)");
                entity.Property(p => p.Currency).IsRequired().HasMaxLength(10).HasDefaultValue("INR");
                entity.Property(p => p.SortOrder).HasDefaultValue(0);
                entity.Property(p => p.IsActive).HasDefaultValue(true);
                entity.Property(p => p.IsDefault).HasDefaultValue(false);
                entity.Property(p => p.TrialDays).HasDefaultValue(0);
                entity.Property(p => p.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
                entity.Property(p => p.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

                // Seed data — fixed GUIDs to prevent duplication on re-run
                entity.HasData(
                    new Plan
                    {
                        Id = new Guid("a0000000-0000-0000-0000-000000000001"),
                        Name = "Free",
                        Slug = "free",
                        Description = "Get started with essential financial tools at no cost.",
                        MonthlyPrice = 0m,
                        AnnualPrice = 0m,
                        Currency = "INR",
                        SortOrder = 1,
                        IsActive = true,
                        IsDefault = true,
                        TrialDays = 0,
                        CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                        UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                    },
                    new Plan
                    {
                        Id = new Guid("a0000000-0000-0000-0000-000000000002"),
                        Name = "Basic",
                        Slug = "basic",
                        Description = "Essential features for personal finance management.",
                        MonthlyPrice = 499m,
                        AnnualPrice = 4999m,
                        Currency = "INR",
                        SortOrder = 2,
                        IsActive = true,
                        IsDefault = false,
                        TrialDays = 7,
                        CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                        UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                    },
                    new Plan
                    {
                        Id = new Guid("a0000000-0000-0000-0000-000000000003"),
                        Name = "Advanced",
                        Slug = "advanced",
                        Description = "Advanced analytics and reporting for serious investors.",
                        MonthlyPrice = 999m,
                        AnnualPrice = 9999m,
                        Currency = "INR",
                        SortOrder = 3,
                        IsActive = true,
                        IsDefault = false,
                        TrialDays = 14,
                        CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                        UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                    },
                    new Plan
                    {
                        Id = new Guid("a0000000-0000-0000-0000-000000000004"),
                        Name = "Pro",
                        Slug = "pro",
                        Description = "Full access to all features including AI-powered insights and premium support.",
                        MonthlyPrice = 1499m,
                        AnnualPrice = 14999m,
                        Currency = "INR",
                        SortOrder = 4,
                        IsActive = true,
                        IsDefault = false,
                        TrialDays = 14,
                        CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                        UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                    }
                );
            });
        }

        // ════════════════════════════════════════════════════════════════════
        // FEATURE CONFIGURATION
        // ════════════════════════════════════════════════════════════════════
        private static void ConfigureFeature(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Feature>(entity =>
            {
                entity.HasKey(f => f.Id);

                // Unique constraint on FeatureKey
                entity.HasIndex(f => f.FeatureKey).IsUnique();

                // Performance indexes
                entity.HasIndex(f => f.Category);
                entity.HasIndex(f => f.IsActive);

                // Property configs
                entity.Property(f => f.FeatureKey).IsRequired().HasMaxLength(100);
                entity.Property(f => f.DisplayName).IsRequired().HasMaxLength(200);
                entity.Property(f => f.Description).HasMaxLength(500);
                entity.Property(f => f.Category).HasMaxLength(100);
                entity.Property(f => f.IsActive).HasDefaultValue(true);
                entity.Property(f => f.SortOrder).HasDefaultValue(0);
                entity.Property(f => f.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
                entity.Property(f => f.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

                // Seed data — 15 features with fixed GUIDs
                entity.HasData(
                    new Feature { Id = new Guid("b0000000-0000-0000-0000-000000000001"), FeatureKey = "dashboard", DisplayName = "Dashboard", Description = "Main financial dashboard with overview and widgets.", Category = "Core", IsActive = true, SortOrder = 1, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new Feature { Id = new Guid("b0000000-0000-0000-0000-000000000002"), FeatureKey = "transactions", DisplayName = "Transactions", Description = "Track income and expense transactions.", Category = "Core", IsActive = true, SortOrder = 2, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new Feature { Id = new Guid("b0000000-0000-0000-0000-000000000003"), FeatureKey = "news", DisplayName = "Financial News", Description = "Access curated financial news articles.", Category = "Core", IsActive = true, SortOrder = 3, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new Feature { Id = new Guid("b0000000-0000-0000-0000-000000000004"), FeatureKey = "profile", DisplayName = "Profile Management", Description = "Manage user profile and account settings.", Category = "Core", IsActive = true, SortOrder = 4, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new Feature { Id = new Guid("b0000000-0000-0000-0000-000000000005"), FeatureKey = "security_settings", DisplayName = "Security Settings", Description = "Configure 2FA, recovery codes, and login security.", Category = "Core", IsActive = true, SortOrder = 5, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new Feature { Id = new Guid("b0000000-0000-0000-0000-000000000006"), FeatureKey = "onboarding", DisplayName = "Onboarding", Description = "Guided setup and onboarding experience.", Category = "Core", IsActive = true, SortOrder = 6, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new Feature { Id = new Guid("b0000000-0000-0000-0000-000000000007"), FeatureKey = "analytics", DisplayName = "Analytics", Description = "Basic financial analytics and charts.", Category = "Analytics", IsActive = true, SortOrder = 7, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new Feature { Id = new Guid("b0000000-0000-0000-0000-000000000008"), FeatureKey = "investment_tracking", DisplayName = "Investment Tracking", Description = "Monitor and manage investment portfolio.", Category = "Investments", IsActive = true, SortOrder = 8, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new Feature { Id = new Guid("b0000000-0000-0000-0000-000000000009"), FeatureKey = "cards", DisplayName = "Cards Management", Description = "Manage payment cards and linked accounts.", Category = "Finance", IsActive = true, SortOrder = 9, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new Feature { Id = new Guid("b0000000-0000-0000-0000-000000000010"), FeatureKey = "reports", DisplayName = "Reports", Description = "Generate financial reports and summaries.", Category = "Reports", IsActive = true, SortOrder = 10, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new Feature { Id = new Guid("b0000000-0000-0000-0000-000000000011"), FeatureKey = "export_pdf", DisplayName = "Export PDF", Description = "Export reports and data as PDF documents.", Category = "Reports", IsActive = true, SortOrder = 11, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new Feature { Id = new Guid("b0000000-0000-0000-0000-000000000012"), FeatureKey = "export_csv", DisplayName = "Export CSV", Description = "Export data as CSV files for spreadsheet use.", Category = "Reports", IsActive = true, SortOrder = 12, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new Feature { Id = new Guid("b0000000-0000-0000-0000-000000000013"), FeatureKey = "premium_analytics", DisplayName = "Premium Analytics", Description = "Advanced analytics with trend analysis and predictions.", Category = "Analytics", IsActive = true, SortOrder = 13, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new Feature { Id = new Guid("b0000000-0000-0000-0000-000000000014"), FeatureKey = "ai_suggestions", DisplayName = "AI Suggestions", Description = "AI-powered financial insights and recommendations.", Category = "AI", IsActive = true, SortOrder = 14, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new Feature { Id = new Guid("b0000000-0000-0000-0000-000000000015"), FeatureKey = "user_management", DisplayName = "User Management", Description = "Admin: manage users, roles, and permissions.", Category = "Admin", IsActive = true, SortOrder = 15, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
                );
            });
        }

        // ════════════════════════════════════════════════════════════════════
        // PLAN FEATURE CONFIGURATION (Junction Table)
        // ════════════════════════════════════════════════════════════════════
        private static void ConfigurePlanFeature(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PlanFeature>(entity =>
            {
                entity.HasKey(pf => pf.Id);

                // Composite unique constraint — prevents duplicate plan-feature assignments
                entity.HasIndex(pf => new { pf.PlanId, pf.FeatureId }).IsUnique();

                // FK indexes for join performance
                entity.HasIndex(pf => pf.PlanId);
                entity.HasIndex(pf => pf.FeatureId);

                entity.Property(pf => pf.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

                // Relationships — CASCADE delete on both sides
                entity.HasOne(pf => pf.Plan)
                      .WithMany(p => p.PlanFeatures)
                      .HasForeignKey(pf => pf.PlanId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(pf => pf.Feature)
                      .WithMany(f => f.PlanFeatures)
                      .HasForeignKey(pf => pf.FeatureId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Seed data — Plan-Feature mappings
                // Plan GUIDs: Free=...0001, Basic=...0002, Advanced=...0003, Pro=...0004
                // Feature GUIDs: b0000000-...-000000000001 through ...000000000015

                // FREE plan: dashboard, transactions, news, profile, security_settings, onboarding (6 features)
                entity.HasData(
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0001-000000000001"), PlanId = new Guid("a0000000-0000-0000-0000-000000000001"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000001"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0001-000000000002"), PlanId = new Guid("a0000000-0000-0000-0000-000000000001"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000002"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0001-000000000003"), PlanId = new Guid("a0000000-0000-0000-0000-000000000001"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000003"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0001-000000000004"), PlanId = new Guid("a0000000-0000-0000-0000-000000000001"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000004"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0001-000000000005"), PlanId = new Guid("a0000000-0000-0000-0000-000000000001"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000005"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0001-000000000006"), PlanId = new Guid("a0000000-0000-0000-0000-000000000001"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000006"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },

                    // BASIC plan: Free features + analytics, investment_tracking, cards (9 features)
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0002-000000000001"), PlanId = new Guid("a0000000-0000-0000-0000-000000000002"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000001"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0002-000000000002"), PlanId = new Guid("a0000000-0000-0000-0000-000000000002"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000002"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0002-000000000003"), PlanId = new Guid("a0000000-0000-0000-0000-000000000002"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000003"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0002-000000000004"), PlanId = new Guid("a0000000-0000-0000-0000-000000000002"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000004"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0002-000000000005"), PlanId = new Guid("a0000000-0000-0000-0000-000000000002"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000005"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0002-000000000006"), PlanId = new Guid("a0000000-0000-0000-0000-000000000002"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000006"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0002-000000000007"), PlanId = new Guid("a0000000-0000-0000-0000-000000000002"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000007"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0002-000000000008"), PlanId = new Guid("a0000000-0000-0000-0000-000000000002"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000008"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0002-000000000009"), PlanId = new Guid("a0000000-0000-0000-0000-000000000002"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000009"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },

                    // ADVANCED plan: Basic features + reports, export_pdf, export_csv, premium_analytics (13 features)
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0003-000000000001"), PlanId = new Guid("a0000000-0000-0000-0000-000000000003"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000001"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0003-000000000002"), PlanId = new Guid("a0000000-0000-0000-0000-000000000003"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000002"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0003-000000000003"), PlanId = new Guid("a0000000-0000-0000-0000-000000000003"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000003"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0003-000000000004"), PlanId = new Guid("a0000000-0000-0000-0000-000000000003"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000004"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0003-000000000005"), PlanId = new Guid("a0000000-0000-0000-0000-000000000003"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000005"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0003-000000000006"), PlanId = new Guid("a0000000-0000-0000-0000-000000000003"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000006"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0003-000000000007"), PlanId = new Guid("a0000000-0000-0000-0000-000000000003"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000007"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0003-000000000008"), PlanId = new Guid("a0000000-0000-0000-0000-000000000003"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000008"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0003-000000000009"), PlanId = new Guid("a0000000-0000-0000-0000-000000000003"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000009"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0003-000000000010"), PlanId = new Guid("a0000000-0000-0000-0000-000000000003"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000010"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0003-000000000011"), PlanId = new Guid("a0000000-0000-0000-0000-000000000003"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000011"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0003-000000000012"), PlanId = new Guid("a0000000-0000-0000-0000-000000000003"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000012"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0003-000000000013"), PlanId = new Guid("a0000000-0000-0000-0000-000000000003"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000013"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },

                    // PRO plan: ALL 15 features
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0004-000000000001"), PlanId = new Guid("a0000000-0000-0000-0000-000000000004"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000001"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0004-000000000002"), PlanId = new Guid("a0000000-0000-0000-0000-000000000004"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000002"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0004-000000000003"), PlanId = new Guid("a0000000-0000-0000-0000-000000000004"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000003"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0004-000000000004"), PlanId = new Guid("a0000000-0000-0000-0000-000000000004"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000004"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0004-000000000005"), PlanId = new Guid("a0000000-0000-0000-0000-000000000004"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000005"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0004-000000000006"), PlanId = new Guid("a0000000-0000-0000-0000-000000000004"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000006"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0004-000000000007"), PlanId = new Guid("a0000000-0000-0000-0000-000000000004"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000007"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0004-000000000008"), PlanId = new Guid("a0000000-0000-0000-0000-000000000004"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000008"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0004-000000000009"), PlanId = new Guid("a0000000-0000-0000-0000-000000000004"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000009"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0004-000000000010"), PlanId = new Guid("a0000000-0000-0000-0000-000000000004"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000010"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0004-000000000011"), PlanId = new Guid("a0000000-0000-0000-0000-000000000004"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000011"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0004-000000000012"), PlanId = new Guid("a0000000-0000-0000-0000-000000000004"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000012"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0004-000000000013"), PlanId = new Guid("a0000000-0000-0000-0000-000000000004"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000013"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0004-000000000014"), PlanId = new Guid("a0000000-0000-0000-0000-000000000004"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000014"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0004-000000000015"), PlanId = new Guid("a0000000-0000-0000-0000-000000000004"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000015"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
                );
            });
        }

        // ════════════════════════════════════════════════════════════════════
        // USER SUBSCRIPTION CONFIGURATION
        // ════════════════════════════════════════════════════════════════════
        private static void ConfigureUserSubscription(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserSubscription>(entity =>
            {
                entity.HasKey(us => us.Id);

                // Filtered unique index — only one Active or Trial subscription per user
                entity.HasIndex(us => us.UserId)
                      .IsUnique()
                      .HasFilter("[Status] IN ('Active', 'Trial')");

                // Performance indexes
                entity.HasIndex(us => us.Status);
                entity.HasIndex(us => us.EndDate);
                entity.HasIndex(us => us.PlanId);

                // Enum → string conversions
                entity.Property(us => us.Status)
                      .IsRequired()
                      .HasMaxLength(20)
                      .HasConversion<string>();

                entity.Property(us => us.BillingCycle)
                      .IsRequired()
                      .HasMaxLength(20)
                      .HasConversion<string>();

                // Property configs
                entity.Property(us => us.CancelReason).HasMaxLength(500);
                entity.Property(us => us.AutoRenew).HasDefaultValue(true);
                entity.Property(us => us.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
                entity.Property(us => us.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

                // Relationships
                entity.HasOne(us => us.User)
                      .WithMany(u => u.Subscriptions)
                      .HasForeignKey(us => us.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(us => us.Plan)
                      .WithMany(p => p.UserSubscriptions)
                      .HasForeignKey(us => us.PlanId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }

        // ════════════════════════════════════════════════════════════════════
        // SUBSCRIPTION HISTORY CONFIGURATION
        // ════════════════════════════════════════════════════════════════════
        private static void ConfigureSubscriptionHistory(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SubscriptionHistory>(entity =>
            {
                entity.HasKey(sh => sh.Id);

                // Performance indexes
                entity.HasIndex(sh => sh.UserId);
                entity.HasIndex(sh => sh.CreatedAt).IsDescending();

                // Enum → string conversion
                entity.Property(sh => sh.Action)
                      .IsRequired()
                      .HasMaxLength(50)
                      .HasConversion<string>();

                // Property configs
                entity.Property(sh => sh.Notes).HasMaxLength(500);
                entity.Property(sh => sh.PerformedBy).IsRequired().HasMaxLength(50).HasDefaultValue("System");
                entity.Property(sh => sh.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

                // Relationships
                entity.HasOne(sh => sh.User)
                      .WithMany(u => u.SubscriptionHistories)
                      .HasForeignKey(sh => sh.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(sh => sh.UserSubscription)
                      .WithMany(us => us.SubscriptionHistories)
                      .HasForeignKey(sh => sh.SubscriptionId)
                      .OnDelete(DeleteBehavior.NoAction);
            });
        }

        // ════════════════════════════════════════════════════════════════════
        // PAYMENT CONFIGURATION
        // ════════════════════════════════════════════════════════════════════
        private static void ConfigurePayment(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Payment>(entity =>
            {
                entity.HasKey(p => p.Id);

                // Performance indexes
                entity.HasIndex(p => p.UserId);
                entity.HasIndex(p => p.Status);
                entity.HasIndex(p => p.CreatedAt).IsDescending();

                // Enum → string conversion
                entity.Property(p => p.Status)
                      .IsRequired()
                      .HasMaxLength(20)
                      .HasConversion<string>();

                // Property configs
                entity.Property(p => p.Amount).HasColumnType("decimal(18,2)");
                entity.Property(p => p.Currency).IsRequired().HasMaxLength(10).HasDefaultValue("INR");
                entity.Property(p => p.PaymentMethod).HasMaxLength(50);
                entity.Property(p => p.TransactionRef).HasMaxLength(200);
                entity.Property(p => p.GatewayResponse).HasColumnType("nvarchar(max)");
                entity.Property(p => p.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

                // Relationships
                entity.HasOne(p => p.User)
                      .WithMany(u => u.Payments)
                      .HasForeignKey(p => p.UserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(p => p.UserSubscription)
                      .WithMany(us => us.Payments)
                      .HasForeignKey(p => p.SubscriptionId)
                      .OnDelete(DeleteBehavior.NoAction);
            });
        }

        // ════════════════════════════════════════════════════════════════════
        // INVOICE CONFIGURATION
        // ════════════════════════════════════════════════════════════════════
        private static void ConfigureInvoice(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Invoice>(entity =>
            {
                entity.HasKey(i => i.Id);

                // Unique constraint on InvoiceNumber
                entity.HasIndex(i => i.InvoiceNumber).IsUnique();

                // Performance index
                entity.HasIndex(i => i.UserId);

                // Enum → string conversion
                entity.Property(i => i.Status)
                      .IsRequired()
                      .HasMaxLength(20)
                      .HasConversion<string>();

                // Property configs
                entity.Property(i => i.InvoiceNumber).IsRequired().HasMaxLength(50);
                entity.Property(i => i.Amount).HasColumnType("decimal(18,2)");
                entity.Property(i => i.Tax).HasColumnType("decimal(18,2)").HasDefaultValue(0m);
                entity.Property(i => i.TotalAmount).HasColumnType("decimal(18,2)");
                entity.Property(i => i.Currency).IsRequired().HasMaxLength(10).HasDefaultValue("INR");
                entity.Property(i => i.IssuedAt).HasDefaultValueSql("SYSUTCDATETIME()");

                // Relationships
                entity.HasOne(i => i.User)
                      .WithMany(u => u.Invoices)
                      .HasForeignKey(i => i.UserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(i => i.Payment)
                      .WithMany(p => p.Invoices)
                      .HasForeignKey(i => i.PaymentId)
                      .OnDelete(DeleteBehavior.SetNull);
            });
        }

        // ════════════════════════════════════════════════════════════════════
        // FEATURE AUDIT CONFIGURATION
        // ════════════════════════════════════════════════════════════════════
        private static void ConfigureFeatureAudit(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<FeatureAudit>(entity =>
            {
                entity.HasKey(fa => fa.Id);

                entity.HasIndex(fa => fa.FeatureId);

                entity.Property(fa => fa.Action).IsRequired().HasMaxLength(50);
                entity.Property(fa => fa.OldValues).HasColumnType("nvarchar(max)");
                entity.Property(fa => fa.NewValues).HasColumnType("nvarchar(max)");
                entity.Property(fa => fa.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

                entity.HasOne(fa => fa.Feature)
                      .WithMany(f => f.FeatureAudits)
                      .HasForeignKey(fa => fa.FeatureId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }

        // ════════════════════════════════════════════════════════════════════
        // PLAN AUDIT CONFIGURATION
        // ════════════════════════════════════════════════════════════════════
        private static void ConfigurePlanAudit(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PlanAudit>(entity =>
            {
                entity.HasKey(pa => pa.Id);

                entity.HasIndex(pa => pa.PlanId);

                entity.Property(pa => pa.Action).IsRequired().HasMaxLength(50);
                entity.Property(pa => pa.OldValues).HasColumnType("nvarchar(max)");
                entity.Property(pa => pa.NewValues).HasColumnType("nvarchar(max)");
                entity.Property(pa => pa.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

                entity.HasOne(pa => pa.Plan)
                      .WithMany(p => p.PlanAudits)
                      .HasForeignKey(pa => pa.PlanId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }

        // ════════════════════════════════════════════════════════════════════
        // PLAN PRICE HISTORY CONFIGURATION
        // ════════════════════════════════════════════════════════════════════
        private static void ConfigurePlanPriceHistory(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PlanPriceHistory>(entity =>
            {
                entity.HasKey(ph => ph.Id);

                entity.HasIndex(ph => ph.PlanId);

                entity.Property(ph => ph.MonthlyPrice).HasColumnType("decimal(18,2)");
                entity.Property(ph => ph.AnnualPrice).HasColumnType("decimal(18,2)");
                entity.Property(ph => ph.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

                entity.HasOne(ph => ph.Plan)
                      .WithMany(p => p.PlanPriceHistories)
                      .HasForeignKey(ph => ph.PlanId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Seed initial price history for all plans
                entity.HasData(
                    new PlanPriceHistory
                    {
                        Id = new Guid("d0000000-0000-0000-0000-000000000001"),
                        PlanId = new Guid("a0000000-0000-0000-0000-000000000001"),
                        MonthlyPrice = 0m,
                        AnnualPrice = 0m,
                        EffectiveFrom = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                        EffectiveTo = null,
                        ChangedBy = Guid.Empty, // System-generated seed
                        CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                    },
                    new PlanPriceHistory
                    {
                        Id = new Guid("d0000000-0000-0000-0000-000000000002"),
                        PlanId = new Guid("a0000000-0000-0000-0000-000000000002"),
                        MonthlyPrice = 499m,
                        AnnualPrice = 4999m,
                        EffectiveFrom = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                        EffectiveTo = null,
                        ChangedBy = Guid.Empty,
                        CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                    },
                    new PlanPriceHistory
                    {
                        Id = new Guid("d0000000-0000-0000-0000-000000000003"),
                        PlanId = new Guid("a0000000-0000-0000-0000-000000000003"),
                        MonthlyPrice = 999m,
                        AnnualPrice = 9999m,
                        EffectiveFrom = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                        EffectiveTo = null,
                        ChangedBy = Guid.Empty,
                        CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                    },
                    new PlanPriceHistory
                    {
                        Id = new Guid("d0000000-0000-0000-0000-000000000004"),
                        PlanId = new Guid("a0000000-0000-0000-0000-000000000004"),
                        MonthlyPrice = 1499m,
                        AnnualPrice = 14999m,
                        EffectiveFrom = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                        EffectiveTo = null,
                        ChangedBy = Guid.Empty,
                        CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                    }
                );
            });
        }
    }

}
