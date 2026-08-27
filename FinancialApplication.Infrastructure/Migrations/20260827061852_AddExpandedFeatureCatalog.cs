using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FinancialApplication.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExpandedFeatureCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Features",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000007"),
                column: "SortOrder",
                value: 10);

            migrationBuilder.UpdateData(
                table: "Features",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000008"),
                column: "SortOrder",
                value: 11);

            migrationBuilder.UpdateData(
                table: "Features",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000009"),
                column: "SortOrder",
                value: 12);

            migrationBuilder.UpdateData(
                table: "Features",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000010"),
                column: "SortOrder",
                value: 20);

            migrationBuilder.UpdateData(
                table: "Features",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000011"),
                column: "SortOrder",
                value: 21);

            migrationBuilder.UpdateData(
                table: "Features",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000012"),
                column: "SortOrder",
                value: 16);

            migrationBuilder.UpdateData(
                table: "Features",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000013"),
                column: "SortOrder",
                value: 22);

            migrationBuilder.UpdateData(
                table: "Features",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000014"),
                column: "SortOrder",
                value: 30);

            migrationBuilder.UpdateData(
                table: "Features",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000015"),
                column: "SortOrder",
                value: 31);

            migrationBuilder.InsertData(
                table: "Features",
                columns: new[] { "Id", "Category", "CreatedAt", "Description", "DisplayName", "FeatureKey", "IsActive", "SortOrder", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("b0000000-0000-0000-0000-000000000016"), "Goals", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Set and track basic financial savings goals.", "Basic Goals", "goals_basic", true, 7, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("b0000000-0000-0000-0000-000000000017"), "Core", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Receive alerts for transactions, goals, and account activity.", "Notifications", "notifications", true, 8, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("b0000000-0000-0000-0000-000000000018"), "Goals", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Create unlimited financial goals with no restrictions.", "Unlimited Goals", "goals_unlimited", true, 13, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("b0000000-0000-0000-0000-000000000019"), "Transactions", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Set up automatic recurring income and expense entries.", "Recurring Transactions", "recurring_transactions", true, 14, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("b0000000-0000-0000-0000-000000000020"), "Transactions", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Create custom transaction categories for better organization.", "Custom Categories", "transaction_categories", true, 15, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("b0000000-0000-0000-0000-000000000021"), "Investments", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Advanced investment analytics with sector-wise breakdowns.", "Investment Analytics", "investment_analytics", true, 23, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("b0000000-0000-0000-0000-000000000022"), "Investments", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Track investment returns and calculate ROI over time.", "Returns & ROI Tracking", "investment_returns_tracking", true, 24, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("b0000000-0000-0000-0000-000000000023"), "Budgeting", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Create and manage monthly budgets with category-wise limits.", "Budget Planning", "budget_planning", true, 25, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("b0000000-0000-0000-0000-000000000024"), "Analytics", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "AI-powered analysis of spending patterns and saving suggestions.", "AI Spending Insights", "transaction_insights", true, 26, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("b0000000-0000-0000-0000-000000000025"), "Goals", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Smart goal suggestions based on your financial habits.", "Goal Recommendations", "goal_recommendations", true, 27, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("b0000000-0000-0000-0000-000000000026"), "Finance", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Track finances in multiple currencies with auto-conversion.", "Multi-Currency Support", "multi_currency", true, 28, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("b0000000-0000-0000-0000-000000000027"), "Investments", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Advanced portfolio management with allocation and rebalancing.", "Portfolio Management", "portfolio_management", true, 32, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("b0000000-0000-0000-0000-000000000028"), "Reports", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Generate tax-ready reports with capital gains and deductions.", "Tax Reports & Summaries", "tax_reports", true, 33, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("b0000000-0000-0000-0000-000000000029"), "Developer", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Programmatic access to your financial data via REST API.", "API Access", "api_access", true, 34, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("b0000000-0000-0000-0000-000000000030"), "Support", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Get priority customer support with faster response times.", "Priority Support", "priority_support", true, 35, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("b0000000-0000-0000-0000-000000000031"), "Security", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "View complete security and activity audit trail.", "Full Audit Log", "audit_log", true, 36, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "PlanFeatures",
                columns: new[] { "Id", "CreatedAt", "FeatureId", "PlanId" },
                values: new object[,]
                {
                    { new Guid("c0000000-0000-0000-0002-000000000015"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("b0000000-0000-0000-0000-000000000012"), new Guid("a0000000-0000-0000-0000-000000000002") },
                    { new Guid("c0000000-0000-0000-0001-000000000007"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("b0000000-0000-0000-0000-000000000016"), new Guid("a0000000-0000-0000-0000-000000000001") },
                    { new Guid("c0000000-0000-0000-0001-000000000008"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("b0000000-0000-0000-0000-000000000017"), new Guid("a0000000-0000-0000-0000-000000000001") },
                    { new Guid("c0000000-0000-0000-0002-000000000010"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("b0000000-0000-0000-0000-000000000016"), new Guid("a0000000-0000-0000-0000-000000000002") },
                    { new Guid("c0000000-0000-0000-0002-000000000011"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("b0000000-0000-0000-0000-000000000017"), new Guid("a0000000-0000-0000-0000-000000000002") },
                    { new Guid("c0000000-0000-0000-0002-000000000012"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("b0000000-0000-0000-0000-000000000018"), new Guid("a0000000-0000-0000-0000-000000000002") },
                    { new Guid("c0000000-0000-0000-0002-000000000013"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("b0000000-0000-0000-0000-000000000019"), new Guid("a0000000-0000-0000-0000-000000000002") },
                    { new Guid("c0000000-0000-0000-0002-000000000014"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("b0000000-0000-0000-0000-000000000020"), new Guid("a0000000-0000-0000-0000-000000000002") },
                    { new Guid("c0000000-0000-0000-0003-000000000014"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("b0000000-0000-0000-0000-000000000016"), new Guid("a0000000-0000-0000-0000-000000000003") },
                    { new Guid("c0000000-0000-0000-0003-000000000015"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("b0000000-0000-0000-0000-000000000017"), new Guid("a0000000-0000-0000-0000-000000000003") },
                    { new Guid("c0000000-0000-0000-0003-000000000016"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("b0000000-0000-0000-0000-000000000018"), new Guid("a0000000-0000-0000-0000-000000000003") },
                    { new Guid("c0000000-0000-0000-0003-000000000017"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("b0000000-0000-0000-0000-000000000019"), new Guid("a0000000-0000-0000-0000-000000000003") },
                    { new Guid("c0000000-0000-0000-0003-000000000018"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("b0000000-0000-0000-0000-000000000020"), new Guid("a0000000-0000-0000-0000-000000000003") },
                    { new Guid("c0000000-0000-0000-0003-000000000019"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("b0000000-0000-0000-0000-000000000021"), new Guid("a0000000-0000-0000-0000-000000000003") },
                    { new Guid("c0000000-0000-0000-0003-000000000020"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("b0000000-0000-0000-0000-000000000022"), new Guid("a0000000-0000-0000-0000-000000000003") },
                    { new Guid("c0000000-0000-0000-0003-000000000021"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("b0000000-0000-0000-0000-000000000023"), new Guid("a0000000-0000-0000-0000-000000000003") },
                    { new Guid("c0000000-0000-0000-0003-000000000022"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("b0000000-0000-0000-0000-000000000024"), new Guid("a0000000-0000-0000-0000-000000000003") },
                    { new Guid("c0000000-0000-0000-0003-000000000023"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("b0000000-0000-0000-0000-000000000025"), new Guid("a0000000-0000-0000-0000-000000000003") },
                    { new Guid("c0000000-0000-0000-0003-000000000024"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("b0000000-0000-0000-0000-000000000026"), new Guid("a0000000-0000-0000-0000-000000000003") },
                    { new Guid("c0000000-0000-0000-0004-000000000016"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("b0000000-0000-0000-0000-000000000016"), new Guid("a0000000-0000-0000-0000-000000000004") },
                    { new Guid("c0000000-0000-0000-0004-000000000017"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("b0000000-0000-0000-0000-000000000017"), new Guid("a0000000-0000-0000-0000-000000000004") },
                    { new Guid("c0000000-0000-0000-0004-000000000018"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("b0000000-0000-0000-0000-000000000018"), new Guid("a0000000-0000-0000-0000-000000000004") },
                    { new Guid("c0000000-0000-0000-0004-000000000019"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("b0000000-0000-0000-0000-000000000019"), new Guid("a0000000-0000-0000-0000-000000000004") },
                    { new Guid("c0000000-0000-0000-0004-000000000020"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("b0000000-0000-0000-0000-000000000020"), new Guid("a0000000-0000-0000-0000-000000000004") },
                    { new Guid("c0000000-0000-0000-0004-000000000021"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("b0000000-0000-0000-0000-000000000021"), new Guid("a0000000-0000-0000-0000-000000000004") },
                    { new Guid("c0000000-0000-0000-0004-000000000022"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("b0000000-0000-0000-0000-000000000022"), new Guid("a0000000-0000-0000-0000-000000000004") },
                    { new Guid("c0000000-0000-0000-0004-000000000023"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("b0000000-0000-0000-0000-000000000023"), new Guid("a0000000-0000-0000-0000-000000000004") },
                    { new Guid("c0000000-0000-0000-0004-000000000024"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("b0000000-0000-0000-0000-000000000024"), new Guid("a0000000-0000-0000-0000-000000000004") },
                    { new Guid("c0000000-0000-0000-0004-000000000025"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("b0000000-0000-0000-0000-000000000025"), new Guid("a0000000-0000-0000-0000-000000000004") },
                    { new Guid("c0000000-0000-0000-0004-000000000026"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("b0000000-0000-0000-0000-000000000026"), new Guid("a0000000-0000-0000-0000-000000000004") },
                    { new Guid("c0000000-0000-0000-0004-000000000027"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("b0000000-0000-0000-0000-000000000027"), new Guid("a0000000-0000-0000-0000-000000000004") },
                    { new Guid("c0000000-0000-0000-0004-000000000028"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("b0000000-0000-0000-0000-000000000028"), new Guid("a0000000-0000-0000-0000-000000000004") },
                    { new Guid("c0000000-0000-0000-0004-000000000029"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("b0000000-0000-0000-0000-000000000029"), new Guid("a0000000-0000-0000-0000-000000000004") },
                    { new Guid("c0000000-0000-0000-0004-000000000030"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("b0000000-0000-0000-0000-000000000030"), new Guid("a0000000-0000-0000-0000-000000000004") },
                    { new Guid("c0000000-0000-0000-0004-000000000031"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("b0000000-0000-0000-0000-000000000031"), new Guid("a0000000-0000-0000-0000-000000000004") }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "PlanFeatures",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0000-0000-0001-000000000007"));

            migrationBuilder.DeleteData(
                table: "PlanFeatures",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0000-0000-0001-000000000008"));

            migrationBuilder.DeleteData(
                table: "PlanFeatures",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0000-0000-0002-000000000010"));

            migrationBuilder.DeleteData(
                table: "PlanFeatures",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0000-0000-0002-000000000011"));

            migrationBuilder.DeleteData(
                table: "PlanFeatures",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0000-0000-0002-000000000012"));

            migrationBuilder.DeleteData(
                table: "PlanFeatures",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0000-0000-0002-000000000013"));

            migrationBuilder.DeleteData(
                table: "PlanFeatures",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0000-0000-0002-000000000014"));

            migrationBuilder.DeleteData(
                table: "PlanFeatures",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0000-0000-0002-000000000015"));

            migrationBuilder.DeleteData(
                table: "PlanFeatures",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0000-0000-0003-000000000014"));

            migrationBuilder.DeleteData(
                table: "PlanFeatures",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0000-0000-0003-000000000015"));

            migrationBuilder.DeleteData(
                table: "PlanFeatures",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0000-0000-0003-000000000016"));

            migrationBuilder.DeleteData(
                table: "PlanFeatures",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0000-0000-0003-000000000017"));

            migrationBuilder.DeleteData(
                table: "PlanFeatures",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0000-0000-0003-000000000018"));

            migrationBuilder.DeleteData(
                table: "PlanFeatures",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0000-0000-0003-000000000019"));

            migrationBuilder.DeleteData(
                table: "PlanFeatures",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0000-0000-0003-000000000020"));

            migrationBuilder.DeleteData(
                table: "PlanFeatures",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0000-0000-0003-000000000021"));

            migrationBuilder.DeleteData(
                table: "PlanFeatures",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0000-0000-0003-000000000022"));

            migrationBuilder.DeleteData(
                table: "PlanFeatures",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0000-0000-0003-000000000023"));

            migrationBuilder.DeleteData(
                table: "PlanFeatures",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0000-0000-0003-000000000024"));

            migrationBuilder.DeleteData(
                table: "PlanFeatures",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0000-0000-0004-000000000016"));

            migrationBuilder.DeleteData(
                table: "PlanFeatures",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0000-0000-0004-000000000017"));

            migrationBuilder.DeleteData(
                table: "PlanFeatures",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0000-0000-0004-000000000018"));

            migrationBuilder.DeleteData(
                table: "PlanFeatures",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0000-0000-0004-000000000019"));

            migrationBuilder.DeleteData(
                table: "PlanFeatures",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0000-0000-0004-000000000020"));

            migrationBuilder.DeleteData(
                table: "PlanFeatures",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0000-0000-0004-000000000021"));

            migrationBuilder.DeleteData(
                table: "PlanFeatures",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0000-0000-0004-000000000022"));

            migrationBuilder.DeleteData(
                table: "PlanFeatures",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0000-0000-0004-000000000023"));

            migrationBuilder.DeleteData(
                table: "PlanFeatures",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0000-0000-0004-000000000024"));

            migrationBuilder.DeleteData(
                table: "PlanFeatures",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0000-0000-0004-000000000025"));

            migrationBuilder.DeleteData(
                table: "PlanFeatures",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0000-0000-0004-000000000026"));

            migrationBuilder.DeleteData(
                table: "PlanFeatures",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0000-0000-0004-000000000027"));

            migrationBuilder.DeleteData(
                table: "PlanFeatures",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0000-0000-0004-000000000028"));

            migrationBuilder.DeleteData(
                table: "PlanFeatures",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0000-0000-0004-000000000029"));

            migrationBuilder.DeleteData(
                table: "PlanFeatures",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0000-0000-0004-000000000030"));

            migrationBuilder.DeleteData(
                table: "PlanFeatures",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0000-0000-0004-000000000031"));

            migrationBuilder.DeleteData(
                table: "Features",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000016"));

            migrationBuilder.DeleteData(
                table: "Features",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000017"));

            migrationBuilder.DeleteData(
                table: "Features",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000018"));

            migrationBuilder.DeleteData(
                table: "Features",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000019"));

            migrationBuilder.DeleteData(
                table: "Features",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000020"));

            migrationBuilder.DeleteData(
                table: "Features",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000021"));

            migrationBuilder.DeleteData(
                table: "Features",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000022"));

            migrationBuilder.DeleteData(
                table: "Features",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000023"));

            migrationBuilder.DeleteData(
                table: "Features",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000024"));

            migrationBuilder.DeleteData(
                table: "Features",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000025"));

            migrationBuilder.DeleteData(
                table: "Features",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000026"));

            migrationBuilder.DeleteData(
                table: "Features",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000027"));

            migrationBuilder.DeleteData(
                table: "Features",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000028"));

            migrationBuilder.DeleteData(
                table: "Features",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000029"));

            migrationBuilder.DeleteData(
                table: "Features",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000030"));

            migrationBuilder.DeleteData(
                table: "Features",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000031"));

            migrationBuilder.UpdateData(
                table: "Features",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000007"),
                column: "SortOrder",
                value: 7);

            migrationBuilder.UpdateData(
                table: "Features",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000008"),
                column: "SortOrder",
                value: 8);

            migrationBuilder.UpdateData(
                table: "Features",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000009"),
                column: "SortOrder",
                value: 9);

            migrationBuilder.UpdateData(
                table: "Features",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000010"),
                column: "SortOrder",
                value: 10);

            migrationBuilder.UpdateData(
                table: "Features",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000011"),
                column: "SortOrder",
                value: 11);

            migrationBuilder.UpdateData(
                table: "Features",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000012"),
                column: "SortOrder",
                value: 12);

            migrationBuilder.UpdateData(
                table: "Features",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000013"),
                column: "SortOrder",
                value: 13);

            migrationBuilder.UpdateData(
                table: "Features",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000014"),
                column: "SortOrder",
                value: 14);

            migrationBuilder.UpdateData(
                table: "Features",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000015"),
                column: "SortOrder",
                value: 15);
        }
    }
}
