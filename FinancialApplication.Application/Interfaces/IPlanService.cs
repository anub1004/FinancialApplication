using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FinancialApplication.Application.DTOs.Subscription;

namespace FinancialApplication.Application.Interfaces
{
    /// <summary>
    /// Service interface for Plan CRUD and PlanFeature mapping operations. Used by Admin user.
    /// </summary>
    public interface IPlanService
    {
        Task<List<PlanDto>> GetAllPlansAsync(bool includeInactive = false);
        Task<PlanDto?> GetPlanByIdAsync(Guid id);
        Task<PlanDto> CreatePlanAsync(CreatePlanRequest request);
        Task<PlanDto> UpdatePlanAsync(Guid id, UpdatePlanRequest request);
        Task<bool> DeletePlanAsync(Guid id);
        Task<bool> AssignFeatureToPlanAsync(Guid planId, Guid featureId);
        Task<bool> RemoveFeatureFromPlanAsync(Guid planId, Guid featureId);
        Task<bool> UpdatePricingAsync(Guid planId, UpdatePricingRequest request);
    }
}
