using FinancialApplication.Application.DTOs;
using FinancialApplication.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinancialApplication.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BlogController : ControllerBase
    {
        private readonly IBannerFetchService _bannerFetchService;

        public BlogController(IBannerFetchService bannerFetchService)
        {
            _bannerFetchService = bannerFetchService;
        }

        [Authorize]
        [HttpPost("fetch-banners")]
        [ProducesResponseType(typeof(List<BannerResponseDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<BannerResponseDto>>> FetchBanners([FromBody] BannerRequestDto request)
        {
            if (request.Urls == null || request.Urls.Count == 0)
                return BadRequest("URL list cannot be empty");

            if (request.Urls.Count > 50)
                return BadRequest("Maximum 50 URLs allowed per request");

            var results = await _bannerFetchService.FetchBannersAsync(request.Urls);
            return Ok(results);
        }
    }
}
