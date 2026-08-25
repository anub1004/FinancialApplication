using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FinancialApplication.Application.DTOs.Subscription;

namespace FinancialApplication.Application.Interfaces
{
    /// <summary>
    /// Service interface for user subscription lifecycle operations (subscribe, upgrade, downgrade, cancel, reactivate).
    /// </summary>
    public interface ISubscriptionService
    {
        Task<UserSubscriptionDto?> GetCurrentSubscriptionAsync(Guid userId);
        Task<List<string>> GetUserFeaturesAsync(Guid userId);
        Task<UserSubscriptionDto> SubscribeAsync(Guid userId, SubscribeRequest request);
        Task<UserSubscriptionDto> UpgradeAsync(Guid userId, UpgradeRequest request);
        Task<UserSubscriptionDto> DowngradeAsync(Guid userId, DowngradeRequest request);
        Task<bool> CancelAsync(Guid userId, CancelRequest request);
        Task<UserSubscriptionDto> ReactivateAsync(Guid userId);
        Task<List<SubscriptionHistoryDto>> GetHistoryAsync(Guid userId);
        Task<UserSubscriptionDto> CreateSubscriptionForNewUserAsync(Guid userId, Guid? selectedPlanId);
    }
}
