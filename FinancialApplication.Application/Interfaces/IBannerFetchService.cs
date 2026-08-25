using FinancialApplication.Application.DTOs;

namespace FinancialApplication.Application.Interfaces
{
    /// <summary>
    /// Fetches banner images from external URLs, compresses them, and stores them in the database.
    /// </summary>
    public interface IBannerFetchService
    {
        /// <summary>
        /// Scrapes banner images from the given URLs, downloads and compresses them,
        /// and stores the compressed images in the database.
        /// </summary>
        Task<List<BannerResponseDto>> FetchBannersAsync(List<string> urls);
    }
}
