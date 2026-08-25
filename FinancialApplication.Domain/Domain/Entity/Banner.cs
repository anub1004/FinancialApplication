using System;
using System.ComponentModel.DataAnnotations;

namespace FinancialApplication.Domain.Domain.Entity
{
    /// <summary>
    /// Stores a compressed banner image fetched from an external URL.
    /// Images are downloaded, compressed (resized + JPEG quality reduction),
    /// and stored as byte[] to avoid dependency on external image availability.
    /// </summary>
    public class Banner
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// The compressed image binary data (JPEG format).
        /// </summary>
        [Required]
        public byte[] CompressedImage { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// MIME type of the stored image (e.g., "image/jpeg").
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string ContentType { get; set; } = "image/jpeg";

        /// <summary>
        /// The original external URL the image was scraped from.
        /// </summary>
        [Required]
        [MaxLength(2048)]
        public string OriginalUrl { get; set; } = string.Empty;

        /// <summary>
        /// The source page URL that contained this banner image.
        /// </summary>
        [MaxLength(2048)]
        public string? SourcePageUrl { get; set; }

        /// <summary>
        /// OG title scraped from the source page.
        /// </summary>
        [MaxLength(500)]
        public string? Title { get; set; }

        /// <summary>
        /// OG description scraped from the source page.
        /// </summary>
        [MaxLength(2000)]
        public string? Description { get; set; }

        /// <summary>
        /// Original image file size before compression (bytes).
        /// </summary>
        public long OriginalSizeBytes { get; set; }

        /// <summary>
        /// Compressed image file size (bytes).
        /// </summary>
        public long CompressedSizeBytes { get; set; }

        /// <summary>
        /// UTC timestamp of when this banner was created.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
