using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FinancialApplication.Application.DTOs.Transaction;

namespace FinancialApplication.Application.Interfaces
{
    public interface ITransactionService
    {
        Task<TransactionDto> CreateAsync(Guid userId, CreateTransactionDto dto);
        Task<TransactionDto?> GetByIdAsync(Guid userId, Guid transactionId);
        Task<(List<TransactionDto> Items, int TotalCount)> GetAllAsync(
            Guid userId,
            string? category = null,
            string? type = null,       // "Income" or "Expense"
            DateTime? fromDate = null,
            DateTime? toDate = null,
            decimal? minAmount = null,
            decimal? maxAmount = null,
            string? search = null,
            string sortBy = "TransactionDate",
            string sortOrder = "desc",
            int page = 1,
            int pageSize = 20);
        Task<TransactionDto> UpdateAsync(Guid userId, Guid transactionId, UpdateTransactionDto dto);
        Task<bool> DeleteAsync(Guid userId, Guid transactionId);
        Task<TransactionSummaryDto> GetMonthlySummaryAsync(Guid userId, int? month = null, int? year = null);
        Task<List<CategoryDto>> GetCategoriesAsync();
    }
}
