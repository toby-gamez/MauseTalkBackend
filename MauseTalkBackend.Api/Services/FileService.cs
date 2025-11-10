using MauseTalkBackend.Domain.DTOs;
using MauseTalkBackend.Domain.Interfaces;
using MauseTalkBackend.Shared.Constants;
using MauseTalkBackend.Shared.Extensions;

namespace MauseTalkBackend.Api.Services;

public class FileService : IFileService
{
    private readonly string _uploadsPath;

    public FileService()
    {
        _uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
        Directory.CreateDirectory(_uploadsPath);
    }

    public async Task<FileUploadResultDto> SaveFileAsync(FileUploadDto fileUpload, string category = "general")
    {
        if (!IsValidFileSize(fileUpload.FileSize, fileUpload.MimeType))
        {
            throw new ArgumentException($"File size {fileUpload.FileSize.ToFileSize()} exceeds maximum allowed size");
        }

        var categoryPath = Path.Combine(_uploadsPath, category);
        Directory.CreateDirectory(categoryPath);

        var fileExtension = Path.GetExtension(fileUpload.FileName);
        var sanitizedFileName = Path.GetFileNameWithoutExtension(fileUpload.FileName).SanitizeFileName();
        var uniqueFileName = $"{sanitizedFileName}_{Guid.NewGuid():N}{fileExtension}";
        var filePath = Path.Combine(categoryPath, uniqueFileName);

        await File.WriteAllBytesAsync(filePath, fileUpload.Content);

        var fileUrl = $"/uploads/{category}/{uniqueFileName}";

        return new FileUploadResultDto
        {
            FileUrl = fileUrl,
            FileName = fileUpload.FileName,
            FileSize = fileUpload.FileSize,
            MimeType = fileUpload.MimeType
        };
    }

    public async Task<byte[]?> GetFileAsync(string fileUrl)
    {
        var filePath = await GetFilePathAsync(fileUrl);
        if (File.Exists(filePath))
        {
            return await File.ReadAllBytesAsync(filePath);
        }
        return null;
    }

    public async Task<bool> DeleteFileAsync(string fileUrl)
    {
        var filePath = await GetFilePathAsync(fileUrl);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            return true;
        }
        return false;
    }

    public Task<string> GetFilePathAsync(string fileUrl)
    {
        var relativePath = fileUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        return Task.FromResult(Path.Combine(_uploadsPath.Replace("uploads", ""), relativePath));
    }

    public bool IsImageFile(string mimeType)
    {
        return FileConstants.Images.AllowedMimeTypes.Contains(mimeType.ToLowerInvariant());
    }

    public bool IsAudioFile(string mimeType)
    {
        return FileConstants.Audio.AllowedMimeTypes.Contains(mimeType.ToLowerInvariant());
    }

    public bool IsValidFileSize(long fileSize, string mimeType)
    {
        if (IsImageFile(mimeType))
            return fileSize <= FileConstants.Images.MaxSizeBytes;
        
        if (IsAudioFile(mimeType))
            return fileSize <= FileConstants.Audio.MaxSizeBytes;
        
        return fileSize <= FileConstants.General.MaxSizeBytes;
    }
}