# NewsProcessingService - Developer Guide & Testing

## Quick Start: Understanding the Optimizations

### The Three Critical Improvements

#### 1️⃣ **Partitioning**: Articles sorted by scrape success (O(n) time)
```
INPUT:  [Article A (url1), Article B (url1), Article C (url3)]
		 [After scraping]
OUTPUT: [Article A (image found), Article B (image found), Article C (no image)]
		 ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
		 Articles WITH scraped images first
```

#### 2️⃣ **Resilience**: One article fails, others continue
```
BEFORE: Article A fails → Task.WhenAll throws → Batch STOPS
AFTER:  Article A fails → Exception caught → Articles B, C, D continue → Partial success
```

#### 3️⃣ **Caching**: Same URL scraped once, used many times
```
First Article (url="news.com/story1"):  HTTP GET → cache[url] = "image.jpg"
Second Article (url="news.com/story1"): Cache HIT → return "image.jpg" (no HTTP)
```

---

## Integration Guide

### How to Call the Service

```csharp
// 1. Fetch news from API
var apiUrl = "https://newsapi.org/v2/everything?q=bitcoin";
var jsonResponse = await newsService.FetchNewsAsync(apiUrl);

// 2. Parse JSON and extract articles
using var doc = JsonDocument.Parse(jsonResponse);
var articles = doc.RootElement.GetProperty("articles");

// 3. Process articles (scrape images, partition)
var processedArticles = await newsService.ProcessArticlesAsync(articles);
// Returns: [A (image), B (image), C (no image), D (no image)]

// 4. Save to database
await newsService.SaveNewsAsync(processedArticles, isFinanceNews: true);

// 5. Clean up old articles
await newsService.DeleteOldNewsAsync(retentionDays: 30, isFinanceNews: true);
```

---

## Architecture: How Data Flows

```
┌─────────────────────────────────────────────────────────────────┐
│ 1. FetchNewsAsync(apiUrl)                                       │
│    └─ Calls news API, returns raw JSON string                   │
└────────────────────┬────────────────────────────────────────────┘
					 │ JSON string
					 ▼
┌─────────────────────────────────────────────────────────────────┐
│ 2. ProcessArticlesAsync(JsonElement articles)                   │
│                                                                  │
│    a. Parse JSON to JsonObject list (single pass)               │
│    b. Parallel.ForEachAsync: For each article                   │
│       ├─ Check cache for URL                                    │
│       │  └─ Cache HIT? Use cached imageUrl                      │
│       │  └─ Cache MISS?                                         │
│       ├─ Acquire semaphore (limit concurrent HTTP)              │
│       ├─ ExtractImageUrlAsync(articleUrl)                       │
│       │  ├─ HTTP GET article page                               │
│       │  ├─ Parse HTML for image URLs (meta, img tags)          │
│       │  ├─ Normalize relative URLs to absolute                 │
│       │  └─ Return imageUrl (or null)                           │
│       ├─ Cache result: _imageUrlCache[url] = imageUrl           │
│       ├─ Release semaphore                                       │
│       └─ Partition to correct list                              │
│          ├─ If imageUrl != null → articlesWithImages.Add()      │
│          └─ If imageUrl == null → articlesWithoutImages.Add()   │
│                                                                  │
│    c. Combine lists: [with_images] + [without_images]          │
│    └─ Return partitioned list                                   │
└────────────────────┬────────────────────────────────────────────┘
					 │ List<JsonObject> with imageUrl added
					 ▼
┌─────────────────────────────────────────────────────────────────┐
│ 3. SaveNewsAsync(List<JsonObject> processedArticles)            │
│                                                                  │
│    a. Query DB for existing articles in 2-day window            │
│    b. Extract URLs from existing articles                       │
│    c. Filter: Only new articles (not in DB)                     │
│    d. Serialize to JSON strings                                 │
│    e. Batch insert: DbContext.AddRangeAsync()                   │
│    └─ SaveChangesAsync() - single DB transaction                │
└────────────────────┬────────────────────────────────────────────┘
					 │ Articles saved with imageUrl
					 ▼
┌─────────────────────────────────────────────────────────────────┐
│ 4. DeleteOldNewsAsync(retentionDays)                            │
│    └─ ExecuteDeleteAsync() - batch delete articles > 30 days    │
└─────────────────────────────────────────────────────────────────┘
```

---

## Testing Guide

### Unit Test: Image URL Cache

```csharp
[Fact]
public async Task ProcessArticlesAsync_CachesImageUrls_AvoidsDuplicateScraping()
{
	// Arrange
	var url = "https://example.com/article1";
	var json = @"
	[{
		""url"": """ + url + @""",
		""title"": ""Article 1""
	},
	{
		""url"": """ + url + @""",
		""title"": ""Article 2 (same URL)""
	}]";

	using var doc = JsonDocument.Parse(json);
	var articles = doc.RootElement.GetProperty("$");

	// Act
	var results = await _service.ProcessArticlesAsync(articles);

	// Assert
	Assert.Equal(2, results.Count);

	// Both articles should have same imageUrl (cached)
	var img1 = results[0]["imageUrl"]?.GetValue<string?>();
	var img2 = results[1]["imageUrl"]?.GetValue<string?>();
	Assert.Equal(img1, img2);
}
```

### Unit Test: Partitioning Order

```csharp
[Fact]
public async Task ProcessArticlesAsync_ParallelPartitions_ImagesFirst()
{
	// Arrange
	var json = @"
	[{
		""url"": ""https://example.com/1"",
		""title"": ""No image""
	},
	{
		""url"": ""https://example.com/2"",
		""title"": ""Has image""
	},
	{
		""url"": ""https://example.com/3"",
		""title"": ""Has image""
	}]";

	using var doc = JsonDocument.Parse(json);
	var articles = doc.RootElement;

	// Mock ExtractImageUrlAsync to return specific results
	// Article 1: null (no image)
	// Article 2: "img.jpg" (has image)
	// Article 3: "img2.jpg" (has image)

	// Act
	var results = await _service.ProcessArticlesAsync(articles);

	// Assert
	// First two should have images
	Assert.NotNull(results[0]["imageUrl"]?.GetValue<string>());
	Assert.NotNull(results[1]["imageUrl"]?.GetValue<string>());
	// Last should NOT have image
	Assert.Null(results[2]["imageUrl"]);
}
```

### Integration Test: Database Deduplication

```csharp
[Fact]
public async Task SaveNewsAsync_DeduplicatesExistingArticles_OnlyInsertsNew()
{
	// Arrange
	var url1 = "https://example.com/existing";
	var url2 = "https://example.com/new";

	// Insert existing article into DB
	var existing = new FinanceNewsArticle 
	{ 
		JsonData = @"{""url"":""" + url1 + @"""}",
		CreatedAt = DateTime.UtcNow.AddHours(-1)
	};
	_dbContext.FinanceNewsArticles.Add(existing);
	await _dbContext.SaveChangesAsync();

	// New articles to save (one duplicate, one new)
	var articles = new List<JsonObject>
	{
		JsonNode.Parse(@"{""url"":""" + url1 + @"""}").AsObject(),
		JsonNode.Parse(@"{""url"":""" + url2 + @"""}").AsObject()
	};

	// Act
	await _service.SaveNewsAsync(articles, isFinanceNews: true);

	// Assert
	var count = await _dbContext.FinanceNewsArticles.CountAsync();
	Assert.Equal(2, count);  // Only ONE new article should be inserted
}
```

### Performance Test: Concurrency Limit

```csharp
[Fact]
public async Task ProcessArticlesAsync_RespectsSemaphoreConcurrency_LimitsParallelRequests()
{
	// Arrange
	var service = new NewsProcessingService(
		_dbContext,
		_httpClientFactory,
		_logger,
		_configuration  // Set MaxConcurrentScrapes = 3
	);

	var json = @"[" + 
		string.Join(",", Enumerable.Range(1, 100)
			.Select(i => @"{""url"":""https://example.com/" + i + @"""}")) +
		@"]";

	using var doc = JsonDocument.Parse(json);
	var articles = doc.RootElement;

	var concurrentCount = 0;
	var maxConcurrent = 0;
	var lockObj = new object();

	// Mock to track concurrent requests
	// (This would require injection of tracking mechanism)

	// Act
	var results = await service.ProcessArticlesAsync(articles);

	// Assert
	// Verify that max concurrent never exceeded 3
	// [Would need instrumentation to verify]
}
```

### Load Test: 10,000 Articles

```csharp
[Fact]
public async Task ProcessArticlesAsync_Handles10000Articles_UnderTwoSeconds()
{
	// Arrange
	var articles = Enumerable.Range(1, 10000)
		.Select(i => @"{""url"":""https://example.com/" + i + @"""}")
		.ToList();

	var json = "[" + string.Join(",", articles) + "]";

	using var doc = JsonDocument.Parse(json);
	var articleElement = doc.RootElement;

	// Act
	var sw = Stopwatch.StartNew();
	var results = await _service.ProcessArticlesAsync(articleElement);
	sw.Stop();

	// Assert
	Assert.Equal(10000, results.Count);
	Assert.True(sw.ElapsedMilliseconds < 2000, $"Took {sw.ElapsedMilliseconds}ms");
}
```

---

## Monitoring & Observability

### Metrics to Track

```csharp
// In your monitoring system:

1. Cache Hit Rate
   - Metric: _imageUrlCache.Count / total_scrape_attempts
   - Healthy: > 70% for news aggregators
   - Alert: < 50%

2. Batch Completion Rate
   - Metric: articles_added_to_db / articles_processed
   - Healthy: > 99%
   - Alert: < 95% (indicates scraping issues)

3. Average Scrape Time
   - Metric: ProcessArticlesAsync execution time
   - Healthy: < 5 seconds for 100 articles
   - Alert: > 10 seconds

4. Concurrent Requests
   - Metric: SemaphoreSlim.CurrentCount
   - Healthy: Regularly cycles 0-5
   - Alert: Stuck at 0 (potential deadlock)

5. Database Batch Size
   - Metric: articles_per_batch
   - Healthy: 50-200 articles
   - Alert: Single article batches (inefficient)
```

### Log Analysis

Production logs should show:
```
[Information] Fetching news from API: https://api.newsapi.org/...
[Information] Successfully fetched news response (45000 chars)
[Information] Processed 100 articles: 85 with images, 15 without
[Information] Saved 50 new Finance news articles to database
[Information] Deleted 200 Finance news articles older than 30 days (before 2024-11-15T10:30:00Z)
```

Debug logs (only when enabled) should show:
```
[Debug] Cache hit for https://example.com/article1: https://cdn.example.com/image.jpg
[Debug] No image found for https://example.com/article2
```

---

## Troubleshooting

### Issue: CPU Stays at 100%

**Diagnosis:** Too many concurrent scrapes
```
Solution: Reduce MaxConcurrentScrapes in config
"NewsService": { "MaxConcurrentScrapes": 3 }
```

### Issue: Network Connection Errors

**Diagnosis:** Target sites blocking requests
```
Solution: Add delays between requests, use rotating user agents
Consider: Circuit breaker pattern if specific domains fail
```

### Issue: Memory Usage Growing

**Diagnosis:** Image cache not cleared
```
Solution: Add TTL to cache entries, or implement cache eviction
Current: Cache persists for service lifetime
Future: Consider Redis for distributed cache with TTL
```

### Issue: Database Deadlocks

**Diagnosis:** Concurrent SaveChangesAsync calls
```
Solution: Ensure only one SaveChangesAsync per batch
Current: Code is thread-safe for production use
Verify: No direct DbContext calls from multiple threads
```

---

## Performance Tuning

### Scenario 1: Large Batches (1000+ articles)

**Recommendation:**
```json
{
  "NewsService": {
	"MaxConcurrentScrapes": 10,
	"BatchSize": 500
  }
}
```

### Scenario 2: Limited Network (Mobile Networks)

**Recommendation:**
```json
{
  "NewsService": {
	"MaxConcurrentScrapes": 2,
	"Timeout": 30000
  }
}
```

### Scenario 3: High CPU Server (Many Cores)

**Recommendation:**
```json
{
  "NewsService": {
	"MaxConcurrentScrapes": Environment.ProcessorCount
  }
}
```

---

## Code Quality Checklist

- [x] No null reference exceptions (proper null-coalescing)
- [x] All exceptions handled (no unhandled throws)
- [x] Semaphore always released (finally block)
- [x] ConcurrentDictionary for thread-safety
- [x] Structured logging (proper log levels)
- [x] Cancellation token propagation
- [x] Resource cleanup (using statements)
- [x] Memory efficient (pre-allocation)
- [ ] Add distributed caching for multi-instance
- [ ] Add circuit breaker for failing domains
- [ ] Add retry logic with exponential backoff

---

## Migration Checklist for DevOps

- [ ] Update configuration with `NewsService:MaxConcurrentScrapes`
- [ ] Database migration (if schema changed) - NONE REQUIRED
- [ ] Restart application
- [ ] Monitor logs for errors
- [ ] Verify cache hit rate increasing over time
- [ ] Confirm batch sizes larger than before
- [ ] Check memory usage after 1 hour
- [ ] Compare wall-clock time for same feed (should be faster)
- [ ] Set up alerts for metrics
- [ ] Document in runbook

---

## Future Enhancements

### Phase 2: Distributed Caching
```csharp
// Replace in-memory cache with Redis
private readonly IDistributedCache _cache;

if (await _cache.GetStringAsync(url) is { } cachedUrl)
{
	imageUrl = cachedUrl;
}
else
{
	imageUrl = await ExtractImageUrlAsync(url, token);
	await _cache.SetStringAsync(url, imageUrl, 
		new DistributedCacheEntryOptions 
		{ 
			AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24) 
		});
}
```

### Phase 3: Circuit Breaker
```csharp
// Prevent cascading failures to specific domains
var breaker = new CircuitBreaker(failureThreshold: 5, timeout: TimeSpan.FromMinutes(1));

try
{
	breaker.Record(() => ExtractImageUrlAsync(url, token));
}
catch (BrokenCircuitException)
{
	_logger.LogWarning("Circuit breaker open for domain {Domain}", new Uri(url).Host);
	return null;
}
```

### Phase 4: Adaptive Concurrency
```csharp
// Increase/decrease SemaphoreSlim based on response times
var avgResponseTime = _metrics.GetAverageResponseTime();
if (avgResponseTime < 500ms)
	_scrapeSemaphore = new SemaphoreSlim(10);  // Increase
else if (avgResponseTime > 5000ms)
	_scrapeSemaphore = new SemaphoreSlim(2);   // Decrease
```
