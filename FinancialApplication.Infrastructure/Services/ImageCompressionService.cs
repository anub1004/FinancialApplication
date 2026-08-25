using FinancialApplication.Application.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace FinancialApplication.Infrastructure.Services
{
    /// <summary>
    /// Compresses images using SixLabors.ImageSharp (pure managed .NET, no native deps).
    /// 
    /// Strategy:
    ///   1. Decode the image from any supported format (JPEG, PNG, WebP, GIF, BMP, TIFF)
    ///   2. Resize to fit within maxWidth while preserving aspect ratio
    ///   3. Re-encode as JPEG with the specified quality level
    /// 
    /// Typical compression ratios:
    ///   - 2MB PNG  → ~80KB JPEG (quality 70, 800px width)
    ///   - 500KB JPEG → ~60KB JPEG (quality 70, 800px width)
    /// </summary>
    public class ImageCompressionService : IImageCompressionService
    {
        /// <inheritdoc />
        public byte[] Compress(byte[] imageBytes, int maxWidth = 800, int quality = 70)
        {
            if (imageBytes == null || imageBytes.Length == 0)
                return Array.Empty<byte>();

            // Clamp quality to valid JPEG range
            quality = Math.Clamp(quality, 1, 100);

            using var image = Image.Load(imageBytes);

            // Only resize if the image is wider than maxWidth (don't upscale)
            if (image.Width > maxWidth)
            {
                image.Mutate(ctx => ctx.Resize(new ResizeOptions
                {
                    Size = new Size(maxWidth, 0), // 0 = auto-calculate height preserving aspect ratio
                    Mode = ResizeMode.Max
                }));
            }

            // Encode as JPEG with specified quality
            using var outputStream = new MemoryStream();
            var encoder = new JpegEncoder { Quality = quality };
            image.SaveAsJpeg(outputStream, encoder);

            return outputStream.ToArray();
        }
    }
}
