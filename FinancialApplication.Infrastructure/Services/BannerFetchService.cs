using FinancialApplication.Application.DTOs;
using FinancialApplication.Application.Interfaces;
using HtmlAgilityPack;

namespace FinancialApplication.Infrastructure.Services
{
    public class BannerFetchService : IBannerFetchService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public BannerFetchService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<List<BannerResponseDto>> FetchBannersAsync(List<string> urls)
        {
            var client = _httpClientFactory.CreateClient("BannerFetcher");
            var tasks = urls.Select(url => FetchSingleBannerAsync(client, url));
            var results = await Task.WhenAll(tasks);
            return results.ToList();
        }

        private static async Task<BannerResponseDto> FetchSingleBannerAsync(HttpClient client, string url)
        {
            try
            {
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

                return new BannerResponseDto
                {
                    Url = url,
                    ImageUrl = imageUrl,
                    Title = ogTitle?.GetAttributeValue("content", null),
                    Description = ogDesc?.GetAttributeValue("content", null),
                    Success = imageUrl != null
                };
            }
            catch (Exception ex)
            {
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
