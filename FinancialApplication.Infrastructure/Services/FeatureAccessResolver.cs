using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FinancialApplication.Application.Interfaces;
using FinancialApplication.Domain.Domain.Enums;
using FinancialApplication.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace FinancialApplication.Infrastructure.Services
{
    /// <summary>
    /// Core engine that determines whether a user has access to a specific feature.
    /// Uses IMemoryCache with a cache-first strategy:
    ///   - user_features:{userId}  → 5-minute sliding expiration
    ///   - plan_features:{planId}  → 10-minute sliding expiration (internal helper)
    /// Cache invalidation is triggered by admin operations on features, plans, and subscriptions.
    /// </summary>
    public class FeatureAccessResolver : IFeatureAccessResolver
    {
        private readonly AppDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<FeatureAccessResolver> _logger;

        // Prefix constants for cache keys
        private const string UserFeaturesCachePrefix = "user_features:";
        private const string PlanFeaturesCachePrefix = "plan_features:";

        // Sliding expiration durations
        private static readonly TimeSpan UserFeaturesCacheExpiration = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan PlanFeaturesCacheExpiration = TimeSpan.FromMinutes(10);

        /// <summary>
        /// CancellationTokenSource used to bulk-invalidate all feature-related cache entries.
        /// When cancelled, every cache entry linked to this token is evicted immediately.
        /// A new CTS is created after each global invalidation.
        /// </summary>
        private CancellationTokenSource _globalCts = new();

        public FeatureAccessResolver(
            AppDbContext context,
            IMemoryCache cache,
            ILogger<FeatureAccessResolver> logger)
        {
            _context = context;
            _cache   = cache;
            _logger  = logger;
        }

        // ─────────────────────────────────────────────────────────────────────
        // PUBLIC — Feature Resolution
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns all feature keys the user currently has access to based on their
        /// active/trial subscription and the plan's assigned (active) features.
        /// Results are cached with a 5-minute sliding expiration.
        /// </summary>
        public async Task<HashSet<string>> GetUserFeaturesAsync(Guid userId)
        {
            var cacheKey = $"{UserFeaturesCachePrefix}{userId}";

            if (_cache.TryGetValue(cacheKey, out HashSet<string>? cachedFeatures) && cachedFeatures != null)
            {
                _logger.LogDebug("Cache HIT for user features: {UserId}", userId);
                return cachedFeatures;
            }

            _logger.LogDebug("Cache MISS for user features: {UserId} — querying database", userId);

            // Resolve features from DB:
            //   Features f
            //   JOIN PlanFeatures pf  ON f.Id = pf.FeatureId
            //   JOIN UserSubscriptions us ON pf.PlanId = us.PlanId
            //   WHERE us.UserId = @userId
            //     AND us.Status IN ('Active','Trial')
            //     AND us.EndDate > UTC_NOW
            //     AND f.IsActive = true
            var now = DateTime.UtcNow;

            var features = await _context.Features
                .Where(f => f.IsActive)
                .Where(f => f.PlanFeatures.Any(pf =>
                    _context.UserSubscriptions.Any(us =>
                        us.PlanId == pf.PlanId &&
                        us.UserId == userId &&
                        (us.Status == SubscriptionStatusEnum.Active ||
                         us.Status == SubscriptionStatusEnum.Trial ||
                         (us.Status == SubscriptionStatusEnum.Cancelled && us.EndDate > now)) &&
                        us.EndDate > now)))
                .Select(f => f.FeatureKey)
                .ToListAsync();

            var featureSet = new HashSet<string>(features, StringComparer.OrdinalIgnoreCase);

            // Cache with sliding expiration, linked to global CTS for bulk invalidation
            var cacheOptions = new MemoryCacheEntryOptions
            {
                SlidingExpiration = UserFeaturesCacheExpiration
            };
            cacheOptions.AddExpirationToken(new CancellationChangeToken(_globalCts.Token));

            _cache.Set(cacheKey, featureSet, cacheOptions);

            _logger.LogInformation(
                "Resolved {FeatureCount} features for user {UserId}: [{Features}]",
                featureSet.Count, userId, string.Join(", ", featureSet));

            return featureSet;
        }

        /// <summary>
        /// Checks if a specific user has access to a given feature key.
        /// Delegates to GetUserFeaturesAsync (cached) and checks containment.
        /// </summary>
        public async Task<bool> HasFeatureAsync(Guid userId, string featureKey)
        {
            if (string.IsNullOrWhiteSpace(featureKey))
                return false;

            var features = await GetUserFeaturesAsync(userId);
            return features.Contains(featureKey);
        }

        // ─────────────────────────────────────────────────────────────────────
        // PUBLIC — Cache Invalidation
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Invalidates the cached feature set for a single user.
        /// Called when a user's subscription changes (subscribe, upgrade, downgrade, cancel, reactivate).
        /// </summary>
        public void InvalidateUserCache(Guid userId)
        {
            var cacheKey = $"{UserFeaturesCachePrefix}{userId}";
            _cache.Remove(cacheKey);
            _logger.LogInformation("Invalidated feature cache for user {UserId}", userId);
        }

        /// <summary>
        /// Invalidates the cached feature sets for ALL users subscribed to a specific plan.
        /// Called when a plan's features are modified (assign/remove feature, toggle feature on plan).
        /// Queries the DB for all active/trial subscribers on the plan, then removes each user's cache entry.
        /// </summary>
        public void InvalidatePlanCache(Guid planId)
        {
            _logger.LogInformation("Invalidating feature cache for all users on plan {PlanId}", planId);

            // Query all users with an active/trial subscription on this plan
            var userIds = _context.UserSubscriptions
                .Where(us => us.PlanId == planId &&
                            (us.Status == SubscriptionStatusEnum.Active ||
                             us.Status == SubscriptionStatusEnum.Trial))
                .Select(us => us.UserId)
                .Distinct()
                .ToList();

            foreach (var userId in userIds)
            {
                var cacheKey = $"{UserFeaturesCachePrefix}{userId}";
                _cache.Remove(cacheKey);
            }

            // Also remove the plan-level cache entry if one exists
            _cache.Remove($"{PlanFeaturesCachePrefix}{planId}");

            _logger.LogInformation(
                "Invalidated feature cache for {UserCount} users on plan {PlanId}",
                userIds.Count, planId);
        }

        /// <summary>
        /// Invalidates ALL feature-related cache entries across every user and plan.
        /// Called when a global change occurs (e.g., toggling a feature's IsActive flag).
        /// Uses the CancellationTokenSource pattern: cancelling the token evicts all linked entries.
        /// </summary>
        public void InvalidateAllCaches()
        {
            _logger.LogWarning("Global cache invalidation triggered — evicting ALL feature caches");

            // Cancel the current CTS → all cache entries linked to it are evicted
            _globalCts.Cancel();
            _globalCts.Dispose();

            // Create a fresh CTS for future cache entries
            _globalCts = new CancellationTokenSource();
        }
    }
}
