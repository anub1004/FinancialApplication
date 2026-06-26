using System.Net.Mime;
using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace FinancialApplication.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FinanceNewsController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private const string ApiKey = "IYZGX3KEXVKKM26C";
        private const string BaseUrl = "https://www.alphavantage.co/query?function=NEWS_SENTIMENT&tickers=AAPL";

        public FinanceNewsController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public async Task<IActionResult> GetNews()
        {
            var client = _httpClientFactory.CreateClient();
            var url = $"{BaseUrl}&apikey={ApiKey}";
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");

            var response = await client.GetAsync(url);
            var json = await response.Content.ReadAsStringAsync();

            return Content(json, MediaTypeNames.Application.Json, Encoding.UTF8);
        }
    }
}
