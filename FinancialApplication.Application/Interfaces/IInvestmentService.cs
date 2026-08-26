using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FinancialApplication.Application.DTOs.Investment;

namespace FinancialApplication.Application.Interfaces
{
    public interface IInvestmentService
    {
        Task<InvestmentDto> CreateAsync(Guid userId, CreateInvestmentDto dto);
        Task<InvestmentDto?> GetByIdAsync(Guid userId, Guid investmentId);
        Task<(List<InvestmentDto> Items, int TotalCount)> GetAllAsync(
            Guid userId,
            string? investmentType = null,
            string? status = null,
            string sortBy = "CreatedAt",
            string sortOrder = "desc",
            int page = 1,
            int pageSize = 20);
        Task<InvestmentDto> UpdateAsync(Guid userId, Guid investmentId, UpdateInvestmentDto dto);
        Task<bool> DeleteAsync(Guid userId, Guid investmentId);
        Task<InvestmentSummaryDto> GetPortfolioSummaryAsync(Guid userId);
    }
}
