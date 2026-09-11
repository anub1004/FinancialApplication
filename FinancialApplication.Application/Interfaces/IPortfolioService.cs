using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FinancialApplication.Application.DTOs.Portfolio;

namespace FinancialApplication.Application.Interfaces
{
    public interface IPortfolioService
    {
        Task<PortfolioAssetDto> CreateAsync(Guid userId, CreatePortfolioAssetDto dto);
        Task<PortfolioAssetDto?> GetByIdAsync(Guid userId, Guid assetId);
        Task<List<PortfolioAssetDto>> GetAllAsync(Guid userId);
        Task<PortfolioAssetDto> UpdateAsync(Guid userId, Guid assetId, UpdatePortfolioAssetDto dto);
        Task<bool> DeleteAsync(Guid userId, Guid assetId);
        Task<PortfolioSummaryDto> GetSummaryAsync(Guid userId);
    }
}
