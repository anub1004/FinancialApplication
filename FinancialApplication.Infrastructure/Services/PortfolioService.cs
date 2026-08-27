using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FinancialApplication.Application.DTOs.Portfolio;
using FinancialApplication.Application.Interfaces;
using FinancialApplication.Domain.Domain.Entity;
using FinancialApplication.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FinancialApplication.Infrastructure.Services
{
    public class PortfolioService : IPortfolioService
    {
        private readonly AppDbContext _db;

        public PortfolioService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<PortfolioAssetDto> CreateAsync(Guid userId, CreatePortfolioAssetDto dto)
        {
            var asset = new PortfolioAsset
            {
                UserId = userId,
                Name = dto.Name,
                AssetType = dto.AssetType,
                InvestedAmount = dto.InvestedAmount,
                CurrentValue = dto.CurrentValue,
                Color = dto.Color,
                Notes = dto.Notes,
                PurchaseDate = dto.PurchaseDate,
                Currency = dto.Currency,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.PortfolioAssets.Add(asset);
            await _db.SaveChangesAsync();

            // Recalculate allocation percentages for all user's assets
            await RecalculateAllocationsAsync(userId);

            return await MapToDtoAsync(userId, asset.PortfolioAssetId);
        }

        public async Task<PortfolioAssetDto?> GetByIdAsync(Guid userId, Guid assetId)
        {
            var asset = await _db.PortfolioAssets
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.PortfolioAssetId == assetId && a.UserId == userId);

            if (asset == null) return null;
            return MapToDto(asset, await GetTotalCurrentValueAsync(userId));
        }

        public async Task<List<PortfolioAssetDto>> GetAllAsync(Guid userId)
        {
            var assets = await _db.PortfolioAssets
                .AsNoTracking()
                .Where(a => a.UserId == userId)
                .OrderBy(a => a.AssetType)
                .ThenByDescending(a => a.CurrentValue)
                .ToListAsync();

            var totalCurrentValue = assets.Sum(a => a.CurrentValue);
            return assets.Select(a => MapToDto(a, totalCurrentValue)).ToList();
        }

        public async Task<PortfolioAssetDto> UpdateAsync(Guid userId, Guid assetId, UpdatePortfolioAssetDto dto)
        {
            var asset = await _db.PortfolioAssets
                .FirstOrDefaultAsync(a => a.PortfolioAssetId == assetId && a.UserId == userId)
                ?? throw new KeyNotFoundException("Portfolio asset not found.");

            if (dto.Name != null) asset.Name = dto.Name;
            if (dto.AssetType != null) asset.AssetType = dto.AssetType;
            if (dto.InvestedAmount.HasValue) asset.InvestedAmount = dto.InvestedAmount.Value;
            if (dto.CurrentValue.HasValue) asset.CurrentValue = dto.CurrentValue.Value;
            if (dto.Color != null) asset.Color = dto.Color;
            if (dto.Notes != null) asset.Notes = dto.Notes;
            if (dto.PurchaseDate.HasValue) asset.PurchaseDate = dto.PurchaseDate.Value;
            if (dto.Currency != null) asset.Currency = dto.Currency;
            asset.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            await RecalculateAllocationsAsync(userId);

            return await MapToDtoAsync(userId, asset.PortfolioAssetId);
        }

        public async Task<bool> DeleteAsync(Guid userId, Guid assetId)
        {
            var asset = await _db.PortfolioAssets
                .FirstOrDefaultAsync(a => a.PortfolioAssetId == assetId && a.UserId == userId);

            if (asset == null) return false;

            _db.PortfolioAssets.Remove(asset);
            await _db.SaveChangesAsync();
            await RecalculateAllocationsAsync(userId);

            return true;
        }

        public async Task<PortfolioSummaryDto> GetSummaryAsync(Guid userId)
        {
            var assets = await _db.PortfolioAssets
                .AsNoTracking()
                .Where(a => a.UserId == userId)
                .ToListAsync();

            var totalInvested = assets.Sum(a => a.InvestedAmount);
            var totalCurrentValue = assets.Sum(a => a.CurrentValue);
            var totalReturns = totalCurrentValue - totalInvested;

            return new PortfolioSummaryDto
            {
                TotalInvested = totalInvested,
                TotalCurrentValue = totalCurrentValue,
                TotalReturns = totalReturns,
                OverallReturnPercentage = totalInvested > 0 ? Math.Round((totalReturns / totalInvested) * 100, 2) : 0,
                AssetCount = assets.Count,
                Currency = assets.FirstOrDefault()?.Currency ?? "INR",
                ByType = assets
                    .GroupBy(a => a.AssetType)
                    .Select(g => new AssetTypeBreakdownDto
                    {
                        AssetType = g.Key,
                        TotalInvested = g.Sum(a => a.InvestedAmount),
                        TotalCurrentValue = g.Sum(a => a.CurrentValue),
                        AllocationPercentage = totalCurrentValue > 0 ? Math.Round((g.Sum(a => a.CurrentValue) / totalCurrentValue) * 100, 2) : 0,
                        Count = g.Count()
                    })
                    .OrderByDescending(b => b.AllocationPercentage)
                    .ToList()
            };
        }

        // ── Private helpers ──────────────────────────────────────────────────

        private async Task RecalculateAllocationsAsync(Guid userId)
        {
            var assets = await _db.PortfolioAssets
                .Where(a => a.UserId == userId)
                .ToListAsync();

            var totalCurrentValue = assets.Sum(a => a.CurrentValue);

            foreach (var asset in assets)
            {
                asset.AllocationPercentage = totalCurrentValue > 0
                    ? Math.Round((asset.CurrentValue / totalCurrentValue) * 100, 2)
                    : 0;
            }

            await _db.SaveChangesAsync();
        }

        private async Task<decimal> GetTotalCurrentValueAsync(Guid userId)
        {
            return await _db.PortfolioAssets
                .Where(a => a.UserId == userId)
                .SumAsync(a => a.CurrentValue);
        }

        private async Task<PortfolioAssetDto> MapToDtoAsync(Guid userId, Guid assetId)
        {
            var asset = await _db.PortfolioAssets.AsNoTracking()
                .FirstAsync(a => a.PortfolioAssetId == assetId && a.UserId == userId);
            var totalCV = await GetTotalCurrentValueAsync(userId);
            return MapToDto(asset, totalCV);
        }

        private static PortfolioAssetDto MapToDto(PortfolioAsset a, decimal totalCurrentValue)
        {
            var pl = a.CurrentValue - a.InvestedAmount;
            return new PortfolioAssetDto
            {
                PortfolioAssetId = a.PortfolioAssetId,
                Name = a.Name,
                AssetType = a.AssetType,
                InvestedAmount = a.InvestedAmount,
                CurrentValue = a.CurrentValue,
                AllocationPercentage = totalCurrentValue > 0 ? Math.Round((a.CurrentValue / totalCurrentValue) * 100, 2) : 0,
                ProfitLoss = pl,
                ReturnPercentage = a.InvestedAmount > 0 ? Math.Round((pl / a.InvestedAmount) * 100, 2) : 0,
                Color = a.Color,
                Notes = a.Notes,
                PurchaseDate = a.PurchaseDate,
                Currency = a.Currency,
                CreatedAt = a.CreatedAt,
                UpdatedAt = a.UpdatedAt
            };
        }
    }
}
