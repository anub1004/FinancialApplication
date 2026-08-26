using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FinancialApplication.Application.DTOs.Goal;
using FinancialApplication.Application.Interfaces;
using FinancialApplication.Domain.Domain.Entity;
using FinancialApplication.Domain.Domain.Enums;
using FinancialApplication.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FinancialApplication.Infrastructure.Services
{
    public class GoalService : IGoalService
    {
        private readonly AppDbContext _context;

        public GoalService(AppDbContext context)
        {
            _context = context;
        }

        // ── Create ──────────────────────────────────────────────────────────
        public async Task<GoalDto> CreateAsync(Guid userId, CreateGoalDto dto)
        {
            var goal = new Goal
            {
                GoalId = Guid.NewGuid(),
                UserId = userId,
                Title = dto.Title,
                Description = dto.Description,
                TargetAmount = dto.TargetAmount,
                CurrentAmount = dto.CurrentAmount,
                Deadline = dto.Deadline,
                Status = dto.CurrentAmount > 0 ? GoalStatusEnum.InProgress : GoalStatusEnum.NotStarted,
                Icon = dto.Icon,
                Color = dto.Color,
                Currency = dto.Currency,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Goals.Add(goal);
            await _context.SaveChangesAsync();

            return MapToDto(goal);
        }

        // ── Get by ID ───────────────────────────────────────────────────────
        public async Task<GoalDto?> GetByIdAsync(Guid userId, Guid goalId)
        {
            var goal = await _context.Goals
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.GoalId == goalId && g.UserId == userId);

            return goal == null ? null : MapToDto(goal);
        }

        // ── Get All ─────────────────────────────────────────────────────────
        public async Task<List<GoalDto>> GetAllAsync(Guid userId, string? status = null)
        {
            var query = _context.Goals
                .AsNoTracking()
                .Where(g => g.UserId == userId);

            if (!string.IsNullOrWhiteSpace(status))
            {
                if (Enum.TryParse<GoalStatusEnum>(status, true, out var statusEnum))
                    query = query.Where(g => g.Status == statusEnum);
            }

            var goals = await query
                .OrderByDescending(g => g.CreatedAt)
                .ToListAsync();

            return goals.Select(MapToDto).ToList();
        }

        // ── Update ──────────────────────────────────────────────────────────
        public async Task<GoalDto> UpdateAsync(Guid userId, Guid goalId, UpdateGoalDto dto)
        {
            var goal = await _context.Goals
                .FirstOrDefaultAsync(g => g.GoalId == goalId && g.UserId == userId)
                ?? throw new KeyNotFoundException("Goal not found.");

            if (dto.Title != null) goal.Title = dto.Title;
            if (dto.Description != null) goal.Description = dto.Description;
            if (dto.TargetAmount.HasValue) goal.TargetAmount = dto.TargetAmount.Value;
            if (dto.Deadline.HasValue) goal.Deadline = dto.Deadline.Value;
            if (dto.Icon != null) goal.Icon = dto.Icon;
            if (dto.Color != null) goal.Color = dto.Color;
            if (dto.Currency != null) goal.Currency = dto.Currency;

            goal.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return MapToDto(goal);
        }

        // ── Delete ──────────────────────────────────────────────────────────
        public async Task<bool> DeleteAsync(Guid userId, Guid goalId)
        {
            var goal = await _context.Goals
                .FirstOrDefaultAsync(g => g.GoalId == goalId && g.UserId == userId);

            if (goal == null) return false;

            _context.Goals.Remove(goal);
            await _context.SaveChangesAsync();
            return true;
        }

        // ── Contribute ──────────────────────────────────────────────────────
        public async Task<GoalDto> ContributeAsync(Guid userId, Guid goalId, GoalContributionDto dto)
        {
            var goal = await _context.Goals
                .FirstOrDefaultAsync(g => g.GoalId == goalId && g.UserId == userId)
                ?? throw new KeyNotFoundException("Goal not found.");

            if (goal.Status == GoalStatusEnum.Completed)
                throw new InvalidOperationException("Cannot contribute to a completed goal.");

            if (goal.Status == GoalStatusEnum.Failed)
                throw new InvalidOperationException("Cannot contribute to a failed goal. Reactivate it first.");

            goal.CurrentAmount += dto.Amount;

            // Auto-complete if target reached
            if (goal.CurrentAmount >= goal.TargetAmount)
            {
                goal.CurrentAmount = goal.TargetAmount; // Cap at target
                goal.Status = GoalStatusEnum.Completed;
            }
            else if (goal.Status == GoalStatusEnum.NotStarted)
            {
                goal.Status = GoalStatusEnum.InProgress;
            }

            goal.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return MapToDto(goal);
        }

        // ── Update Status ───────────────────────────────────────────────────
        public async Task<GoalDto> UpdateStatusAsync(Guid userId, Guid goalId, GoalStatusUpdateDto dto)
        {
            var goal = await _context.Goals
                .FirstOrDefaultAsync(g => g.GoalId == goalId && g.UserId == userId)
                ?? throw new KeyNotFoundException("Goal not found.");

            goal.Status = dto.Status;
            goal.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return MapToDto(goal);
        }

        // ── Mapping ─────────────────────────────────────────────────────────
        private static GoalDto MapToDto(Goal g)
        {
            var progress = g.TargetAmount > 0
                ? Math.Min(100, Math.Round((g.CurrentAmount / g.TargetAmount) * 100, 1))
                : 0;

            var daysRemaining = (g.Deadline.Date - DateTime.UtcNow.Date).Days;
            if (daysRemaining < 0) daysRemaining = 0;

            // Calculate monthly target: remaining amount / remaining months
            decimal? monthlyTarget = null;
            var remaining = g.TargetAmount - g.CurrentAmount;
            if (remaining > 0 && daysRemaining > 0)
            {
                var monthsRemaining = Math.Max(1, daysRemaining / 30.0);
                monthlyTarget = Math.Round(remaining / (decimal)monthsRemaining, 2);
            }

            return new GoalDto
            {
                GoalId = g.GoalId,
                Title = g.Title,
                Description = g.Description,
                TargetAmount = g.TargetAmount,
                CurrentAmount = g.CurrentAmount,
                ProgressPercentage = progress,
                Deadline = g.Deadline,
                Status = g.Status,
                StatusName = g.Status.ToString(),
                Icon = g.Icon,
                Color = g.Color,
                Currency = g.Currency,
                DaysRemaining = daysRemaining,
                MonthlyTargetToComplete = monthlyTarget,
                CreatedAt = g.CreatedAt,
                UpdatedAt = g.UpdatedAt
            };
        }
    }
}
