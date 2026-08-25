using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FinancialApplication.Application.Interfaces
{
    /// <summary>
    /// Core engine interface to determine if a user has access to a specific feature, with cache management.
    /// </summary>
    public interface IFeatureAccessResolver
    {
        Task<HashSet<string>> GetUserFeaturesAsync(Guid userId);
        Task<bool> HasFeatureAsync(Guid userId, string featureKey);
        void InvalidateUserCache(Guid userId);
        void InvalidatePlanCache(Guid planId);
        void InvalidateAllCaches();
    }
}
