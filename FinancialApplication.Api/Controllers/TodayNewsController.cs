using System.Text.Json;
using FinancialApplication.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinancialApplication.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TodayNewsController : ControllerBase
    {
        private readonly AppDbContext _dbContext;

        public TodayNewsController(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// Returns processed today's news articles from the database.
        /// Reads the single DB record containing all articles as a JSON array,
        /// then applies search, sorting (images first), and pagination in-memory.
        /// Supports optional pagination via 'page' and 'pageSize' query parameters.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetNews(
            [FromQuery] int? page = null, 
            [FromQuery] int? pageSize = null, 
            [FromQuery] string? search = null)
        {
            // Read the single record containing all articles as a JSON array
            var record = await _dbContext.TodayNewsArticles
                .OrderByDescending(n => n.CreatedAt)
                .FirstOrDefaultAsync();

            if (record == null)
            {
                return Ok(page.HasValue && pageSize.HasValue
                    ? new { totalItems = 0, totalPages = 0, pageNumber = 1, pageSize = pageSize ?? 10, articleCount = 0, items = Array.Empty<object>() }
                    : (object)new { articleCount = 0, items = Array.Empty<object>() });
            }

            // Parse the JSON array from the single record
            List<JsonElement> allArticles;
            try
            {
                using var doc = JsonDocument.Parse(record.JsonData);
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    // Clone elements so they survive the JsonDocument disposal
                    allArticles = doc.RootElement.EnumerateArray()
                        .Select(el => el.Clone())
                        .ToList();
                }
                else
                {
                    // Legacy: single article object (not an array)
                    allArticles = new List<JsonElement> { doc.RootElement.Clone() };
                }
            }
            catch
            {
                return Ok(Array.Empty<object>());
            }

            // Apply search filter on the in-memory article list
            if (!string.IsNullOrWhiteSpace(search))
            {
                allArticles = allArticles
                    .Where(el => el.GetRawText().Contains(search, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            // Sort: two-level ordering
            // 1) Priority: articles with valid images first, blocked domains and no-image articles last
            // 2) Within each priority group: newest articles first (by date descending)
            var sortedArticles = allArticles
                .OrderBy(el =>
                {
                    // Push channelnewsasia articles to the back
                    if (el.TryGetProperty("url", out var urlProp)
                        && urlProp.ValueKind == JsonValueKind.String
                        && urlProp.GetString()?.Contains("channelnewsasia", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        return 1; // channelnewsasia → last
                    }
                    if (el.TryGetProperty("url", out var urlsProp)
                        && urlsProp.ValueKind == JsonValueKind.String
                        && urlsProp.GetString()?.Contains("globenewswire.com", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        return 1; // globenewswire.com → last
                    }
                    if (el.TryGetProperty("url", out var urlsaProp)
                       && urlsaProp.ValueKind == JsonValueKind.String
                       && urlsaProp.GetString()?.Contains("oregonlive.com", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        return 1; // oregonlive.com → last
                    }

                    if (el.TryGetProperty("imageUrl", out var imgProp)
                        && imgProp.ValueKind == JsonValueKind.String
                        && !string.IsNullOrWhiteSpace(imgProp.GetString())
                        && !imgProp.GetString()!.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                    {
                        return 0; // has valid HTTP image → first
                    }
                    return 1; // no image or data: URI → last
                })
                .ThenByDescending(el =>
                {
                    // Sort by article date descending (newest first) within each priority group
                    // Try common date field names from the API
                    foreach (var fieldName in new[] { "date", "published_at", "pubDate" })
                    {
                        if (el.TryGetProperty(fieldName, out var dateProp)
                            && dateProp.ValueKind == JsonValueKind.String
                            && DateTime.TryParse(dateProp.GetString(), out var parsedDate))
                        {
                            return parsedDate;
                        }
                    }
                    return DateTime.MinValue; // no date → sort last within group
                })
                .ToList();

            int totalItems = sortedArticles.Count;

            if (page.HasValue && pageSize.HasValue)
            {
                int pageVal = page.Value <= 0 ? 1 : page.Value;
                int pageSizeVal = pageSize.Value <= 0 || pageSize.Value > 100 ? 10 : pageSize.Value;
                int totalPages = (int)Math.Ceiling(totalItems / (double)pageSizeVal);

                var pagedItems = sortedArticles
                    .Skip((pageVal - 1) * pageSizeVal)
                    .Take(pageSizeVal)
                    .ToList();

                return Ok(new
                {
                    totalItems = totalItems,
                    totalPages = totalPages,
                    pageNumber = pageVal,
                    pageSize = pageSizeVal,
                    articleCount = record.ArticleCount,
                    items = pagedItems
                });
            }

            return Ok(new
            {
                articleCount = record.ArticleCount,
                items = sortedArticles
            });
        }
    }
}
