using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using FinancialApplication.Application.DTOs.Dashboard;
using FinancialApplication.Application.Interfaces;
using FinancialApplication.Domain.Domain.Enums;
using FinancialApplication.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FinancialApplication.Infrastructure.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly AppDbContext _context;

        public DashboardService(AppDbContext context)
        {
            _context = context;
        }

        // ── Main Summary ────────────────────────────────────────────────────
        public async Task<DashboardSummaryDto> GetSummaryAsync(Guid userId, int? month = null, int? year = null)
        {
            var now = DateTime.UtcNow;
            var targetMonth = month ?? now.Month;
            var targetYear = year ?? now.Year;

            // Transactions for the month
            var transactions = await _context.Transactions
                .AsNoTracking()
                .Where(t => t.UserId == userId
                    && t.TransactionDate.Month == targetMonth
                    && t.TransactionDate.Year == targetYear)
                .ToListAsync();

            var totalIncome = transactions
                .Where(t => t.TransactionType == TransactionTypeEnum.Income)
                .Sum(t => t.Amount);

            var totalExpense = transactions
                .Where(t => t.TransactionType == TransactionTypeEnum.Expense)
                .Sum(t => t.Amount);

            var netBalance = totalIncome - totalExpense;
            var savingsRate = totalIncome > 0 ? Math.Round((netBalance / totalIncome) * 100, 1) : 0;

            // Investment snapshot
            var investments = await _context.Investments
                .AsNoTracking()
                .Where(i => i.UserId == userId)
                .ToListAsync();

            var activeInvestments = investments.Where(i => i.Status == "Active").ToList();

            // Goals snapshot
            var goals = await _context.Goals
                .AsNoTracking()
                .Where(g => g.UserId == userId)
                .ToListAsync();

            return new DashboardSummaryDto
            {
                TotalIncome = totalIncome,
                TotalExpense = totalExpense,
                NetBalance = netBalance,
                SavingsRate = savingsRate,
                Currency = "INR",
                Month = targetMonth,
                Year = targetYear,
                TransactionCount = transactions.Count,
                ActiveInvestments = activeInvestments.Count,
                ActiveGoals = goals.Count(g => g.Status == GoalStatusEnum.InProgress || g.Status == GoalStatusEnum.NotStarted),
                TotalInvested = investments.Sum(i => i.Amount),
                InvestmentCurrentValue = investments.Sum(i => i.CurrentValue),
                InvestmentReturns = investments.Sum(i => i.CurrentValue) - investments.Sum(i => i.Amount),
                GoalsCompleted = goals.Count(g => g.Status == GoalStatusEnum.Completed),
                GoalsInProgress = goals.Count(g => g.Status == GoalStatusEnum.InProgress),
                GoalsTotalSaved = goals.Sum(g => g.CurrentAmount),
                GoalsTotalTarget = goals.Sum(g => g.TargetAmount)
            };
        }

        // ── Monthly Trend ───────────────────────────────────────────────────
        public async Task<List<MonthlyTrendDto>> GetMonthlyTrendAsync(Guid userId, int months = 6)
        {
            var now = DateTime.UtcNow;
            var startDate = new DateTime(now.Year, now.Month, 1).AddMonths(-(months - 1));

            var transactions = await _context.Transactions
                .AsNoTracking()
                .Where(t => t.UserId == userId && t.TransactionDate >= startDate)
                .ToListAsync();

            var result = new List<MonthlyTrendDto>();

            for (int i = 0; i < months; i++)
            {
                var date = startDate.AddMonths(i);
                var monthTransactions = transactions
                    .Where(t => t.TransactionDate.Month == date.Month && t.TransactionDate.Year == date.Year)
                    .ToList();

                var income = monthTransactions
                    .Where(t => t.TransactionType == TransactionTypeEnum.Income)
                    .Sum(t => t.Amount);

                var expense = monthTransactions
                    .Where(t => t.TransactionType == TransactionTypeEnum.Expense)
                    .Sum(t => t.Amount);

                result.Add(new MonthlyTrendDto
                {
                    Month = date.Month,
                    Year = date.Year,
                    MonthName = date.ToString("MMM yyyy", CultureInfo.InvariantCulture),
                    Income = income,
                    Expense = expense,
                    Net = income - expense
                });
            }

            return result;
        }

        // ── Category Breakdown ──────────────────────────────────────────────
        public async Task<List<CategoryBreakdownDto>> GetCategoryBreakdownAsync(Guid userId, int? month = null, int? year = null)
        {
            var now = DateTime.UtcNow;
            var targetMonth = month ?? now.Month;
            var targetYear = year ?? now.Year;

            var expenses = await _context.Transactions
                .AsNoTracking()
                .Where(t => t.UserId == userId
                    && t.TransactionType == TransactionTypeEnum.Expense
                    && t.TransactionDate.Month == targetMonth
                    && t.TransactionDate.Year == targetYear)
                .ToListAsync();

            var totalExpense = expenses.Sum(t => t.Amount);

            return expenses
                .GroupBy(t => t.Category)
                .Select(g => new CategoryBreakdownDto
                {
                    Category = g.Key,
                    Amount = g.Sum(t => t.Amount),
                    Percentage = totalExpense > 0
                        ? Math.Round((g.Sum(t => t.Amount) / totalExpense) * 100, 1)
                        : 0,
                    Count = g.Count()
                })
                .OrderByDescending(c => c.Amount)
                .ToList();
        }

        // ── Recent Activity ─────────────────────────────────────────────────
        public async Task<List<RecentActivityDto>> GetRecentActivityAsync(Guid userId, int count = 10)
        {
            var activities = new List<RecentActivityDto>();

            // Recent transactions
            var recentTransactions = await _context.Transactions
                .AsNoTracking()
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.TransactionDate)
                .Take(count)
                .ToListAsync();

            foreach (var t in recentTransactions)
            {
                activities.Add(new RecentActivityDto
                {
                    Id = t.TransactionId,
                    Type = "Transaction",
                    Title = t.TransactionType == TransactionTypeEnum.Income
                        ? $"Income: {t.Category}"
                        : $"Expense: {t.Category}",
                    Description = string.IsNullOrEmpty(t.Description)
                        ? t.Category
                        : t.Description,
                    Amount = t.TransactionType == TransactionTypeEnum.Income ? t.Amount : -t.Amount,
                    Currency = t.Currency,
                    Date = t.TransactionDate,
                    Icon = t.TransactionType == TransactionTypeEnum.Income ? "💰" : "💸",
                    IsPositive = t.TransactionType == TransactionTypeEnum.Income
                });
            }

            // Recent investments
            var recentInvestments = await _context.Investments
                .AsNoTracking()
                .Where(i => i.UserId == userId)
                .OrderByDescending(i => i.UpdatedAt)
                .Take(5)
                .ToListAsync();

            foreach (var i in recentInvestments)
            {
                activities.Add(new RecentActivityDto
                {
                    Id = i.InvestmentId,
                    Type = "Investment",
                    Title = i.Name,
                    Description = $"{i.InvestmentType} — {i.Status}",
                    Amount = i.Amount,
                    Currency = i.Currency,
                    Date = i.UpdatedAt,
                    Icon = "📈",
                    IsPositive = true
                });
            }

            // Recent goals
            var recentGoals = await _context.Goals
                .AsNoTracking()
                .Where(g => g.UserId == userId)
                .OrderByDescending(g => g.UpdatedAt)
                .Take(5)
                .ToListAsync();

            foreach (var g in recentGoals)
            {
                var progress = g.TargetAmount > 0
                    ? Math.Round((g.CurrentAmount / g.TargetAmount) * 100, 0)
                    : 0;

                activities.Add(new RecentActivityDto
                {
                    Id = g.GoalId,
                    Type = "Goal",
                    Title = g.Title,
                    Description = $"{progress}% complete — {g.Status}",
                    Amount = g.CurrentAmount,
                    Currency = g.Currency,
                    Date = g.UpdatedAt,
                    Icon = g.Icon ?? "🎯",
                    IsPositive = true
                });
            }

            // Sort combined and take top N
            return activities
                .OrderByDescending(a => a.Date)
                .Take(count)
                .ToList();
        }
    }
}
