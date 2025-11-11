using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Forms;
using MauseTalkBackend.Domain.DTOs;
using MauseTalkBackend.Domain.Entities;
using MauseTalkBackend.Shared.Models;

namespace MauseTalkBackend.Client.Services;

public class FileService
{
    private readonly HttpClient _httpClient;

    public FileService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ApiResponse<FileUploadResult>> UploadFileAsync(Stream fileStream, string fileName, string mimeType)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            using var fileContent = new StreamContent(fileStream);
            
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(mimeType);
            content.Add(fileContent, "file", fileName);

            var response = await _httpClient.PostAsync("api/files/upload", content);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<FileUploadResult>>();
            
            return result ?? new ApiResponse<FileUploadResult> { Success = false, Message = "Neočekávaná chyba" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<FileUploadResult>
            {
                Success = false,
                Message = "Chyba při nahrávání souboru",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<byte[]?> DownloadFileAsync(string fileUrl)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/files/download?fileUrl={Uri.EscapeDataString(fileUrl)}");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsByteArrayAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Download file error: {ex.Message}");
        }
        return null;
    }

    public async Task<ApiResponse> DeleteFileAsync(string fileUrl)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/files?fileUrl={Uri.EscapeDataString(fileUrl)}");
            var result = await response.Content.ReadFromJsonAsync<ApiResponse>();
            return result ?? new ApiResponse { Success = false, Message = "Neočekávaná chyba" };
        }
        catch (Exception ex)
        {
            return new ApiResponse
            {
                Success = false,
                Message = "Chyba při mazání souboru",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public bool IsValidImageFile(string fileName, long fileSize)
    {
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp" };
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        const long maxSize = 10 * 1024 * 1024; // 10MB

        return allowedExtensions.Contains(extension) && fileSize <= maxSize;
    }

    public bool IsValidAudioFile(string fileName, long fileSize)
    {
        var allowedExtensions = new[] { ".mp3", ".wav", ".m4a", ".ogg", ".aac", ".flac" };
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        const long maxSize = 25 * 1024 * 1024; // 25MB

        return allowedExtensions.Contains(extension) && fileSize <= maxSize;
    }

    public bool IsValidDocumentFile(string fileName, long fileSize)
    {
        var allowedExtensions = new[] { ".pdf", ".txt", ".doc", ".docx", ".xlsx", ".pptx" };
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        const long maxSize = 50 * 1024 * 1024; // 50MB

        return allowedExtensions.Contains(extension) && fileSize <= maxSize;
    }

    public MessageType GetMessageTypeFromFile(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        if (new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp" }.Contains(extension))
            return MessageType.Image;

        if (new[] { ".mp3", ".wav", ".m4a", ".ogg", ".aac", ".flac" }.Contains(extension))
            return MessageType.Voice;

        return MessageType.File;
    }

    public string GetMimeType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".bmp" => "image/bmp",
            ".mp3" => "audio/mpeg",
            ".wav" => "audio/wav",
            ".m4a" => "audio/mp4",
            ".ogg" => "audio/ogg",
            ".aac" => "audio/aac",
            ".flac" => "audio/flac",
            ".pdf" => "application/pdf",
            ".txt" => "text/plain",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            _ => "application/octet-stream"
        };
    }

    public string FormatFileSize(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        int counter = 0;
        decimal number = bytes;

        while (Math.Round(number / 1024) >= 1)
        {
            number = number / 1024;
            counter++;
        }

        return $"{number:n1} {suffixes[counter]}";
    }
}