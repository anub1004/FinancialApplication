using System.Text.Json;
using FinancialApplication.Application.Interfaces;
using FinancialApplication.Infrastructure.Data;
using FinancialApplication.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace NewsDataUpdateService
{
    public class Program
    {
        static async Task<int> Main(string[] args)
        {
            // Bootstrap Serilog early so startup errors are also logged to file
            var bootstrapConfig = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(bootstrapConfig)
                .Enrich.FromLogContext()
                .CreateLogger();

            try
            {
                Log.Information("══════════════════════════════════════════");
                Log.Information("  News Data Update Service Starting...");
                Log.Information("══════════════════════════════════════════");

                var host = Host.CreateDefaultBuilder(args)
                    .UseSerilog() // Use Serilog for all Microsoft.Extensions.Logging calls
                    .ConfigureServices((context, services) =>
                    {
                        var connectionString = context.Configuration.GetConnectionString("DefaultConnection")
                            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

                        Log.Information("Connecting to database: {Server}", connectionString.Split(';')[0]);

                        services.AddDbContext<AppDbContext>(options =>
                            options.UseSqlServer(connectionString));

                        services.AddHttpClient("NewsScraper", client =>
                        {
                            client.Timeout = TimeSpan.FromSeconds(5);
                            client.DefaultRequestHeaders.Add("User-Agent",
                                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
                        });

                        services.AddSingleton<IImageCompressionService, ImageCompressionService>();
                        services.AddScoped<INewsProcessingService, NewsProcessingService>();
                    })
                    .Build();

                using var scope = host.Services.CreateScope();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                var newsService = scope.ServiceProvider.GetRequiredService<INewsProcessingService>();
                var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

                logger.LogInformation("──── Starting News Data Update Job ────");

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

                            logger.LogInformation("Processing and scraping articles...");
                            var processed = await newsService.ProcessArticlesAsync(articles);

                            logger.LogInformation("Saving articles to the database...");
                            await newsService.SaveNewsAsync(processed, target.IsFinance);
                            totalSaved += processed.Count;

                            var total = count;
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
                            hasMore = false;
                        }
                    }

                    logger.LogInformation("Completed update cycle for {ApiName}. Total saved this run: {Total} articles.", target.Name, totalSaved);
                }

                logger.LogInformation("──── News Data Update Job Complete. Refreshing API cache... ────");

                // Ping the API to bust the in-memory cache so frontend sees new data immediately
                var apiBaseUrl = configuration.GetValue<string>("App:ApiBaseUrl");
                if (!string.IsNullOrEmpty(apiBaseUrl))
                {
                    using var httpClient = new System.Net.Http.HttpClient();
                    httpClient.Timeout = TimeSpan.FromSeconds(15);

                    var endpoints = new[]
                    {
                        $"{apiBaseUrl}/api/TodayNews/refresh",
                        $"{apiBaseUrl}/api/FinanceNews/refresh"
                    };

                    foreach (var endpoint in endpoints)
                    {
                        try
                        {
                            var response = await httpClient.PostAsync(endpoint, null);
                            logger.LogInformation("Cache refresh called: {Endpoint} → {Status}", endpoint, response.StatusCode);
                        }
                        catch (Exception ex)
                        {
                            // Non-fatal — cache will refresh on next automatic cycle
                            logger.LogWarning(ex, "Could not call cache refresh endpoint: {Endpoint}", endpoint);
                        }
                    }
                }

                logger.LogInformation("──── All done. Process exiting. ────");
                Log.CloseAndFlush();
                return 0; // Success exit code — Task Scheduler marks task as completed

            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "An unhandled fatal error terminated the news update job.");
                Log.CloseAndFlush();
                return 1; // Non-zero exit code — Task Scheduler marks task as failed
            }
        }
    }
}
