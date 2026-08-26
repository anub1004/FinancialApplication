using System;
using System.Collections.Generic;
using FinancialApplication.Domain.Domain.Enums;

namespace FinancialApplication.Application.DTOs.Dashboard
{
    // ── Main Dashboard Summary ───────────────────────────────────────────────
    public class DashboardSummaryDto
    {
        public decimal TotalIncome { get; set; }
        public decimal TotalExpense { get; set; }
        public decimal NetBalance { get; set; }
        public decimal SavingsRate { get; set; }
        public string Currency { get; set; } = "INR";
        public int Month { get; set; }
        public int Year { get; set; }

        // Quick counts
        public int TransactionCount { get; set; }
        public int ActiveInvestments { get; set; }
        public int ActiveGoals { get; set; }

        // Investment snapshot
        public decimal TotalInvested { get; set; }
        public decimal InvestmentCurrentValue { get; set; }
        public decimal InvestmentReturns { get; set; }

        // Goals snapshot
        public int GoalsCompleted { get; set; }
        public int GoalsInProgress { get; set; }
        public decimal GoalsTotalSaved { get; set; }
        public decimal GoalsTotalTarget { get; set; }
    }

    // ── Monthly Trend ────────────────────────────────────────────────────────
    public class MonthlyTrendDto
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public decimal Income { get; set; }
        public decimal Expense { get; set; }
        public decimal Net { get; set; }
    }

    // ── Category Breakdown ───────────────────────────────────────────────────
    public class CategoryBreakdownDto
    {
        public string Category { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal Percentage { get; set; }
        public int Count { get; set; }
    }

    // ── Recent Activity ──────────────────────────────────────────────────────
    public class RecentActivityDto
    {
        public Guid Id { get; set; }
        public string Type { get; set; } = string.Empty; // "Transaction", "Investment", "Goal"
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal? Amount { get; set; }
        public string? Currency { get; set; }
        public DateTime Date { get; set; }
        public string Icon { get; set; } = string.Empty;
        public bool IsPositive { get; set; }
    }
}
