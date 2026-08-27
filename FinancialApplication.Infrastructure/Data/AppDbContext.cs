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
        public DbSet<PortfolioAsset> PortfolioAssets { get; set; }
        public DbSet<TaxEntry> TaxEntries { get; set; }

        // ── Banner System DbSet ─────────────────────────────────────────────
        public DbSet<Banner> Banners { get; set; }

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
            // BANNER CONFIGURATION
            // ════════════════════════════════════════════════════════════════════

            ConfigureBanner(modelBuilder);

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

            // ════════════════════════════════════════════════════════════════════
            // PORTFOLIO & TAX CONFIGURATIONS
            // ════════════════════════════════════════════════════════════════════

            ConfigurePortfolioAsset(modelBuilder);
            ConfigureTaxEntry(modelBuilder);
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

                // Seed data — 27 features with fixed GUIDs
                entity.HasData(
                    // ── Core Features (Free+) ──
                    new Feature { Id = new Guid("b0000000-0000-0000-0000-000000000001"), FeatureKey = "dashboard", DisplayName = "Dashboard", Description = "Main financial dashboard with overview and widgets.", Category = "Core", IsActive = true, SortOrder = 1, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new Feature { Id = new Guid("b0000000-0000-0000-0000-000000000002"), FeatureKey = "transactions", DisplayName = "Transactions", Description = "Track income and expense transactions.", Category = "Core", IsActive = true, SortOrder = 2, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new Feature { Id = new Guid("b0000000-0000-0000-0000-000000000003"), FeatureKey = "news", DisplayName = "Financial News", Description = "Access curated financial news articles.", Category = "Core", IsActive = true, SortOrder = 3, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new Feature { Id = new Guid("b0000000-0000-0000-0000-000000000004"), FeatureKey = "profile", DisplayName = "Profile Management", Description = "Manage user profile and account settings.", Category = "Core", IsActive = true, SortOrder = 4, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new Feature { Id = new Guid("b0000000-0000-0000-0000-000000000005"), FeatureKey = "security_settings", DisplayName = "Security Settings", Description = "Configure 2FA, recovery codes, and login security.", Category = "Core", IsActive = true, SortOrder = 5, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new Feature { Id = new Guid("b0000000-0000-0000-0000-000000000006"), FeatureKey = "onboarding", DisplayName = "Onboarding", Description = "Guided setup and onboarding experience.", Category = "Core", IsActive = true, SortOrder = 6, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new Feature { Id = new Guid("b0000000-0000-0000-0000-000000000016"), FeatureKey = "goals_basic", DisplayName = "Basic Goals", Description = "Set and track basic financial savings goals.", Category = "Goals", IsActive = true, SortOrder = 7, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new Feature { Id = new Guid("b0000000-0000-0000-0000-000000000017"), FeatureKey = "notifications", DisplayName = "Notifications", Description = "Receive alerts for transactions, goals, and account activity.", Category = "Core", IsActive = true, SortOrder = 8, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },

                    // ── Basic+ Features ──
                    new Feature { Id = new Guid("b0000000-0000-0000-0000-000000000007"), FeatureKey = "analytics", DisplayName = "Analytics", Description = "Basic financial analytics and charts.", Category = "Analytics", IsActive = true, SortOrder = 10, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new Feature { Id = new Guid("b0000000-0000-0000-0000-000000000008"), FeatureKey = "investment_tracking", DisplayName = "Investment Tracking", Description = "Monitor and manage investment portfolio.", Category = "Investments", IsActive = true, SortOrder = 11, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new Feature { Id = new Guid("b0000000-0000-0000-0000-000000000009"), FeatureKey = "cards", DisplayName = "Cards Management", Description = "Manage payment cards and linked accounts.", Category = "Finance", IsActive = true, SortOrder = 12, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new Feature { Id = new Guid("b0000000-0000-0000-0000-000000000018"), FeatureKey = "goals_unlimited", DisplayName = "Unlimited Goals", Description = "Create unlimited financial goals with no restrictions.", Category = "Goals", IsActive = true, SortOrder = 13, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new Feature { Id = new Guid("b0000000-0000-0000-0000-000000000019"), FeatureKey = "recurring_transactions", DisplayName = "Recurring Transactions", Description = "Set up automatic recurring income and expense entries.", Category = "Transactions", IsActive = true, SortOrder = 14, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new Feature { Id = new Guid("b0000000-0000-0000-0000-000000000020"), FeatureKey = "transaction_categories", DisplayName = "Custom Categories", Description = "Create custom transaction categories for better organization.", Category = "Transactions", IsActive = true, SortOrder = 15, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new Feature { Id = new Guid("b0000000-0000-0000-0000-000000000012"), FeatureKey = "export_csv", DisplayName = "Export CSV", Description = "Export data as CSV files for spreadsheet use.", Category = "Reports", IsActive = true, SortOrder = 16, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },

                    // ── Advanced+ Features ──
                    new Feature { Id = new Guid("b0000000-0000-0000-0000-000000000010"), FeatureKey = "reports", DisplayName = "Reports", Description = "Generate financial reports and summaries.", Category = "Reports", IsActive = true, SortOrder = 20, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new Feature { Id = new Guid("b0000000-0000-0000-0000-000000000011"), FeatureKey = "export_pdf", DisplayName = "Export PDF", Description = "Export reports and data as PDF documents.", Category = "Reports", IsActive = true, SortOrder = 21, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new Feature { Id = new Guid("b0000000-0000-0000-0000-000000000013"), FeatureKey = "premium_analytics", DisplayName = "Premium Analytics", Description = "Advanced analytics with trend analysis and predictions.", Category = "Analytics", IsActive = true, SortOrder = 22, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new Feature { Id = new Guid("b0000000-0000-0000-0000-000000000021"), FeatureKey = "investment_analytics", DisplayName = "Investment Analytics", Description = "Advanced investment analytics with sector-wise breakdowns.", Category = "Investments", IsActive = true, SortOrder = 23, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new Feature { Id = new Guid("b0000000-0000-0000-0000-000000000022"), FeatureKey = "investment_returns_tracking", DisplayName = "Returns & ROI Tracking", Description = "Track investment returns and calculate ROI over time.", Category = "Investments", IsActive = true, SortOrder = 24, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new Feature { Id = new Guid("b0000000-0000-0000-0000-000000000023"), FeatureKey = "budget_planning", DisplayName = "Budget Planning", Description = "Create and manage monthly budgets with category-wise limits.", Category = "Budgeting", IsActive = true, SortOrder = 25, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new Feature { Id = new Guid("b0000000-0000-0000-0000-000000000024"), FeatureKey = "transaction_insights", DisplayName = "AI Spending Insights", Description = "AI-powered analysis of spending patterns and saving suggestions.", Category = "Analytics", IsActive = true, SortOrder = 26, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new Feature { Id = new Guid("b0000000-0000-0000-0000-000000000025"), FeatureKey = "goal_recommendations", DisplayName = "Goal Recommendations", Description = "Smart goal suggestions based on your financial habits.", Category = "Goals", IsActive = true, SortOrder = 27, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new Feature { Id = new Guid("b0000000-0000-0000-0000-000000000026"), FeatureKey = "multi_currency", DisplayName = "Multi-Currency Support", Description = "Track finances in multiple currencies with auto-conversion.", Category = "Finance", IsActive = true, SortOrder = 28, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },

                    // ── Pro-Only Features ──
                    new Feature { Id = new Guid("b0000000-0000-0000-0000-000000000014"), FeatureKey = "ai_suggestions", DisplayName = "AI Suggestions", Description = "AI-powered financial insights and recommendations.", Category = "AI", IsActive = true, SortOrder = 30, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new Feature { Id = new Guid("b0000000-0000-0000-0000-000000000015"), FeatureKey = "user_management", DisplayName = "User Management", Description = "Admin: manage users, roles, and permissions.", Category = "Admin", IsActive = true, SortOrder = 31, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new Feature { Id = new Guid("b0000000-0000-0000-0000-000000000027"), FeatureKey = "portfolio_management", DisplayName = "Portfolio Management", Description = "Advanced portfolio management with allocation and rebalancing.", Category = "Investments", IsActive = true, SortOrder = 32, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new Feature { Id = new Guid("b0000000-0000-0000-0000-000000000028"), FeatureKey = "tax_reports", DisplayName = "Tax Reports & Summaries", Description = "Generate tax-ready reports with capital gains and deductions.", Category = "Reports", IsActive = true, SortOrder = 33, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new Feature { Id = new Guid("b0000000-0000-0000-0000-000000000029"), FeatureKey = "api_access", DisplayName = "API Access", Description = "Programmatic access to your financial data via REST API.", Category = "Developer", IsActive = true, SortOrder = 34, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new Feature { Id = new Guid("b0000000-0000-0000-0000-000000000030"), FeatureKey = "priority_support", DisplayName = "Priority Support", Description = "Get priority customer support with faster response times.", Category = "Support", IsActive = true, SortOrder = 35, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new Feature { Id = new Guid("b0000000-0000-0000-0000-000000000031"), FeatureKey = "audit_log", DisplayName = "Full Audit Log", Description = "View complete security and activity audit trail.", Category = "Security", IsActive = true, SortOrder = 36, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
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
                // Feature GUIDs: b0000000-...-000000000001 through ...000000000031

                // FREE plan (8 features): dashboard, transactions, news, profile, security_settings, onboarding, goals_basic, notifications
                entity.HasData(
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0001-000000000001"), PlanId = new Guid("a0000000-0000-0000-0000-000000000001"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000001"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0001-000000000002"), PlanId = new Guid("a0000000-0000-0000-0000-000000000001"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000002"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0001-000000000003"), PlanId = new Guid("a0000000-0000-0000-0000-000000000001"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000003"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0001-000000000004"), PlanId = new Guid("a0000000-0000-0000-0000-000000000001"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000004"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0001-000000000005"), PlanId = new Guid("a0000000-0000-0000-0000-000000000001"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000005"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0001-000000000006"), PlanId = new Guid("a0000000-0000-0000-0000-000000000001"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000006"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0001-000000000007"), PlanId = new Guid("a0000000-0000-0000-0000-000000000001"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000016"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0001-000000000008"), PlanId = new Guid("a0000000-0000-0000-0000-000000000001"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000017"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },

                    // BASIC plan (15 features): Free features + analytics, investment_tracking, cards, goals_unlimited, recurring_transactions, transaction_categories, export_csv
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0002-000000000001"), PlanId = new Guid("a0000000-0000-0000-0000-000000000002"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000001"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0002-000000000002"), PlanId = new Guid("a0000000-0000-0000-0000-000000000002"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000002"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0002-000000000003"), PlanId = new Guid("a0000000-0000-0000-0000-000000000002"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000003"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0002-000000000004"), PlanId = new Guid("a0000000-0000-0000-0000-000000000002"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000004"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0002-000000000005"), PlanId = new Guid("a0000000-0000-0000-0000-000000000002"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000005"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0002-000000000006"), PlanId = new Guid("a0000000-0000-0000-0000-000000000002"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000006"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0002-000000000007"), PlanId = new Guid("a0000000-0000-0000-0000-000000000002"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000007"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0002-000000000008"), PlanId = new Guid("a0000000-0000-0000-0000-000000000002"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000008"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0002-000000000009"), PlanId = new Guid("a0000000-0000-0000-0000-000000000002"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000009"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0002-000000000010"), PlanId = new Guid("a0000000-0000-0000-0000-000000000002"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000016"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0002-000000000011"), PlanId = new Guid("a0000000-0000-0000-0000-000000000002"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000017"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0002-000000000012"), PlanId = new Guid("a0000000-0000-0000-0000-000000000002"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000018"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0002-000000000013"), PlanId = new Guid("a0000000-0000-0000-0000-000000000002"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000019"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0002-000000000014"), PlanId = new Guid("a0000000-0000-0000-0000-000000000002"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000020"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0002-000000000015"), PlanId = new Guid("a0000000-0000-0000-0000-000000000002"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000012"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },

                    // ADVANCED plan (22 features): Basic features + reports, export_pdf, premium_analytics, investment_analytics, investment_returns_tracking, budget_planning, transaction_insights, goal_recommendations, multi_currency
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
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0003-000000000014"), PlanId = new Guid("a0000000-0000-0000-0000-000000000003"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000016"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0003-000000000015"), PlanId = new Guid("a0000000-0000-0000-0000-000000000003"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000017"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0003-000000000016"), PlanId = new Guid("a0000000-0000-0000-0000-000000000003"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000018"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0003-000000000017"), PlanId = new Guid("a0000000-0000-0000-0000-000000000003"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000019"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0003-000000000018"), PlanId = new Guid("a0000000-0000-0000-0000-000000000003"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000020"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0003-000000000019"), PlanId = new Guid("a0000000-0000-0000-0000-000000000003"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000021"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0003-000000000020"), PlanId = new Guid("a0000000-0000-0000-0000-000000000003"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000022"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0003-000000000021"), PlanId = new Guid("a0000000-0000-0000-0000-000000000003"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000023"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0003-000000000022"), PlanId = new Guid("a0000000-0000-0000-0000-000000000003"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000024"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0003-000000000023"), PlanId = new Guid("a0000000-0000-0000-0000-000000000003"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000025"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0003-000000000024"), PlanId = new Guid("a0000000-0000-0000-0000-000000000003"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000026"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },

                    // PRO plan: ALL 27 features (Advanced + ai_suggestions, user_management, portfolio_management, tax_reports, api_access, priority_support, audit_log)
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
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0004-000000000015"), PlanId = new Guid("a0000000-0000-0000-0000-000000000004"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000015"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0004-000000000016"), PlanId = new Guid("a0000000-0000-0000-0000-000000000004"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000016"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0004-000000000017"), PlanId = new Guid("a0000000-0000-0000-0000-000000000004"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000017"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0004-000000000018"), PlanId = new Guid("a0000000-0000-0000-0000-000000000004"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000018"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0004-000000000019"), PlanId = new Guid("a0000000-0000-0000-0000-000000000004"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000019"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0004-000000000020"), PlanId = new Guid("a0000000-0000-0000-0000-000000000004"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000020"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0004-000000000021"), PlanId = new Guid("a0000000-0000-0000-0000-000000000004"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000021"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0004-000000000022"), PlanId = new Guid("a0000000-0000-0000-0000-000000000004"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000022"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0004-000000000023"), PlanId = new Guid("a0000000-0000-0000-0000-000000000004"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000023"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0004-000000000024"), PlanId = new Guid("a0000000-0000-0000-0000-000000000004"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000024"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0004-000000000025"), PlanId = new Guid("a0000000-0000-0000-0000-000000000004"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000025"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0004-000000000026"), PlanId = new Guid("a0000000-0000-0000-0000-000000000004"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000026"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0004-000000000027"), PlanId = new Guid("a0000000-0000-0000-0000-000000000004"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000027"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0004-000000000028"), PlanId = new Guid("a0000000-0000-0000-0000-000000000004"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000028"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0004-000000000029"), PlanId = new Guid("a0000000-0000-0000-0000-000000000004"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000029"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0004-000000000030"), PlanId = new Guid("a0000000-0000-0000-0000-000000000004"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000030"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                    new PlanFeature { Id = new Guid("c0000000-0000-0000-0004-000000000031"), PlanId = new Guid("a0000000-0000-0000-0000-000000000004"), FeatureId = new Guid("b0000000-0000-0000-0000-000000000031"), CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
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

        // ════════════════════════════════════════════════════════════════════
        // BANNER CONFIGURATION
        // ════════════════════════════════════════════════════════════════════
        private static void ConfigureBanner(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Banner>(entity =>
            {
                entity.HasKey(b => b.Id);

                // Index on OriginalUrl for dedup checks (avoid re-downloading same image)
                entity.HasIndex(b => b.OriginalUrl);

                // Store compressed image as varbinary(max)
                entity.Property(b => b.CompressedImage)
                      .IsRequired()
                      .HasColumnType("varbinary(max)");

                entity.Property(b => b.ContentType)
                      .IsRequired()
                      .HasMaxLength(50)
                      .HasDefaultValue("image/jpeg");

                entity.Property(b => b.OriginalUrl)
                      .IsRequired()
                      .HasMaxLength(2048);

                entity.Property(b => b.SourcePageUrl)
                      .HasMaxLength(2048);

                entity.Property(b => b.Title)
                      .HasMaxLength(500);

                entity.Property(b => b.Description)
                      .HasMaxLength(2000);

                entity.Property(b => b.CreatedAt)
                      .HasDefaultValueSql("SYSUTCDATETIME()");
            });
        }

        // ════════════════════════════════════════════════════════════════════
        // PORTFOLIO ASSET CONFIGURATION
        // ════════════════════════════════════════════════════════════════════
        private static void ConfigurePortfolioAsset(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PortfolioAsset>(entity =>
            {
                entity.HasKey(e => e.PortfolioAssetId);

                entity.ToTable("PortfolioAssets");

                // Performance index on UserId for user-scoped queries
                entity.HasIndex(e => e.UserId);

                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.AssetType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.InvestedAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.CurrentValue).HasColumnType("decimal(18,2)");
                entity.Property(e => e.AllocationPercentage).HasColumnType("decimal(5,2)");
                entity.Property(e => e.Color).HasMaxLength(10);
                entity.Property(e => e.Notes).HasMaxLength(500);
                entity.Property(e => e.Currency).IsRequired().HasMaxLength(10).HasDefaultValue("INR");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

                entity.HasOne(e => e.User)
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }

        // ════════════════════════════════════════════════════════════════════
        // TAX ENTRY CONFIGURATION
        // ════════════════════════════════════════════════════════════════════
        private static void ConfigureTaxEntry(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TaxEntry>(entity =>
            {
                entity.HasKey(e => e.TaxEntryId);

                entity.ToTable("TaxEntries");

                // Composite index on UserId + FinancialYear for FY-scoped queries
                entity.HasIndex(e => new { e.UserId, e.FinancialYear });

                entity.Property(e => e.FinancialYear).IsRequired().HasMaxLength(10);
                entity.Property(e => e.Category).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.EntryType).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Section).HasMaxLength(20);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

                entity.HasOne(e => e.User)
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }

}
