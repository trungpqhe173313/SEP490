using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Configuration;

namespace NB.API.Utils
{
    public interface ICloudinaryService
    {
        Task<string?> UploadImageAsync(IFormFile file, string folder = "contracts/images");
        Task<bool> DeleteFileAsync(string publicId);
        Task<string?> UpdateImageAsync(IFormFile newFile, string? oldPublicId, string folder = "contracts/images");
    }

    public class CloudinaryService : ICloudinaryService
    {
        private readonly Cloudinary _cloudinary;
        private readonly ILogger<CloudinaryService> _logger;

        public CloudinaryService(IConfiguration configuration, ILogger<CloudinaryService> logger)
        {
            var cloudName = configuration["Cloudinary:CloudName"];
            var apiKey = configuration["Cloudinary:ApiKey"];
            var apiSecret = configuration["Cloudinary:ApiSecret"];

            if (string.IsNullOrEmpty(cloudName) || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret))
            {
                throw new ArgumentException("Cloudinary configuration is missing or invalid");
            }

            var account = new Account(cloudName, apiKey, apiSecret);
            _cloudinary = new Cloudinary(account);
            _logger = logger;
        }

        public async Task<string?> UploadImageAsync(IFormFile file, string folder = "contracts/images")
        {
            if (file == null || file.Length == 0)
                return null;

            try
            {
                using var stream = file.OpenReadStream();
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    Folder = folder, // Thư mục trên Cloudinary
                    UseFilename = false,
                    UniqueFilename = true,
                    Overwrite = false
                };

                var uploadResult = await _cloudinary.UploadAsync(uploadParams);

                if (uploadResult.Error != null)
                {
                    _logger.LogError($"Cloudinary upload error: {uploadResult.Error.Message}");
                    return null;
                }

                // Return relative path (PublicId)
                return uploadResult.PublicId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading image to Cloudinary");
                return null;
            }
        }

        public async Task<bool> DeleteFileAsync(string publicId)
        {
            if (string.IsNullOrEmpty(publicId))
                return false;

            try
            {
                var deletionParams = new DeletionParams(publicId);
                var result = await _cloudinary.DestroyAsync(deletionParams);

                return result.Result == "ok";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file from Cloudinary");
                return false;
            }
        }

        public async Task<string?> UpdateImageAsync(IFormFile newFile, string? oldPublicId, string folder = "contracts/images")
        {
            if (newFile == null || newFile.Length == 0)
                return null;

            try
            {
                // Upload image mới
                var newPublicId = await UploadImageAsync(newFile, folder);

                if (newPublicId == null)
                {
                    _logger.LogError("Failed to upload new image during update");
                    return null;
                }

                // Delete image cũ 
                if (!string.IsNullOrEmpty(oldPublicId))
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await DeleteFileAsync(oldPublicId);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, $"Failed to delete old image: {oldPublicId}");
                        }
                    });
                }

                return newPublicId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating image in Cloudinary");
                return null;
            }
        }
    }
}
