using System.Text.Json;
using FinancialApplication.Api.Services;
using FinancialApplication.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace FinancialApplication.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FinanceNewsController : ControllerBase
    {
        private readonly AppDbContext _dbContext;
        private readonly IMemoryCache _cache;

        public FinanceNewsController(AppDbContext dbContext, IMemoryCache cache)
        {
            _dbContext = dbContext;
            _cache = cache;
        }

        /// <summary>
        /// Returns processed finance news articles from the database.
        /// Data is already sorted by the NewsDataUpdateService (images first, no-images last).
        /// This controller just reads, caches, and paginates — no re-processing needed.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetNews(
            [FromQuery] int? page = null, 
            [FromQuery] int? pageSize = null, 
            [FromQuery] string? search = null)
        {
            // Read from pre-warmed cache (populated by NewsCacheWarmupService on startup)
            if (!_cache.TryGetValue(NewsCacheWarmupService.FinanceNewsCacheKey, out CachedNewsData? cachedData) || cachedData == null)
            {
                // Cache miss fallback — load from DB
                cachedData = await LoadAndCacheAsync();

                if (cachedData == null)
                {
                    return Ok(page.HasValue && pageSize.HasValue
                        ? new { totalItems = 0, totalPages = 0, pageNumber = 1, pageSize = pageSize ?? 10, articleCount = 0, items = Array.Empty<object>() }
                        : (object)new { articleCount = 0, items = Array.Empty<object>() });
                }
            }

            return BuildResponse(cachedData, page, pageSize, search);
        }

        private async Task<CachedNewsData?> LoadAndCacheAsync()
        {
            var record = await _dbContext.FinanceNewsArticles
                .AsNoTracking()
                .OrderByDescending(n => n.CreatedAt)
                .FirstOrDefaultAsync();

            if (record == null) return null;

            // Data is already sorted by NewsProcessingService — just parse, no re-sorting
            var articles = ParseArticles(record.JsonData);
            var data = new CachedNewsData { Articles = articles, ArticleCount = record.ArticleCount };
            _cache.Set(NewsCacheWarmupService.FinanceNewsCacheKey, data, NewsCacheWarmupService.CacheDuration);
            return data;
        }

        /// <summary>
        /// Parse the JSON array without sorting — data is pre-sorted by the update service.
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

        private IActionResult BuildResponse(CachedNewsData cachedData, int? page, int? pageSize, string? search)
        {
            var allArticles = cachedData.Articles;

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(search))
            {
                allArticles = allArticles
                    .Where(el => el.GetRawText().Contains(search, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            int totalItems = allArticles.Count;

            if (page.HasValue && pageSize.HasValue)
            {
                int pageVal = page.Value <= 0 ? 1 : page.Value;
                int pageSizeVal = pageSize.Value <= 0 || pageSize.Value > 100 ? 10 : pageSize.Value;
                int totalPages = (int)Math.Ceiling(totalItems / (double)pageSizeVal);

                var pagedItems = allArticles
                    .Skip((pageVal - 1) * pageSizeVal)
                    .Take(pageSizeVal)
                    .ToList();

                return Ok(new
                {
                    totalItems,
                    totalPages,
                    pageNumber = pageVal,
                    pageSize = pageSizeVal,
                    articleCount = cachedData.ArticleCount,
                    items = pagedItems
                });
            }

            return Ok(new
            {
                articleCount = cachedData.ArticleCount,
                items = allArticles
            });
        }
    }
}
