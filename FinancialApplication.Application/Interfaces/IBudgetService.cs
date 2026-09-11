using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FinancialApplication.Application.DTOs.Budget;

namespace FinancialApplication.Application.Interfaces
{
    /// <summary>
    /// Service interface for Budget CRUD, alerts, and summary operations.
    /// </summary>
    public interface IBudgetService
    {
        Task<BudgetDto> CreateAsync(Guid userId, CreateBudgetDto dto);
        Task<BudgetDto?> GetByIdAsync(Guid userId, Guid budgetId);
        Task<List<BudgetDto>> GetAllAsync(Guid userId, int? month = null, int? year = null);
        Task<BudgetDto> UpdateAsync(Guid userId, Guid budgetId, UpdateBudgetDto dto);
        Task<bool> DeleteAsync(Guid userId, Guid budgetId);
        Task<List<BudgetAlertDto>> GetAlertsAsync(Guid userId, int? month = null, int? year = null);
        Task<BudgetSummaryDto> GetSummaryAsync(Guid userId, int? month = null, int? year = null);
    }
}
