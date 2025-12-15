using Microsoft.AspNetCore.Http;

namespace NB.Service.Common
{
    public interface ICloudinaryService
    {
        Task<string?> UploadImageAsync(IFormFile file, string folder = "contracts/images");
        Task<string?> UploadImageFromBytesAsync(byte[] imageBytes, string fileName, string folder = "qrcodes");
        Task<bool> DeleteFileAsync(string publicId);
        Task<string?> UpdateImageAsync(IFormFile newFile, string? oldPublicId, string folder = "contracts/images");
    }
}
