using FinancialApplication.Application.DTOs;
using FinancialApplication.Application.Interfaces;
using FinancialApplication.Domain.Domain.Entity;
using FinancialApplication.Infrastructure.Data;
using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FinancialApplication.Infrastructure.Services
{
    /// <summary>
    /// Fetches banner images from external URLs via web scraping, then:
    ///   1. Downloads the actual image bytes from the scraped image URL
    ///   2. Compresses the image (resize + JPEG quality reduction) via IImageCompressionService
    ///   3. Saves the compressed bytes to the Banners table in the database
    /// 
    /// If an image has already been downloaded (matching OriginalUrl in DB), it returns
    /// the existing record instead of re-downloading.
    /// </summary>
    public class BannerFetchService : IBannerFetchService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IImageCompressionService _imageCompressionService;
        private readonly AppDbContext _dbContext;
        private readonly ILogger<BannerFetchService> _logger;

        public BannerFetchService(
            IHttpClientFactory httpClientFactory,
            IImageCompressionService imageCompressionService,
            AppDbContext dbContext,
            ILogger<BannerFetchService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _imageCompressionService = imageCompressionService;
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<List<BannerResponseDto>> FetchBannersAsync(List<string> urls)
        {
            var client = _httpClientFactory.CreateClient("BannerFetcher");
            var tasks = urls.Select(url => FetchAndCompressBannerAsync(client, url));
            var results = await Task.WhenAll(tasks);
            return results.ToList();
        }

        private async Task<BannerResponseDto> FetchAndCompressBannerAsync(HttpClient client, string url)
        {
            try
            {
                // ── Step 1: Scrape the page for an image URL ────────────────────
                var html = await client.GetStringAsync(url);
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                var ogImage = doc.DocumentNode.SelectSingleNode("//meta[@property='og:image']/@content");
                var twitterImage = doc.DocumentNode.SelectSingleNode("//meta[@name='twitter:image']/@content");
                var firstImg = doc.DocumentNode.SelectSingleNode("//article//img/@src") ??
                               doc.DocumentNode.SelectSingleNode("//main//img/@src") ??
                               doc.DocumentNode.SelectSingleNode("//img[@class='banner' or contains(@class, 'hero') or contains(@class, 'featured')]/@src");

                var imageUrl = ogImage?.GetAttributeValue("content", null)
                            ?? twitterImage?.GetAttributeValue("content", null)
                            ?? firstImg?.GetAttributeValue("src", null);

                var ogTitle = doc.DocumentNode.SelectSingleNode("//meta[@property='og:title']/@content");
                var ogDesc = doc.DocumentNode.SelectSingleNode("//meta[@property='og:description']/@content");

                var title = ogTitle?.GetAttributeValue("content", null);
                var description = ogDesc?.GetAttributeValue("content", null);

                if (string.IsNullOrWhiteSpace(imageUrl))
                {
                    return new BannerResponseDto
                    {
                        Url = url,
                        Title = title,
                        Description = description,
                        Success = false,
                        Error = "No image found on page"
                    };
                }

                // Resolve relative URLs
                if (Uri.TryCreate(imageUrl, UriKind.Relative, out _))
                {
                    imageUrl = new Uri(new Uri(url), imageUrl).ToString();
                }

                // ── Step 2: Check if we already have this image in DB ──────────
                var existingBanner = await _dbContext.Banners
                    .FirstOrDefaultAsync(b => b.OriginalUrl == imageUrl);

                if (existingBanner != null)
                {
                    _logger.LogDebug("Banner image already exists in DB for {ImageUrl}, returning existing ID {BannerId}",
                        imageUrl, existingBanner.Id);

                    return new BannerResponseDto
                    {
                        Url = url,
                        ImageUrl = imageUrl,
                        Title = title,
                        Description = description,
                        Success = true,
                        BannerId = existingBanner.Id,
                        OriginalSizeBytes = existingBanner.OriginalSizeBytes,
                        CompressedSizeBytes = existingBanner.CompressedSizeBytes,
                        CompressionRatio = existingBanner.OriginalSizeBytes > 0
                            ? Math.Round((1.0 - (double)existingBanner.CompressedSizeBytes / existingBanner.OriginalSizeBytes) * 100, 1)
                            : 0
                    };
                }

                // ── Step 3: Download the actual image bytes ─────────────────────
                var imageBytes = await client.GetByteArrayAsync(imageUrl);
                var originalSize = imageBytes.Length;

                _logger.LogInformation("Downloaded banner image from {ImageUrl} ({OriginalSize} bytes)",
                    imageUrl, originalSize);

                // ── Step 4: Compress the image ──────────────────────────────────
                var compressedBytes = _imageCompressionService.Compress(imageBytes, maxWidth: 800, quality: 70);
                var compressedSize = compressedBytes.Length;

                _logger.LogInformation("Compressed banner image: {OriginalSize} → {CompressedSize} bytes ({Ratio}% reduction)",
                    originalSize, compressedSize,
                    Math.Round((1.0 - (double)compressedSize / originalSize) * 100, 1));

                // ── Step 5: Save to database ────────────────────────────────────
                var banner = new Banner
                {
                    CompressedImage = compressedBytes,
                    ContentType = "image/jpeg",
                    OriginalUrl = imageUrl,
                    SourcePageUrl = url,
                    Title = title,
                    Description = description,
                    OriginalSizeBytes = originalSize,
                    CompressedSizeBytes = compressedSize,
                    CreatedAt = DateTime.UtcNow
                };

                _dbContext.Banners.Add(banner);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Saved compressed banner to DB with ID {BannerId}", banner.Id);

                return new BannerResponseDto
                {
                    Url = url,
                    ImageUrl = imageUrl,
                    Title = title,
                    Description = description,
                    Success = true,
                    BannerId = banner.Id,
                    OriginalSizeBytes = originalSize,
                    CompressedSizeBytes = compressedSize,
                    CompressionRatio = originalSize > 0
                        ? Math.Round((1.0 - (double)compressedSize / originalSize) * 100, 1)
                        : 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch/compress banner from {Url}", url);

                return new BannerResponseDto
                {
                    Url = url,
                    Success = false,
                    Error = ex.Message
                };
            }
        }
    }
}
