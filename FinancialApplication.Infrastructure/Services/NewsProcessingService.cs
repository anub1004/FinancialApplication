using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Nodes;
using FinancialApplication.Application.Interfaces;
using FinancialApplication.Domain.Domain.Entity;
using FinancialApplication.Infrastructure.Data;
using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FinancialApplication.Infrastructure.Services
{
    /// <summary>
   
    /// </summary>
    public class NewsProcessingService : INewsProcessingService
    {
        private readonly AppDbContext _dbContext;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<NewsProcessingService> _logger;
        private readonly IConfiguration _configuration;

        // Cache scraped image URLs to prevent redundant scraping of same article URL
        // Use ConcurrentDictionary for thread-safe access without explicit locking
        private readonly ConcurrentDictionary<string, string?> _imageUrlCache;

        // Max concurrent scrapes read from configuration
        private readonly int _maxConcurrentScrapes;

        public NewsProcessingService(
            AppDbContext dbContext,
            IHttpClientFactory httpClientFactory,
            ILogger<NewsProcessingService> logger,
            IConfiguration configuration)
        {
            _dbContext = dbContext;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _configuration = configuration;

            _maxConcurrentScrapes = _configuration.GetValue("NewsService:MaxConcurrentScrapes", 10);

            // Initialize cache for image URLs. In production, consider external cache (Redis) for cross-instance sharing
            _imageUrlCache = new ConcurrentDictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        }

        /// <inheritdoc />
        public async Task<string> FetchNewsAsync(string apiUrl, CancellationToken ct = default)
        {
            var apiKey = _configuration["NewsService:ApiKey"]
                ?? _configuration["NewsConfigApi2:ApiKey"]
                ?? throw new InvalidOperationException("ApiKey is not configured.");

            var client = _httpClientFactory.CreateClient("NewsScraper");
            client.DefaultRequestHeaders.Remove("apikey");
            client.DefaultRequestHeaders.Add("apikey", apiKey);

            _logger.LogInformation("Fetching news from API: {Url}", apiUrl);

            var response = await client.GetAsync(apiUrl, ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            _logger.LogInformation("Successfully fetched news response ({Length} chars)", json.Length);

            return json;
        }

        /// <inheritdoc />
        public async Task<List<JsonObject>> ProcessArticlesAsync(JsonElement articles, CancellationToken ct = default)
        {
            if (articles.ValueKind != JsonValueKind.Array)
            {
                _logger.LogWarning("Articles element is not a JSON array");
                return new List<JsonObject>();
            }

            // Parse all articles upfront once for efficiency
            var articleList = articles.EnumerateArray()
                .Select(article =>
                {
                    var jsonObject = JsonNode.Parse(article.GetRawText())?.AsObject();
                    return jsonObject;
                })
                .Where(obj => obj != null)
                .ToList();

            // PERF: Pre-allocate two lists for O(n) partitioning
            // This avoids expensive sorting and maintains insertion order
            var articlesWithImages = new List<JsonObject>(articleList.Count / 2 + 1);
            var articlesWithoutImages = new List<JsonObject>(articleList.Count / 2);

            // FIX: Use configured MaxDegreeOfParallelism instead of 1
            // Previously MaxDegreeOfParallelism=1 forced sequential execution making the semaphore useless.
            // Now Parallel.ForEachAsync directly controls concurrency — no semaphore needed.
            var scrapeOptions = new ParallelOptions
            {
                CancellationToken = ct,
                MaxDegreeOfParallelism = _maxConcurrentScrapes
            };

            _logger.LogInformation("Scraping {Count} articles with MaxDegreeOfParallelism={MaxDop}",
                articleList.Count, _maxConcurrentScrapes);

            await Parallel.ForEachAsync(articleList, scrapeOptions, async (jsonObject, token) =>
            {
                if (jsonObject == null)
                {
                    return;
                }

                try
                {
                    var url = jsonObject["url"]?.GetValue<string>();

                    if (string.IsNullOrWhiteSpace(url))
                    {
                        _logger.LogWarning("Article has no 'url' field, will appear in no-image section");
                        jsonObject["imageUrl"] = null;
                        // Add to no-images list thread-safely
                        lock (articlesWithoutImages)
                        {
                            articlesWithoutImages.Add(jsonObject);
                        }
                        return;
                    }

                    // PERF: Check cache first to avoid duplicate scraping
                    // For high-volume news sites, many articles may link to same sources
                    string? imageUrl;

                    if (_imageUrlCache.TryGetValue(url, out var cachedUrl))
                    {
                        imageUrl = cachedUrl;
                        _logger.LogDebug("Cache hit for {Url}: {ImageUrl}", url, imageUrl ?? "null");
                    }
                    else
                    {
                        // Scrape the article page for an image URL
                        // Concurrency is controlled by Parallel.ForEachAsync's MaxDegreeOfParallelism
                        imageUrl = await ExtractImageUrlAsync(url, token);
                        // Cache result (including null) to avoid re-scraping failures
                        _imageUrlCache.TryAdd(url, imageUrl);
                    }   

                    jsonObject["imageUrl"] = imageUrl;

                    // PERF: O(n) partitioning - check for non-null/non-empty image and add to appropriate list
                    // This is more efficient than sorting after the fact (O(n log n))
                    if (!string.IsNullOrWhiteSpace(imageUrl))
                    {
                        lock (articlesWithImages)
                        {
                            articlesWithImages.Add(jsonObject);
                        }
                    }
                    else
                    {
                        lock (articlesWithoutImages)
                        {
                            articlesWithoutImages.Add(jsonObject);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // PERF: Log at Warning level instead of Error; this is a recoverable issue
                    // Individual article failures shouldn't alarm ops; we continue processing
                    var url = jsonObject?["url"]?.GetValue<string>() ?? "unknown";
                    _logger.LogWarning(ex, "Failed to process article {Url}. Continuing with next article", url);

                    // Still add article to no-images list to ensure it appears in results
                    jsonObject["imageUrl"] = null;
                    lock (articlesWithoutImages)
                    {
                        articlesWithoutImages.Add(jsonObject);
                    }
                }
            });

            // PERF: Combine lists - images first, then no-images
            // Total allocation: one final list of exact size
            var result = new List<JsonObject>(articlesWithImages.Count + articlesWithoutImages.Count);
            result.AddRange(articlesWithImages);
            result.AddRange(articlesWithoutImages);

            _logger.LogInformation("Processed {Total} articles: {WithImages} with images, {WithoutImages} without",
                result.Count, articlesWithImages.Count, articlesWithoutImages.Count);

            return result;
        }

        /// <inheritdoc />
        public async Task<string?> ExtractImageUrlAsync(string articleUrl, CancellationToken ct = default)
        {
            try
            {
                // PERF: Reuse HttpClient from factory to leverage connection pooling
                // IHttpClientFactory manages socket pooling and DNS caching
                var client = _httpClientFactory.CreateClient("NewsScraper");
                var html = await client.GetStringAsync(articleUrl, ct);

                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                string? imageUrl = null;

                // PERF: Use null-coalescing operator to short-circuit on first match
                // This avoids unnecessary XPath queries once we find an image

                // Priority 1: Meta Tags (fastest, most reliable for OpenGraph data)
                imageUrl =
                    doc.DocumentNode.SelectSingleNode("//meta[@property='og:image']")
                        ?.GetAttributeValue("content", null)
                    ?? doc.DocumentNode.SelectSingleNode("//meta[@property='og:image:url']")
                        ?.GetAttributeValue("content", null)
                    ?? doc.DocumentNode.SelectSingleNode("//meta[@name='twitter:image']")
                        ?.GetAttributeValue("content", null)
                    ?? doc.DocumentNode.SelectSingleNode("//meta[@name='twitter:image:src']")
                        ?.GetAttributeValue("content", null)
                    ?? doc.DocumentNode.SelectSingleNode("//meta[@itemprop='image']")
                        ?.GetAttributeValue("content", null);

                // Priority 2: link rel=image_src
                imageUrl ??=
                    doc.DocumentNode.SelectSingleNode("//link[@rel='image_src']")
                        ?.GetAttributeValue("href", null);

                // Priority 3: Find prominent image elements (article/main/hero/featured, etc.)
                // PERF: Use chained null-coalescing for readable fallback chain
                HtmlNode? imgNode =
                    doc.DocumentNode.SelectSingleNode("//article//img")
                    ?? doc.DocumentNode.SelectSingleNode("//main//img")
                    ?? doc.DocumentNode.SelectSingleNode("//img[contains(@class,'hero')]")
                    ?? doc.DocumentNode.SelectSingleNode("//img[contains(@class,'featured')]")
                    ?? doc.DocumentNode.SelectSingleNode("//img[contains(@class,'banner')]")
                    ?? doc.DocumentNode.SelectSingleNode("//img[contains(@class,'cover')]")
                    ?? doc.DocumentNode.SelectSingleNode("//img[contains(@class,'thumbnail')]")
                    ?? doc.DocumentNode.SelectSingleNode("//img[contains(@class,'post')]")
                    ?? doc.DocumentNode.SelectSingleNode("//img[contains(@class,'article')]")
                    ?? doc.DocumentNode.SelectSingleNode("//picture//img")
                    ?? doc.DocumentNode.SelectSingleNode("//img");

                if (imageUrl == null && imgNode != null)
                {
                    // Check multiple image src attributes (covers lazy-load variants)
                    imageUrl =
                        imgNode.GetAttributeValue("src", null)
                        ?? imgNode.GetAttributeValue("data-src", null)
                        ?? imgNode.GetAttributeValue("data-lazy-src", null)
                        ?? imgNode.GetAttributeValue("data-original", null)
                        ?? imgNode.GetAttributeValue("data-image", null)
                        ?? imgNode.GetAttributeValue("data-srcset", null)
                        ?? imgNode.GetAttributeValue("srcset", null);
                }

                // PERF: Normalize relative URLs to absolute in one pass
                if (!string.IsNullOrWhiteSpace(imageUrl))
                {
                    // FIX: Reject base64 data URIs (e.g. "data:image/jpg;base64,...")
                    // These are not valid HTTP URLs and cause errors in downstream image
                    // proxy/resizer services ("Error when parsing query string").
                    if (imageUrl.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogDebug("Skipping data URI image for {ArticleUrl}", articleUrl);
                        imageUrl = null;
                    }
                    // Check if URL is relative (no scheme)
                    else if (Uri.TryCreate(imageUrl, UriKind.Relative, out _))
                    {
                        try
                        {
                            imageUrl = new Uri(new Uri(articleUrl), imageUrl).ToString();
                        }
                        catch
                        {
                            _logger.LogDebug("Failed to resolve relative URL {RelativeUrl} against {ArticleUrl}", imageUrl, articleUrl);
                            imageUrl = null;
                        }
                    }

                    // PERF: Handle srcset (multiple URLs) - extract first one efficiently
                    // Srcset format: "url1 1x, url2 2x" - we want the first URL
                    if (imageUrl?.Contains(",") == true || imageUrl?.Contains(" ") == true)
                    {
                        // Split by comma to get first srcset entry, then split by space to remove descriptor
                        var firstEntry = imageUrl.Split(',')[0].Trim();
                        imageUrl = firstEntry.Split(' ')[0].Trim();
                    }
                }

                // PERF: Only log on Debug level to reduce overhead in production
                // High-throughput scenarios can have thousands of articles; logging each adds latency
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    if (!string.IsNullOrWhiteSpace(imageUrl))
                    {
                        _logger.LogDebug("Extracted image for {Url}: {ImageUrl}", articleUrl, imageUrl);
                    }
                    else
                    {
                        _logger.LogDebug("No image found for {Url}", articleUrl);
                    }
                }

                return imageUrl;
            }
            catch (TaskCanceledException) when (ct.IsCancellationRequested)
            {
                // Let cancellation propagate if host requested shutdown
                throw;
            }
            catch (HttpRequestException ex)
            {
                // Network errors (404, timeout, etc.) are expected; don't alarm logs
                _logger.LogDebug(ex, "HTTP error extracting image from {Url}", articleUrl);
                return null;
            }
            catch (Exception ex)
            {
                // Unexpected errors (malformed HTML, parser issues) - still recoverable
                _logger.LogWarning(ex, "Unexpected error scraping image from {Url}", articleUrl);
                return null;
            }
        }

        /// <inheritdoc />
        public async Task DeleteOldNewsAsync(int retentionDays, bool isFinanceNews, CancellationToken ct = default)
        {
            // With single-record storage, we only keep the latest record and delete any stale ones.
            // In practice there should be at most 1 record, but this handles legacy multi-row data too.
            var cutoff = DateTime.UtcNow.AddDays(-retentionDays);

            if (isFinanceNews)
            {
                var deletedCount = await _dbContext.FinanceNewsArticles
                    .Where(n => n.CreatedAt < cutoff)
                    .ExecuteDeleteAsync(ct);

                _logger.LogInformation("Deleted {Count} Finance news records older than {Days} days (before {Cutoff:u})",
                    deletedCount, retentionDays, cutoff);
            }
            else
            {
                var deletedCount = await _dbContext.TodayNewsArticles
                    .Where(n => n.CreatedAt < cutoff)
                    .ExecuteDeleteAsync(ct);

                _logger.LogInformation("Deleted {Count} Today's news records older than {Days} days (before {Cutoff:u})",
                    deletedCount, retentionDays, cutoff);
            }
        }

        /// <inheritdoc />
        public async Task SaveNewsAsync(List<JsonObject> processedArticles, bool isFinanceNews, CancellationToken ct = default)
        {
            if (processedArticles.Count == 0)
            {
                _logger.LogWarning("No processed articles to save");
                return;
            }

            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = false,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
           

            try
            {
                // SINGLE-RECORD UPSERT PATTERN:
                // 1. Read the ONE existing record (if any)
                // 2. Parse its JSON array, extract existing URLs for dedup
                // 3. Merge new articles into the array
                // 4. Update the single record (or insert if first run)
                // Result: exactly 1 row in the table at all times

                if (isFinanceNews)
                {
                    await UpsertFinanceNewsAsync(processedArticles, jsonOptions, ct);
                }
                else
                {
                    await UpsertTodayNewsAsync(processedArticles, jsonOptions, ct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving {FeedType} articles to database",
                    isFinanceNews ? "Finance" : "Today's");
                throw;
            }
        }

        /// <summary>
        /// Upserts Finance news articles into exactly 1 database record.
        /// Reads the existing single record, merges new non-duplicate articles, and updates in place.
        /// </summary>
        private async Task UpsertFinanceNewsAsync(List<JsonObject> processedArticles, JsonSerializerOptions jsonOptions, CancellationToken ct)
        {
            // Get the single existing record (ordered by latest just in case legacy multi-row data exists)
            var existingRecord = await _dbContext.FinanceNewsArticles
                .OrderByDescending(n => n.CreatedAt)
                .FirstOrDefaultAsync(ct);

            // Build merged JSON array
            var mergedArray = MergeArticles(existingRecord?.JsonData, processedArticles);

            if (mergedArray == null)
            {
                _logger.LogInformation("All Finance news articles already exist in database. Skipped saving.");
                return;
            }

            var mergedJson = mergedArray.ToJsonString(jsonOptions);

            if (existingRecord != null)
            {
                // UPDATE the existing single record in place
                existingRecord.JsonData = mergedJson;
                existingRecord.ArticleCount = mergedArray.Count;
                existingRecord.CreatedAt = DateTime.UtcNow;

                // Delete any extra legacy rows (keep only this one)
                var extraRows = await _dbContext.FinanceNewsArticles
                    .Where(n => n.Id != existingRecord.Id)
                    .ExecuteDeleteAsync(ct);

                if (extraRows > 0)
                {
                    _logger.LogInformation("Cleaned up {Count} legacy Finance news rows, consolidated into 1 record", extraRows);
                }
            }
            else
            {
                // INSERT the first record ever
                var newRecord = new FinanceNewsArticle
                {
                    JsonData = mergedJson,
                    ArticleCount = mergedArray.Count,
                    CreatedAt = DateTime.UtcNow
                };
                await _dbContext.FinanceNewsArticles.AddAsync(newRecord, ct);
            }

            await _dbContext.SaveChangesAsync(ct);
            _logger.LogInformation("Saved Finance news — 1 record with {Count} total articles", mergedArray.Count);
        }

        /// <summary>
        /// Upserts Today's news articles into exactly 1 database record.
        /// Reads the existing single record, merges new non-duplicate articles, and updates in place.
        /// </summary>
        private async Task UpsertTodayNewsAsync(List<JsonObject> processedArticles, JsonSerializerOptions jsonOptions, CancellationToken ct)
        {
            // Get the single existing record
            var existingRecord = await _dbContext.TodayNewsArticles
                .OrderByDescending(n => n.CreatedAt)
                .FirstOrDefaultAsync(ct);

            // Build merged JSON array
            var mergedArray = MergeArticles(existingRecord?.JsonData, processedArticles);

            if (mergedArray == null)
            {
                _logger.LogInformation("All Today's news articles already exist in database. Skipped saving.");
                return;
            }

            var mergedJson = mergedArray.ToJsonString(jsonOptions);

            if (existingRecord != null)
            {
                // UPDATE the existing single record in place
                existingRecord.JsonData = mergedJson;
                existingRecord.ArticleCount = mergedArray.Count;
                existingRecord.CreatedAt = DateTime.UtcNow;

                // Delete any extra legacy rows
                var extraRows = await _dbContext.TodayNewsArticles
                    .Where(n => n.Id != existingRecord.Id)
                    .ExecuteDeleteAsync(ct);

                if (extraRows > 0)
                {
                    _logger.LogInformation("Cleaned up {Count} legacy Today's news rows, consolidated into 1 record", extraRows);
                }
            }
            else
            {
                // INSERT the first record ever
                var newRecord = new TodayNewsArticle
                {
                    JsonData = mergedJson,
                    ArticleCount = mergedArray.Count,
                    CreatedAt = DateTime.UtcNow
                };
                await _dbContext.TodayNewsArticles.AddAsync(newRecord, ct);
            }

            await _dbContext.SaveChangesAsync(ct);
            _logger.LogInformation("Saved Today's news — 1 record with {Count} total articles", mergedArray.Count);
        }

        /// <summary>
        /// Merges new articles into an existing JSON array string, deduplicating by URL.
        /// Returns the merged JsonArray, or null if no new articles were added.
        /// </summary>
        private JsonArray? MergeArticles(string? existingJsonData, List<JsonObject> newArticles)
        {
            var existingUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var mergedArray = new JsonArray();

            // Parse existing articles from the single record (if any)
            if (!string.IsNullOrWhiteSpace(existingJsonData))
            {
                try
                {
                    using var doc = JsonDocument.Parse(existingJsonData);
                    if (doc.RootElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in doc.RootElement.EnumerateArray())
                        {
                            // Track existing URLs for dedup
                            if (item.TryGetProperty("url", out var urlEl))
                            {
                                var url = urlEl.GetString();
                                if (!string.IsNullOrEmpty(url))
                                    existingUrls.Add(url);
                            }

                            // Re-add existing article to merged array
                            var cloned = JsonNode.Parse(item.GetRawText());
                            if (cloned != null)
                                mergedArray.Add(cloned);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse existing JSON data, starting fresh");
                }
            }

            // Add only new (non-duplicate) articles
            int addedCount = 0;
            foreach (var article in newArticles)
            {
                var url = article["url"]?.GetValue<string>();
                if (!string.IsNullOrEmpty(url) && existingUrls.Contains(url))
                {
                    continue; // Already exists, skip
                }

                // Clone the article node to avoid parent-detach issues
                var cloned = JsonNode.Parse(article.ToJsonString());
                if (cloned != null)
                {
                    mergedArray.Add(cloned);
                    addedCount++;
                }
            }

            if (addedCount == 0)
            {
                return null; // No new articles to add
            }

            _logger.LogInformation("Merged {NewCount} new articles with {ExistingCount} existing (total: {Total})",
                addedCount, existingUrls.Count, mergedArray.Count);

            return mergedArray;
        }
    }
}
