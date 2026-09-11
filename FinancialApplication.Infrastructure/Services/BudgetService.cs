using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FinancialApplication.Application.DTOs.Budget;
using FinancialApplication.Application.Interfaces;
using FinancialApplication.Domain.Domain.Entity;
using FinancialApplication.Domain.Domain.Enums;
using FinancialApplication.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FinancialApplication.Infrastructure.Services
{
    /// <summary>
    /// Budget service implementation.
    /// Cross-references the Transactions table to compute actual spending per category.
    /// </summary>
    public class BudgetService : IBudgetService
    {
        private readonly AppDbContext _context;

        public BudgetService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<BudgetDto> CreateAsync(Guid userId, CreateBudgetDto dto)
        {
            // Check for duplicate budget (same user, category, month, year)
            var exists = await _context.Budgets.AnyAsync(b =>
                b.UserId == userId &&
                b.Category == dto.Category &&
                b.Month == dto.Month &&
                b.Year == dto.Year);

            if (exists)
                throw new InvalidOperationException(
                    $"A budget for '{dto.Category}' already exists for {dto.Month}/{dto.Year}.");

            var budget = new Budget
            {
                UserId = userId,
                Category = dto.Category,
                MonthlyLimit = dto.MonthlyLimit,
                AlertThresholdPercent = dto.AlertThresholdPercent,
                Month = dto.Month,
                Year = dto.Year,
                Currency = dto.Currency
            };

            _context.Budgets.Add(budget);
            await _context.SaveChangesAsync();

            var amountSpent = await GetAmountSpentAsync(userId, dto.Category, dto.Month, dto.Year);
            return MapToDto(budget, amountSpent);
        }

        public async Task<BudgetDto?> GetByIdAsync(Guid userId, Guid budgetId)
        {
            var budget = await _context.Budgets
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == budgetId && b.UserId == userId);

            if (budget == null)
                return null;

            var amountSpent = await GetAmountSpentAsync(userId, budget.Category, budget.Month, budget.Year);
            return MapToDto(budget, amountSpent);
        }

        public async Task<List<BudgetDto>> GetAllAsync(Guid userId, int? month = null, int? year = null)
        {
            var targetMonth = month ?? DateTime.UtcNow.Month;
            var targetYear = year ?? DateTime.UtcNow.Year;

            var budgets = await _context.Budgets
                .AsNoTracking()
                .Where(b => b.UserId == userId && b.Month == targetMonth && b.Year == targetYear)
                .OrderBy(b => b.Category)
                .ToListAsync();

            var result = new List<BudgetDto>();
            foreach (var budget in budgets)
            {
                var amountSpent = await GetAmountSpentAsync(userId, budget.Category, targetMonth, targetYear);
                result.Add(MapToDto(budget, amountSpent));
            }

            return result;
        }

        public async Task<BudgetDto> UpdateAsync(Guid userId, Guid budgetId, UpdateBudgetDto dto)
        {
            var budget = await _context.Budgets
                .FirstOrDefaultAsync(b => b.Id == budgetId && b.UserId == userId);

            if (budget == null)
                throw new KeyNotFoundException($"Budget with ID {budgetId} not found.");

            if (dto.MonthlyLimit.HasValue)
                budget.MonthlyLimit = dto.MonthlyLimit.Value;

            if (dto.AlertThresholdPercent.HasValue)
                budget.AlertThresholdPercent = dto.AlertThresholdPercent.Value;

            if (!string.IsNullOrWhiteSpace(dto.Currency))
                budget.Currency = dto.Currency;

            budget.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var amountSpent = await GetAmountSpentAsync(userId, budget.Category, budget.Month, budget.Year);
            return MapToDto(budget, amountSpent);
        }

        public async Task<bool> DeleteAsync(Guid userId, Guid budgetId)
        {
            var budget = await _context.Budgets
                .FirstOrDefaultAsync(b => b.Id == budgetId && b.UserId == userId);

            if (budget == null)
                return false;

            _context.Budgets.Remove(budget);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<BudgetAlertDto>> GetAlertsAsync(Guid userId, int? month = null, int? year = null)
        {
            var targetMonth = month ?? DateTime.UtcNow.Month;
            var targetYear = year ?? DateTime.UtcNow.Year;

            var budgets = await _context.Budgets
                .AsNoTracking()
                .Where(b => b.UserId == userId && b.Month == targetMonth && b.Year == targetYear)
                .ToListAsync();

            var alerts = new List<BudgetAlertDto>();
            foreach (var budget in budgets)
            {
                var amountSpent = await GetAmountSpentAsync(userId, budget.Category, targetMonth, targetYear);
                var percentUsed = budget.MonthlyLimit > 0
                    ? Math.Round(amountSpent / budget.MonthlyLimit * 100, 1)
                    : 0;

                if (percentUsed >= budget.AlertThresholdPercent)
                {
                    var isOverBudget = amountSpent > budget.MonthlyLimit;
                    alerts.Add(new BudgetAlertDto
                    {
                        BudgetId = budget.Id,
                        Category = budget.Category,
                        MonthlyLimit = budget.MonthlyLimit,
                        AmountSpent = amountSpent,
                        PercentUsed = percentUsed,
                        AlertLevel = isOverBudget ? "Exceeded" : "Warning",
                        Message = isOverBudget
                            ? $"Budget exceeded! You've spent ₹{amountSpent:N0} of ₹{budget.MonthlyLimit:N0} on {budget.Category}."
                            : $"Approaching budget limit: {percentUsed}% spent on {budget.Category} (₹{amountSpent:N0} of ₹{budget.MonthlyLimit:N0})."
                    });
                }
            }

            return alerts.OrderByDescending(a => a.PercentUsed).ToList();
        }

        public async Task<BudgetSummaryDto> GetSummaryAsync(Guid userId, int? month = null, int? year = null)
        {
            var targetMonth = month ?? DateTime.UtcNow.Month;
            var targetYear = year ?? DateTime.UtcNow.Year;

            var budgets = await GetAllAsync(userId, targetMonth, targetYear);

            return new BudgetSummaryDto
            {
                Month = targetMonth,
                Year = targetYear,
                TotalBudgets = budgets.Count,
                TotalBudgetLimit = budgets.Sum(b => b.MonthlyLimit),
                TotalSpent = budgets.Sum(b => b.AmountSpent),
                AlertCount = budgets.Count(b => b.IsAlertTriggered),
                OverBudgetCount = budgets.Count(b => b.IsOverBudget)
            };
        }

        // ─────────────────────────────────────────────────────────────────────
        // Private helpers
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Computes the total expense amount for a user's category in a given month/year.
        /// Only considers Expense-type transactions.
        /// </summary>
        private async Task<decimal> GetAmountSpentAsync(Guid userId, string category, int month, int year)
        {
            return await _context.Transactions
                .AsNoTracking()
                .Where(t =>
                    t.UserId == userId &&
                    t.Category == category &&
                    t.TransactionType == TransactionTypeEnum.Expense &&
                    t.TransactionDate.Month == month &&
                    t.TransactionDate.Year == year)
                .SumAsync(t => t.Amount);
        }

        private static BudgetDto MapToDto(Budget budget, decimal amountSpent)
        {
            return new BudgetDto
            {
                Id = budget.Id,
                Category = budget.Category,
                MonthlyLimit = budget.MonthlyLimit,
                AlertThresholdPercent = budget.AlertThresholdPercent,
                Month = budget.Month,
                Year = budget.Year,
                Currency = budget.Currency,
                AmountSpent = amountSpent
            };
        }
    }
}
