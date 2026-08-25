using System.Text.Json;
using FinancialApplication.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FinancialApplication.Api.Services
{
    /// <summary>
    /// Background service that pre-loads and caches news data on app startup,
    /// so the very first API request is served instantly from cache.
    /// Also refreshes the cache periodically to pick up new data from the update job.
    /// </summary>
    public class NewsCacheWarmupService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IMemoryCache _cache;
        private readonly ILogger<NewsCacheWarmupService> _logger;

        // Shared cache keys — used by both this service and the controllers
        public const string FinanceNewsCacheKey = "FinanceNews_Cached";
        public const string TodayNewsCacheKey = "TodayNews_Cached";

        // Cache for 10 minutes — news update job runs every 6 hours
        public static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

        // How often to refresh the cache in the background
        private static readonly TimeSpan RefreshInterval = TimeSpan.FromMinutes(5);

        public NewsCacheWarmupService(
            IServiceScopeFactory scopeFactory,
            IMemoryCache cache,
            ILogger<NewsCacheWarmupService> logger)
        {
            _scopeFactory = scopeFactory;
            _cache = cache;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Warm cache immediately on startup
            await WarmCacheAsync(stoppingToken);

            // Periodically refresh the cache in the background
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(RefreshInterval, stoppingToken);
                await WarmCacheAsync(stoppingToken);
            }
        }

        private async Task WarmCacheAsync(CancellationToken ct)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // Warm caches sequentially — DbContext is not thread-safe
                await WarmFinanceNewsCacheAsync(dbContext, ct);
                await WarmTodayNewsCacheAsync(dbContext, ct);

                _logger.LogInformation("News cache warmed successfully");
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Failed to warm news cache, will retry on next interval");
            }
        }

        private async Task WarmFinanceNewsCacheAsync(AppDbContext dbContext, CancellationToken ct)
        {
            var record = await dbContext.FinanceNewsArticles
                .AsNoTracking()
                .OrderByDescending(n => n.CreatedAt)
                .FirstOrDefaultAsync(ct);

            if (record == null) return;

            // Data is already sorted by NewsProcessingService — just parse
            var articles = ParseArticles(record.JsonData);
            var cachedData = new CachedNewsData
            {
                Articles = articles,
                ArticleCount = record.ArticleCount
            };

            _cache.Set(FinanceNewsCacheKey, cachedData, CacheDuration);
            _logger.LogInformation("Cached {Count} finance news articles ({Size:N0} chars)",
                articles.Count, record.JsonData.Length);
        }

        private async Task WarmTodayNewsCacheAsync(AppDbContext dbContext, CancellationToken ct)
        {
            var record = await dbContext.TodayNewsArticles
                .AsNoTracking()
                .OrderByDescending(n => n.CreatedAt)
                .FirstOrDefaultAsync(ct);

            if (record == null) return;

            // Data is already sorted by NewsProcessingService — just parse
            var articles = ParseArticles(record.JsonData);
            var cachedData = new CachedNewsData
            {
                Articles = articles,
                ArticleCount = record.ArticleCount
            };

            _cache.Set(TodayNewsCacheKey, cachedData, CacheDuration);
            _logger.LogInformation("Cached {Count} today's news articles ({Size:N0} chars)",
                articles.Count, record.JsonData.Length);
        }

        /// <summary>
        /// Parses the raw JSON array string into a list of JsonElements.
        /// No sorting — the data is already sorted by NewsProcessingService.
        /// </summary>
        private static List<JsonElement> ParseArticles(string jsonData)
        {
            using var doc = JsonDocument.Parse(jsonData);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                return new List<JsonElement> { doc.RootElement.Clone() };

            var articles = new List<JsonElement>(doc.RootElement.GetArrayLength());
            foreach (var el in doc.RootElement.EnumerateArray())
            {
                articles.Add(el.Clone());
            }
            return articles;
        }
    }

    /// <summary>
    /// Cached news data shared between the warmup service and controllers.
    /// </summary>
    public class CachedNewsData
    {
        public List<JsonElement> Articles { get; set; } = new();
        public int ArticleCount { get; set; }
    }
}
