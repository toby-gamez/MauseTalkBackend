using MauseTalkBackend.Domain.DTOs;

namespace MauseTalkBackend.Domain.Interfaces;

public interface IFileService
{
    Task<FileUploadResultDto> SaveFileAsync(FileUploadDto fileUpload, string category = "general");
    Task<byte[]?> GetFileAsync(string fileUrl);
    Task<bool> DeleteFileAsync(string fileUrl);
    Task<string> GetFilePathAsync(string fileUrl);
    bool IsImageFile(string mimeType);
    bool IsAudioFile(string mimeType);
    bool IsValidFileSize(long fileSize, string mimeType);
}