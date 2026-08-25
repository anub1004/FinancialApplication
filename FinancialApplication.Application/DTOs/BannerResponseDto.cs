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

        /// <summary>
        /// The database ID of the saved compressed banner image.
        /// Use GET /api/blog/banner-image/{BannerId} to retrieve the image.
        /// </summary>
        public Guid? BannerId { get; set; }

        /// <summary>
        /// Original image size before compression (bytes).
        /// </summary>
        public long? OriginalSizeBytes { get; set; }

        /// <summary>
        /// Compressed image size after compression (bytes).
        /// </summary>
        public long? CompressedSizeBytes { get; set; }

        /// <summary>
        /// Compression ratio as percentage saved (e.g., 85 means 85% smaller).
        /// </summary>
        public double? CompressionRatio { get; set; }
    }
}
