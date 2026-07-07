using System.Text.Json;
using System.Text.Json.Nodes;

namespace FinancialApplication.Application.Interfaces
{
    /// <summary>
    /// Defines the contract for fetching, processing, and storing news articles.
    /// </summary>
    public interface INewsProcessingService
    {
        /// <summary>
        /// Fetches the raw JSON response from the specified news API URL.
        /// </summary>
        Task<string> FetchNewsAsync(string apiUrl, CancellationToken ct = default);

        /// <summary>
        /// Processes the full set of articles concurrently by scraping each URL for an image.
        /// </summary>
        Task<List<JsonObject>> ProcessArticlesAsync(JsonElement articles, CancellationToken ct = default);

        /// <summary>
        /// Scrapes the given URL and extracts the main image URL from meta tags.
        /// Returns null if scraping fails.
        /// </summary>
        Task<string?> ExtractImageUrlAsync(string articleUrl, CancellationToken ct = default);

        /// <summary>
        /// Deletes all news records older than the specified number of days from the selected feed table.
        /// </summary>
        Task DeleteOldNewsAsync(int retentionDays, bool isFinanceNews, CancellationToken ct = default);

        /// <summary>
        /// Saves the processed articles (with imageUrl) to the selected feed table.
        /// </summary>
        Task SaveNewsAsync(List<JsonObject> processedArticles, bool isFinanceNews, CancellationToken ct = default);
    }
}
