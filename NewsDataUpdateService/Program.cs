using System.Text.Json;
using FinancialApplication.Application.Interfaces;
using FinancialApplication.Infrastructure.Data;
using FinancialApplication.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace NewsDataUpdateService
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    var connectionString = context.Configuration.GetConnectionString("DefaultConnection")
                        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");
                    
                    services.AddDbContext<AppDbContext>(options =>
                        options.UseSqlServer(connectionString));

                    services.AddHttpClient("NewsScraper", client =>
                    {
                        client.Timeout = TimeSpan.FromSeconds(5);
                        client.DefaultRequestHeaders.Add("User-Agent",
                            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
                    });

                    services.AddScoped<INewsProcessingService, NewsProcessingService>();
                })
                .Build();

            using var scope = host.Services.CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            var newsService = scope.ServiceProvider.GetRequiredService<INewsProcessingService>();
            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            logger.LogInformation("──── Starting News Data Update Job ────");

            try
            {
                var financeApiUrl = configuration.GetValue<string>("NewsService:ApiUrl")
                    ?? throw new InvalidOperationException("NewsService:ApiUrl is not configured.");
                var todayApiUrl = configuration.GetValue<string>("NewsConfigApi2:ApiUrl")
                    ?? throw new InvalidOperationException("NewsConfigApi2:ApiUrl is not configured.");

                var retentionDays = configuration.GetValue("NewsService:RetentionDays", 7);

                var apiUrls = new[]
                {
                    new { Name = "Finance News (Stocks)", Url = financeApiUrl, IsFinance = true },
                    new { Name = "Today's News (General)", Url = todayApiUrl, IsFinance = false }
                };

                foreach (var target in apiUrls)
                {
                    logger.LogInformation("──── Starting update cycle for {ApiName} ────", target.Name);
                    
                    // 1. Delete stale articles for this feed type
                    logger.LogInformation("Deleting articles older than {Days} days from this feed", retentionDays);
                    await newsService.DeleteOldNewsAsync(retentionDays, target.IsFinance);

                    var limit = 100;
                    var offset = 0;
                    var totalSaved = 0;
                    var hasMore = true;

                    while (hasMore)
                    {
                        try
                        {
                            var paginatedUrl = $"{target.Url}&limit={limit}&offset={offset}";
                            logger.LogInformation("Fetching news from API (Offset: {Offset}, Limit: {Limit})", offset, limit);
                            var json = await newsService.FetchNewsAsync(paginatedUrl);

                            using var doc = JsonDocument.Parse(json);
                            if (!doc.RootElement.TryGetProperty("data", out var articles)
                                || articles.ValueKind != JsonValueKind.Array
                                || articles.GetArrayLength() == 0)
                            {
                                logger.LogWarning("No articles found in API response for {ApiName} at offset {Offset}", target.Name, offset);
                                break;
                            }

                            var count = articles.GetArrayLength();
                            logger.LogInformation("Received {Count} articles from {ApiName} (Offset: {Offset})", count, target.Name, offset);

                            // Process the articles concurrently (without batching)
                            logger.LogInformation("Processing and scraping articles...");
                            var processed = await newsService.ProcessArticlesAsync(articles);

                            // Save the articles directly to the database
                            logger.LogInformation("Saving articles to the database...");
                            await newsService.SaveNewsAsync(processed, target.IsFinance);
                            totalSaved += processed.Count;

                            // Read pagination details to decide if we should fetch next page
                            var total = count; // Default fallback
                            if (doc.RootElement.TryGetProperty("pagination", out var paginationEl) &&
                                paginationEl.TryGetProperty("total", out var totalEl))
                            {
                                total = totalEl.GetInt32();
                            }

                            logger.LogInformation("Progress: Saved {Saved} / {Total} total articles for {ApiName}", totalSaved, total, target.Name);

                            offset += limit;
                            if (offset >= total || count < limit)
                            {
                                hasMore = false;
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Error occurred in update loop for {ApiName} at offset {Offset}.", target.Name, offset);
                            hasMore = false; // Stop this feed on connection/request error
                        }
                    }

                    logger.LogInformation("Completed update cycle for {ApiName}. Total saved this run: {Total} articles.", target.Name, totalSaved);
                }

                logger.LogInformation("──── News Data Update Job Complete ────");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An unhandled error occurred during the news update job initialization.");
            }
        }
    }
}
