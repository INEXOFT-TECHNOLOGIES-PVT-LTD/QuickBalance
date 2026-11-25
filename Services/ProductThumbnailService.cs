using QuickBalance.API.Services;
using System.Security.Cryptography;

namespace QuickBalance.API.Services
{
    public static class ProductThumbnailService
    {
        private const int MaxWidth = 300;
        private const int MaxHeight = 300;
        private const int WebpQuality = 75;

        private const string ThumbnailFolderName = "product-thumbnails";

        /// <summary>
        /// Gets the directory used to store product thumbnails.
        /// Set PRODUCT_THUMBNAIL_PATH to use a persistent custom directory.
        /// </summary>
        private static string ThumbnailDirectory
        {
            get
            {
                string? configuredPath =
                    Environment.GetEnvironmentVariable("PRODUCT_THUMBNAIL_PATH");

                string path = !string.IsNullOrWhiteSpace(configuredPath)
                    ? configuredPath
                    : Path.Combine(
                        AppContext.BaseDirectory,
                        "App_Data",
                        ThumbnailFolderName);

                Directory.CreateDirectory(path);

                return path;
            }
        }
        /// <summary>
        /// Creates a WebP thumbnail while preserving the image aspect ratio.
        /// The resulting image will not exceed 300 x 300 pixels.
        //public static async Task<byte[]> CreateThumbnailAsync(
        //    byte[] originalBytes)
        //{
        //    ArgumentNullException.ThrowIfNull(originalBytes);

        //    if (originalBytes.Length == 0)
        //    {
        //        throw new ArgumentException(
        //            "The original image is empty.",
        //            nameof(originalBytes));
        //    }

        //    using var inputStream = new MemoryStream(originalBytes);

        //    using Image image = await Image.LoadAsync(inputStream);

        //    image.Mutate(operation => operation.Resize(
        //        new ResizeOptions
        //        {
        //            Size = new Size(MaxWidth, MaxHeight),
        //            Mode = ResizeMode.Max
        //        }));

        //    await using var outputStream = new MemoryStream();

        //    await image.SaveAsync(
        //        outputStream,
        //        new WebpEncoder
        //        {
        //            Quality = WebpQuality
        //        });

        //    return outputStream.ToArray();
        //}
        /// <
        /// 
        /// >
        /// Saves or replaces a product thumbnail.
        /// </summary>
        public static async Task SaveThumbnailAsync(
            int productId,
            byte[] thumbnailBytes)
        {
            ValidateProductId(productId);
            ArgumentNullException.ThrowIfNull(thumbnailBytes);

            if (thumbnailBytes.Length == 0)
            {
                throw new ArgumentException(
                    "Thumbnail data is empty.",
                    nameof(thumbnailBytes));
            }

            string path = GetThumbnailPath(productId);

            // Write to a temporary file first to avoid partially written thumbnails.
            string tempPath =
                path + "." + Guid.NewGuid().ToString("N") + ".tmp";

            try
            {
                await File.WriteAllBytesAsync(tempPath, thumbnailBytes);

                // Replace the existing thumbnail when one already exists.
                File.Move(tempPath, path, overwrite: true);
            }
            finally
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
        }
        /// <summary>
        /// Reads a product thumbnail.
        /// Returns null when the thumbnail does not exist.
        /// </summary>
        public static async Task<byte[]?> ReadThumbnailAsync(int productId)
        {
            ValidateProductId(productId);

            string path = GetThumbnailPath(productId);

            if (!File.Exists(path))
            {
                return null;
            }

            return await File.ReadAllBytesAsync(path);
        }
        /// <summary>
        /// Deletes a product thumbnail if it exists.
        /// </summary>
        public static void DeleteThumbnail(int productId)
        {
            ValidateProductId(productId);

            string path = GetThumbnailPath(productId);

            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        /// <summary>
        /// Builds the thumbnail file path for a product.
        /// </summary>
        private static string GetThumbnailPath(int productId)
        {
            return Path.Combine(
                ThumbnailDirectory,
                $"{productId}.webp");
        }

        private static void ValidateProductId(int productId)
        {
            if (productId <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(productId),
                    "Product ID must be greater than zero.");
            }
        }
    }
}
