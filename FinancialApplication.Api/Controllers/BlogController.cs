using FinancialApplication.Application.DTOs;
using FinancialApplication.Application.Interfaces;
using FinancialApplication.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinancialApplication.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BlogController : ControllerBase
    {
        private readonly IBannerFetchService _bannerFetchService;
        private readonly AppDbContext _dbContext;

        public BlogController(IBannerFetchService bannerFetchService, AppDbContext dbContext)
        {
            _bannerFetchService = bannerFetchService;
            _dbContext = dbContext;
        }

        /// <summary>
        /// Fetches banner images from external URLs, compresses them, and stores in DB.
        /// Returns banner metadata including the BannerId for image retrieval.
        /// </summary>
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

        /// <summary>
        /// Retrieves a compressed banner image by its database ID.
        /// Returns the raw JPEG image bytes with proper content type.
        /// </summary>
        [HttpGet("banner-image/{id:guid}")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ResponseCache(Duration = 86400)] // Cache for 24 hours — images are immutable once compressed
        public async Task<IActionResult> GetBannerImage(Guid id)
        {
            var banner = await _dbContext.Banners
                .Where(b => b.Id == id)
                .Select(b => new { b.CompressedImage, b.ContentType })
                .FirstOrDefaultAsync();

            if (banner == null)
                return NotFound(new { error = "Banner image not found." });

            return File(banner.CompressedImage, banner.ContentType);
        }
    }
}
