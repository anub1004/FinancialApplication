using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FinancialApplication.Application.DTOs.Goal;

namespace FinancialApplication.Application.Interfaces
{
    public interface IGoalService
    {
        Task<GoalDto> CreateAsync(Guid userId, CreateGoalDto dto);
        Task<GoalDto?> GetByIdAsync(Guid userId, Guid goalId);
        Task<List<GoalDto>> GetAllAsync(Guid userId, string? status = null);
        Task<GoalDto> UpdateAsync(Guid userId, Guid goalId, UpdateGoalDto dto);
        Task<bool> DeleteAsync(Guid userId, Guid goalId);
        Task<GoalDto> ContributeAsync(Guid userId, Guid goalId, GoalContributionDto dto);
        Task<GoalDto> UpdateStatusAsync(Guid userId, Guid goalId, GoalStatusUpdateDto dto);
    }
}
