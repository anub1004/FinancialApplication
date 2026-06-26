namespace FinancialApplication.Application.DTOs
{
    public class BannerResponseDto
    {
        public string Url { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public bool Success { get; set; }
        public string? Error { get; set; }
    }
}
