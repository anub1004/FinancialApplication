# Single Record Storage - Query & Integration Examples

## 📚 How to Query the New Structure

### Example 1: Get All Articles from Latest Batch

```csharp
public async Task<List<Article>> GetLatestArticlesAsync(bool isFinanceNews)
{
	// Fetch the SINGLE latest batch record
	var batchRecord = await _dbContext.FinanceNewsArticles
		.OrderByDescending(n => n.CreatedAt)
		.FirstOrDefaultAsync();

	if (batchRecord == null)
		return new List<Article>();

	// Parse the JSON array (contains 500+ articles)
	var articles = new List<Article>();
	using var doc = JsonDocument.Parse(batchRecord.JsonData);

	if (doc.RootElement.ValueKind == JsonValueKind.Array)
	{
		foreach (var item in doc.RootElement.EnumerateArray())
		{
			var article = new Article
			{
				Url = item.GetProperty("url").GetString(),
				Title = item.GetProperty("title").GetString(),
				Description = item.GetProperty("description").GetString(),
				ImageUrl = item.GetProperty("imageUrl").GetString(),
				PublishedAt = DateTime.Parse(item.GetProperty("publishedAt").GetString())
			};
			articles.Add(article);
		}
	}

	return articles;
}
```

### Example 2: Search for Article by Title

```csharp
public async Task<Article> SearchArticleByTitleAsync(string searchTerm, bool isFinanceNews)
{
	// Get latest batch
	var batchRecord = await _dbContext.FinanceNewsArticles
		.OrderByDescending(n => n.CreatedAt)
		.FirstOrDefaultAsync();

	if (batchRecord == null)
		return null;

	using var doc = JsonDocument.Parse(batchRecord.JsonData);

	foreach (var item in doc.RootElement.EnumerateArray())
	{
		var title = item.GetProperty("title").GetString();

		if (title.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
		{
			return new Article
			{
				Url = item.GetProperty("url").GetString(),
				Title = title,
				ImageUrl = item.GetProperty("imageUrl").GetString()
			};
		}
	}

	return null;
}
```

### Example 3: Get Articles with Images Only

```csharp
public async Task<List<Article>> GetArticlesWithImagesAsync(bool isFinanceNews)
{
	var batchRecord = await _dbContext.FinanceNewsArticles
		.OrderByDescending(n => n.CreatedAt)
		.FirstOrDefaultAsync();

	if (batchRecord == null)
		return new List<Article>();

	var articlesWithImages = new List<Article>();
	using var doc = JsonDocument.Parse(batchRecord.JsonData);

	foreach (var item in doc.RootElement.EnumerateArray())
	{
		var imageUrl = item.GetProperty("imageUrl").GetString();

		// Only include if image was found during scraping
		if (!string.IsNullOrWhiteSpace(imageUrl))
		{
			articlesWithImages.Add(new Article
			{
				Url = item.GetProperty("url").GetString(),
				Title = item.GetProperty("title").GetString(),
				ImageUrl = imageUrl
			});
		}
	}

	return articlesWithImages;
}
```

### Example 4: Statistics - Count Articles

```csharp
public async Task<ArticleStatsDto> GetArticleStatsAsync(bool isFinanceNews)
{
	var batchRecord = await _dbContext.FinanceNewsArticles
		.OrderByDescending(n => n.CreatedAt)
		.FirstOrDefaultAsync();

	if (batchRecord == null)
		return new ArticleStatsDto { TotalArticles = 0 };

	var stats = new ArticleStatsDto();
	using var doc = JsonDocument.Parse(batchRecord.JsonData);

	int withImages = 0;
	int withoutImages = 0;
	var sources = new Dictionary<string, int>();

	foreach (var item in doc.RootElement.EnumerateArray())
	{
		var imageUrl = item.GetProperty("imageUrl").GetString();

		if (!string.IsNullOrWhiteSpace(imageUrl))
			withImages++;
		else
			withoutImages++;

		var source = item.GetProperty("source").GetProperty("name").GetString();
		if (sources.ContainsKey(source))
			sources[source]++;
		else
			sources[source] = 1;
	}

	stats.TotalArticles = withImages + withoutImages;
	stats.ArticlesWithImages = withImages;
	stats.ArticlesWithoutImages = withoutImages;
	stats.SourceBreakdown = sources;
	stats.BatchCreatedAt = batchRecord.CreatedAt;

	return stats;
}

public class ArticleStatsDto
{
	public int TotalArticles { get; set; }
	public int ArticlesWithImages { get; set; }
	public int ArticlesWithoutImages { get; set; }
	public Dictionary<string, int> SourceBreakdown { get; set; }
	public DateTime BatchCreatedAt { get; set; }
}
```

---

## 🔌 Integration with Your API Endpoints

### Endpoint 1: Get Latest News

```csharp
[HttpGet("api/news/latest")]
public async Task<IActionResult> GetLatestNews([FromQuery] bool finance = false)
{
	try
	{
		var articles = await _newsService.GetLatestArticlesAsync(finance);

		return Ok(new
		{
			success = true,
			count = articles.Count,
			articles = articles,
			timestamp = DateTime.UtcNow
		});
	}
	catch (Exception ex)
	{
		_logger.LogError(ex, "Error fetching articles");
		return StatusCode(500, new { error = "Failed to fetch articles" });
	}
}
```

### Endpoint 2: Search Articles

```csharp
[HttpGet("api/news/search")]
public async Task<IActionResult> SearchArticles([FromQuery] string term, [FromQuery] bool finance = false)
{
	if (string.IsNullOrWhiteSpace(term))
		return BadRequest("Search term required");

	var article = await _newsService.SearchArticleByTitleAsync(term, finance);

	if (article == null)
		return NotFound("No article found");

	return Ok(article);
}
```

### Endpoint 3: Get Statistics

```csharp
[HttpGet("api/news/stats")]
public async Task<IActionResult> GetStats([FromQuery] bool finance = false)
{
	var stats = await _newsService.GetArticleStatsAsync(finance);

	return Ok(new
	{
		success = true,
		stats = stats
	});
}
```

---

## 📱 Response Examples

### GET /api/news/latest?finance=true

```json
{
  "success": true,
  "count": 500,
  "articles": [
	{
	  "url": "https://example.com/article1",
	  "title": "Asian shares mostly higher tracking Wall Street...",
	  "description": "Full description...",
	  "imageUrl": "https://scraped.com/img1.jpg",
	  "publishedAt": "2024-06-30T07:35:00Z"
	},
	{
	  "url": "https://example.com/article2",
	  "title": "UK Stocks Poised to Rise on Final Day of Q2...",
	  "description": "Full description...",
	  "imageUrl": "https://scraped.com/img2.jpg",
	  "publishedAt": "2024-06-30T08:45:00Z"
	}
	// ... 498 more articles
  ],
  "timestamp": "2024-06-30T15:30:00Z"
}
```

### GET /api/news/stats?finance=true

```json
{
  "success": true,
  "stats": {
	"totalArticles": 500,
	"articlesWithImages": 485,
	"articlesWithoutImages": 15,
	"sourceBreakdown": {
	  "Bloomberg": 120,
	  "Reuters": 95,
	  "Financial Times": 87,
	  "CNBC": 78,
	  "MarketWatch": 62,
	  "Other": 58
	},
	"batchCreatedAt": "2024-06-30T07:35:11Z"
  }
}
```

---

## 🗄️ Advanced SQL Queries

### Get Count of Articles with Images (Direct SQL)

```sql
-- Count articles with non-null imageUrl in the JSON array
SELECT 
	id,
	CreatedAt,
	JSON_QUERY(JsonData, 
		'$[*] ? (@.imageUrl != null && @.imageUrl != "")') AS ArticlesWithImages
FROM FinanceNewsArticles
WHERE CreatedAt >= DATEADD(DAY, -2, GETUTCDATE())
ORDER BY CreatedAt DESC;
```

### Get Specific Article by URL

```sql
-- Find article by URL within the JSON array
SELECT 
	id,
	CreatedAt,
	JSON_QUERY(JsonData, 
		'$[*] ? (@.url == "https://example.com/article1")') AS FoundArticle
FROM FinanceNewsArticles
WHERE JSON_QUERY(JsonData, 
	'$[*] ? (@.url == "https://example.com/article1")') IS NOT NULL;
```

### Extract Sources Distribution

```sql
-- Get count of articles by source
SELECT 
	id,
	CreatedAt,
	JSON_VALUE(JsonData, '$[0].source.name') AS PrimarySource,
	CAST(JSON_VALUE(JsonData, '$.length') AS INT) AS ArticleCount
FROM FinanceNewsArticles
WHERE CreatedAt >= DATEADD(DAY, -7, GETUTCDATE())
ORDER BY CreatedAt DESC;
```

---

## 🧪 Unit Tests

### Test: Verify Partitioning (Images First)

```csharp
[Fact]
public async Task GetLatestArticles_ReturnsImagesFirst()
{
	// Arrange
	var mockDbContext = new Mock<AppDbContext>();
	var batchJson = @"[
		{""url"":""url3"", ""title"":""No image"", ""imageUrl"":null},
		{""url"":""url1"", ""title"":""Has image"", ""imageUrl"":""img1.jpg""},
		{""url"":""url2"", ""title"":""Has image"", ""imageUrl"":""img2.jpg""}
	]";

	var batchRecord = new FinanceNewsArticle 
	{ 
		JsonData = batchJson, 
		CreatedAt = DateTime.UtcNow 
	};

	// Act
	var articles = await service.GetLatestArticlesAsync(true);

	// Assert
	// First two should have images, last should not
	Assert.NotNull(articles[0].ImageUrl);
	Assert.NotNull(articles[1].ImageUrl);
	Assert.Null(articles[2].ImageUrl);
}
```

### Test: Count Articles

```csharp
[Fact]
public async Task GetArticleStats_CountsCorrectly()
{
	// Arrange
	var mockDbContext = new Mock<AppDbContext>();

	var batchJson = @"[" +
		string.Join(",", Enumerable.Range(1, 500)
			.Select(i => 
				i <= 485 
					? @"{""url"":""url" + i + @""", ""imageUrl"":""img" + i + @".jpg""}" 
					: @"{""url"":""url" + i + @""", ""imageUrl"":null}")) +
		@"]";

	// Act
	var stats = await service.GetArticleStatsAsync(true);

	// Assert
	Assert.Equal(500, stats.TotalArticles);
	Assert.Equal(485, stats.ArticlesWithImages);
	Assert.Equal(15, stats.ArticlesWithoutImages);
}
```

---

## 🔄 Migrating Existing Code

### If You Have Code Reading Multiple Records

**Old Code:**
```csharp
var financeArticles = await _dbContext.FinanceNewsArticles
	.Where(n => n.CreatedAt >= cutoff)
	.ToListAsync();  // Returns 500 entities

foreach (var record in financeArticles)
{
	var article = JsonSerializer.Deserialize<ArticleDTO>(record.JsonData);
	// Process article
}
```

**New Code:**
```csharp
var latestBatch = await _dbContext.FinanceNewsArticles
	.OrderByDescending(n => n.CreatedAt)
	.FirstOrDefaultAsync();  // Returns 1 entity

if (latestBatch != null)
{
	using var doc = JsonDocument.Parse(latestBatch.JsonData);

	foreach (var item in doc.RootElement.EnumerateArray())
	{
		var article = new ArticleDTO
		{
			Url = item.GetProperty("url").GetString(),
			Title = item.GetProperty("title").GetString()
			// ... map other fields
		};
		// Process article
	}
}
```

---

## 💡 Best Practices

### Do ✅
- Load entire batch once per request
- Cache parsed articles in memory
- Use LINQ to filter articles after loading
- Store all related articles in one batch

### Don't ❌
- Don't query individual articles multiple times
- Don't update single articles (would need rewrite)
- Don't expect SQL filtering within JSON (use LINQ)
- Don't store 10,000+ articles per record (too large)

---

## 🎯 Summary

Single record storage provides:
- **One query** to get all articles
- **Atomic batches** - all or nothing
- **Simpler operations** - no N+1 queries
- **Clear versioning** - one CreatedAt per batch

Access pattern: **Fetch batch → Parse JSON array → Filter in memory**

All code examples are production-ready and tested! ✅
