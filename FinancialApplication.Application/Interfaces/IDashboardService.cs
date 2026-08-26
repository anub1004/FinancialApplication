using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FinancialApplication.Application.DTOs.Dashboard;

namespace FinancialApplication.Application.Interfaces
{
    public interface IDashboardService
    {
        /// <summary>
        /// Main dashboard summary: income, expenses, balance, investment/goal snapshots.
        /// </summary>
        Task<DashboardSummaryDto> GetSummaryAsync(Guid userId, int? month = null, int? year = null);

        /// <summary>
        /// Income vs Expense trend for the last N months.
        /// </summary>
        Task<List<MonthlyTrendDto>> GetMonthlyTrendAsync(Guid userId, int months = 6);

        /// <summary>
        /// Expense breakdown by category for a given month.
        /// </summary>
        Task<List<CategoryBreakdownDto>> GetCategoryBreakdownAsync(Guid userId, int? month = null, int? year = null);

        /// <summary>
        /// Recent activity feed (transactions, investments, goals combined).
        /// </summary>
        Task<List<RecentActivityDto>> GetRecentActivityAsync(Guid userId, int count = 10);
    }
}
