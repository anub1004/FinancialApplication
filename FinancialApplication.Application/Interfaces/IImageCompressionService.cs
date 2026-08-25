namespace FinancialApplication.Application.Interfaces
{
    /// <summary>
    /// Compresses images for storage — resizes and reduces quality to minimize DB footprint.
    /// </summary>
    public interface IImageCompressionService
    {
        /// <summary>
        /// Compresses an image to JPEG format with the specified max width and quality.
        /// </summary>
        /// <param name="imageBytes">Raw image bytes (any supported format: JPEG, PNG, WebP, GIF, BMP).</param>
        /// <param name="maxWidth">Maximum width in pixels. Height is scaled proportionally. Default: 800px.</param>
        /// <param name="quality">JPEG quality (1-100). Lower = smaller file. Default: 70.</param>
        /// <returns>Compressed JPEG bytes.</returns>
        byte[] Compress(byte[] imageBytes, int maxWidth = 800, int quality = 70);
    }
}
