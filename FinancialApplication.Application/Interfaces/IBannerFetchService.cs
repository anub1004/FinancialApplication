using FinancialApplication.Application.DTOs;

namespace FinancialApplication.Application.Interfaces
{
    public interface IBannerFetchService
    {
        Task<List<BannerResponseDto>> FetchBannersAsync(List<string> urls);
    }
}
