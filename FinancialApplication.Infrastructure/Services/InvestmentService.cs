using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FinancialApplication.Application.DTOs.Investment;
using FinancialApplication.Application.Interfaces;
using FinancialApplication.Domain.Domain.Entity;
using FinancialApplication.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FinancialApplication.Infrastructure.Services
{
    public class InvestmentService : IInvestmentService
    {
        private readonly AppDbContext _context;

        public InvestmentService(AppDbContext context)
        {
            _context = context;
        }

        // ── Create ──────────────────────────────────────────────────────────
        public async Task<InvestmentDto> CreateAsync(Guid userId, CreateInvestmentDto dto)
        {
            var currentValue = dto.CurrentValue ?? dto.Amount;
            var returns = currentValue - dto.Amount;
            var returnPct = dto.Amount != 0 ? (returns / dto.Amount) * 100 : 0;

            var investment = new Investment
            {
                InvestmentId = Guid.NewGuid(),
                UserId = userId,
                Name = dto.Name,
                Amount = dto.Amount,
                CurrentValue = currentValue,
                InvestmentType = dto.InvestmentType,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                Status = "Active",
                Returns = returns,
                ReturnPercentage = returnPct,
                Currency = dto.Currency,
                Notes = dto.Notes,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Investments.Add(investment);
            await _context.SaveChangesAsync();

            return MapToDto(investment);
        }

        // ── Get by ID ───────────────────────────────────────────────────────
        public async Task<InvestmentDto?> GetByIdAsync(Guid userId, Guid investmentId)
        {
            var investment = await _context.Investments
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.InvestmentId == investmentId && i.UserId == userId);

            return investment == null ? null : MapToDto(investment);
        }

        // ── Get All (with filters, sort, pagination) ────────────────────────
        public async Task<(List<InvestmentDto> Items, int TotalCount)> GetAllAsync(
            Guid userId,
            string? investmentType = null,
            string? status = null,
            string sortBy = "CreatedAt",
            string sortOrder = "desc",
            int page = 1,
            int pageSize = 20)
        {
            var query = _context.Investments
                .AsNoTracking()
                .Where(i => i.UserId == userId);

            if (!string.IsNullOrWhiteSpace(investmentType))
                query = query.Where(i => i.InvestmentType == investmentType);

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(i => i.Status == status);

            var totalCount = await query.CountAsync();

            query = sortBy?.ToLower() switch
            {
                "amount" => sortOrder == "asc" ? query.OrderBy(i => i.Amount) : query.OrderByDescending(i => i.Amount),
                "currentvalue" => sortOrder == "asc" ? query.OrderBy(i => i.CurrentValue) : query.OrderByDescending(i => i.CurrentValue),
                "returns" => sortOrder == "asc" ? query.OrderBy(i => i.Returns) : query.OrderByDescending(i => i.Returns),
                "startdate" => sortOrder == "asc" ? query.OrderBy(i => i.StartDate) : query.OrderByDescending(i => i.StartDate),
                "name" => sortOrder == "asc" ? query.OrderBy(i => i.Name) : query.OrderByDescending(i => i.Name),
                _ => sortOrder == "asc" ? query.OrderBy(i => i.CreatedAt) : query.OrderByDescending(i => i.CreatedAt),
            };

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items.Select(MapToDto).ToList(), totalCount);
        }

        // ── Update ──────────────────────────────────────────────────────────
        public async Task<InvestmentDto> UpdateAsync(Guid userId, Guid investmentId, UpdateInvestmentDto dto)
        {
            var investment = await _context.Investments
                .FirstOrDefaultAsync(i => i.InvestmentId == investmentId && i.UserId == userId)
                ?? throw new KeyNotFoundException("Investment not found.");

            if (dto.Name != null) investment.Name = dto.Name;
            if (dto.Amount.HasValue) investment.Amount = dto.Amount.Value;
            if (dto.CurrentValue.HasValue) investment.CurrentValue = dto.CurrentValue.Value;
            if (dto.InvestmentType != null) investment.InvestmentType = dto.InvestmentType;
            if (dto.StartDate.HasValue) investment.StartDate = dto.StartDate.Value;
            if (dto.EndDate.HasValue) investment.EndDate = dto.EndDate.Value;
            if (dto.Status != null) investment.Status = dto.Status;
            if (dto.Currency != null) investment.Currency = dto.Currency;
            if (dto.Notes != null) investment.Notes = dto.Notes;

            // Recalculate returns
            investment.Returns = investment.CurrentValue - investment.Amount;
            investment.ReturnPercentage = investment.Amount != 0
                ? (investment.Returns.Value / investment.Amount) * 100
                : 0;

            investment.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return MapToDto(investment);
        }

        // ── Delete ──────────────────────────────────────────────────────────
        public async Task<bool> DeleteAsync(Guid userId, Guid investmentId)
        {
            var investment = await _context.Investments
                .FirstOrDefaultAsync(i => i.InvestmentId == investmentId && i.UserId == userId);

            if (investment == null) return false;

            _context.Investments.Remove(investment);
            await _context.SaveChangesAsync();
            return true;
        }

        // ── Portfolio Summary ───────────────────────────────────────────────
        public async Task<InvestmentSummaryDto> GetPortfolioSummaryAsync(Guid userId)
        {
            var investments = await _context.Investments
                .AsNoTracking()
                .Where(i => i.UserId == userId)
                .ToListAsync();

            var active = investments.Where(i => i.Status == "Active").ToList();

            var totalInvested = investments.Sum(i => i.Amount);
            var totalCurrentValue = investments.Sum(i => i.CurrentValue);
            var totalReturns = totalCurrentValue - totalInvested;
            var overallReturnPct = totalInvested != 0 ? (totalReturns / totalInvested) * 100 : 0;

            var byType = investments
                .GroupBy(i => i.InvestmentType)
                .Select(g => new InvestmentTypeBreakdownDto
                {
                    InvestmentType = g.Key,
                    TotalInvested = g.Sum(i => i.Amount),
                    TotalCurrentValue = g.Sum(i => i.CurrentValue),
                    Count = g.Count()
                })
                .OrderByDescending(b => b.TotalInvested)
                .ToList();

            return new InvestmentSummaryDto
            {
                TotalInvested = totalInvested,
                TotalCurrentValue = totalCurrentValue,
                TotalReturns = totalReturns,
                OverallReturnPercentage = overallReturnPct,
                ActiveCount = active.Count,
                TotalCount = investments.Count,
                Currency = "INR",
                ByType = byType
            };
        }

        // ── Mapping ─────────────────────────────────────────────────────────
        private static InvestmentDto MapToDto(Investment i) => new()
        {
            InvestmentId = i.InvestmentId,
            Name = i.Name,
            Amount = i.Amount,
            CurrentValue = i.CurrentValue,
            InvestmentType = i.InvestmentType,
            StartDate = i.StartDate,
            EndDate = i.EndDate,
            Status = i.Status,
            Returns = i.Returns,
            ReturnPercentage = i.ReturnPercentage,
            Currency = i.Currency,
            Notes = i.Notes,
            CreatedAt = i.CreatedAt,
            UpdatedAt = i.UpdatedAt
        };
    }
}
