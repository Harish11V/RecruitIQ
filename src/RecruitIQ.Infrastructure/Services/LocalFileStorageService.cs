using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using RecruitIQ.Application.Common.Interfaces;

namespace RecruitIQ.Infrastructure.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly FileStorageOptions _options;
    private readonly IWebHostEnvironment? _webHostEnvironment;

    public LocalFileStorageService(IOptions<FileStorageOptions> options, IWebHostEnvironment? webHostEnvironment = null)
    {
        _options = options.Value;
        _webHostEnvironment = webHostEnvironment;
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        var contentRoot = _webHostEnvironment?.ContentRootPath ?? AppContext.BaseDirectory;
        var targetDir = Path.Combine(contentRoot, _options.UploadFolder);

        if (!Directory.Exists(targetDir))
        {
            Directory.CreateDirectory(targetDir);
        }

        var filePath = Path.Combine(targetDir, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
        {
            await fileStream.CopyToAsync(stream, cancellationToken);
        }

        // Normalize URL path. E.g. "wwwroot/uploads/logos" becomes "/uploads/logos/{filename}"
        var normalizedFolder = _options.UploadFolder.Replace("\\", "/").TrimStart('/');
        if (normalizedFolder.StartsWith("wwwroot/", StringComparison.OrdinalIgnoreCase))
        {
            normalizedFolder = normalizedFolder.Substring("wwwroot/".Length);
        }
        else if (normalizedFolder.Equals("wwwroot", StringComparison.OrdinalIgnoreCase))
        {
            normalizedFolder = string.Empty;
        }

        var relativeUrl = string.IsNullOrEmpty(normalizedFolder) 
            ? $"/{fileName}" 
            : $"/{normalizedFolder}/{fileName}";

        return relativeUrl.Replace("//", "/");
    }

    public Task DeleteFileAsync(string fileUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileUrl))
        {
            return Task.CompletedTask;
        }

        var normalizedUrl = fileUrl.Replace("/", Path.DirectorySeparatorChar.ToString()).TrimStart(Path.DirectorySeparatorChar);
        
        // Prepends wwwroot to the relative URL to locate the file in wwwroot
        var relativePath = Path.Combine("wwwroot", normalizedUrl);
        var contentRoot = _webHostEnvironment?.ContentRootPath ?? AppContext.BaseDirectory;
        var physicalPath = Path.GetFullPath(Path.Combine(contentRoot, relativePath));

        if (File.Exists(physicalPath))
        {
            File.Delete(physicalPath);
        }

        return Task.CompletedTask;
    }
}
