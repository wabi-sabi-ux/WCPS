using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace WCPS.WebApp.Services
{
    public class FileService
    {
        private readonly IWebHostEnvironment _env;
        private static readonly string[] AllowedExtensions = { ".pdf", ".jpg", ".jpeg", ".png" };
        private const long MaxBytes = 5 * 1024 * 1024; // 5 MB

        public FileService(IWebHostEnvironment env)
        {
            _env = env;
        }

        /// <summary>
        /// Saves a receipt for the given userId. Returns (success, relativePath, errorMessage).
        /// relativePath is stored in DB and has format "userId/filename.ext".
        /// </summary>
        public async Task<(bool Success, string? RelativePath, string? Error)> SaveReceiptAsync(IFormFile file, string userId)
        {
            if (file == null || file.Length == 0)
                return (false, null, "No file provided.");

            if (file.Length > MaxBytes)
                return (false, null, $"File too large. Max {MaxBytes / (1024 * 1024)} MB.");

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext))
                return (false, null, "Invalid file type. Allowed: .pdf, .jpg, .jpeg, .png");

            if (!file.ContentType.StartsWith("image/") && file.ContentType != "application/pdf")
                return (false, null, "Invalid content type.");

            // read into memory (safe for 5 MB)
            await using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            ms.Position = 0;

            // basic magic-bytes checks:
            if (ext == ".pdf")
            {
                if (ms.Length < 4) return (false, null, "Invalid PDF file.");
                var header = new byte[4];
                ms.Read(header, 0, 4);
                var headerStr = System.Text.Encoding.ASCII.GetString(header);
                if (!headerStr.StartsWith("%PDF"))
                    return (false, null, "Invalid PDF file.");
            }
            else if (ext == ".jpg" || ext == ".jpeg")
            {
                if (ms.Length < 2) return (false, null, "Invalid JPEG file.");
                var sig = new byte[2];
                ms.Read(sig, 0, 2);
                if (sig[0] != 0xFF || sig[1] != 0xD8)
                    return (false, null, "Invalid JPEG file.");
            }
            else if (ext == ".png")
            {
                if (ms.Length < 4) return (false, null, "Invalid PNG file.");
                var sig = new byte[4];
                ms.Read(sig, 0, 4);
                if (!(sig[0] == 0x89 && sig[1] == 0x50 && sig[2] == 0x4E && sig[3] == 0x47))
                    return (false, null, "Invalid PNG file.");
            }

            // prepare folders outside wwwroot: {ContentRoot}/Uploads/{userId}
            var uploadRoot = Path.Combine(_env.ContentRootPath, "Uploads");
            var userFolder = Path.Combine(uploadRoot, userId);
            Directory.CreateDirectory(userFolder);

            var safeFileName = $"{Guid.NewGuid():N}{ext}";
            var fullPath = Path.Combine(userFolder, safeFileName);

            // write to disk
            ms.Position = 0;
            await using (var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await ms.CopyToAsync(fs);
            }

            var relative = Path.Combine(userId, safeFileName).Replace('\\', '/');
            return (true, relative, null);
        }

        /// <summary>
        /// Gets the absolute full path for a stored relative path (stored in DB).
        /// </summary>
        public string GetFullPath(string relativePath)
        {
            return Path.Combine(_env.ContentRootPath, "Uploads", relativePath.Replace('/', Path.DirectorySeparatorChar));
        }

        /// <summary>
        /// Maps file extension to content type for response.
        /// </summary>
        public string GetContentType(string fullPath)
        {
            var ext = Path.GetExtension(fullPath).ToLowerInvariant();
            return ext switch
            {
                ".pdf" => "application/pdf",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                _ => "application/octet-stream"
            };
        }
    }
}
