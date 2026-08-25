using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FinancialApplication.Application.DTOs.Subscription;

namespace FinancialApplication.Application.Interfaces
{
    /// <summary>
    /// Service interface for Feature CRUD operations. Used by Admin user.
    /// </summary>
    public interface IFeatureService
    {
        Task<List<FeatureDto>> GetAllFeaturesAsync();
        Task<FeatureDto?> GetFeatureByIdAsync(Guid id);
        Task<FeatureDto> CreateFeatureAsync(CreateFeatureRequest request);
        Task<FeatureDto> UpdateFeatureAsync(Guid id, UpdateFeatureRequest request);
        Task<bool> DeleteFeatureAsync(Guid id);
        Task<bool> ToggleFeatureAsync(Guid id);
    }
}
