using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MauseTalkBackend.Domain.Interfaces;
using MauseTalkBackend.Domain.DTOs;
using MauseTalkBackend.Shared.Models;
using MauseTalkBackend.Shared.Constants;
using MauseTalkBackend.Shared.Extensions;

namespace MauseTalkBackend.App.Controllers;

[ApiController]
[Route(ApiConstants.ApiPrefix + ApiConstants.Routes.Files)]
[Authorize(Policy = ApiConstants.Policies.RequireAuthenticated)]
public class FilesController : ControllerBase
{
    private readonly IFileService _fileService;

    public FilesController(IFileService fileService)
    {
        _fileService = fileService;
    }

    [HttpPost("upload")]
    public async Task<ActionResult<ApiResponse<FileUploadResultDto>>> UploadFile(IFormFile file, [FromForm] string category = "general")
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(ApiResponse<FileUploadResultDto>.ErrorResult("No file uploaded"));
            }

            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);

            var fileUploadDto = new FileUploadDto
            {
                FileName = file.FileName,
                MimeType = file.ContentType,
                FileSize = file.Length,
                Content = memoryStream.ToArray()
            };

            var result = await _fileService.SaveFileAsync(fileUploadDto, category);
            return Ok(ApiResponse<FileUploadResultDto>.SuccessResult(result, "File uploaded successfully"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<FileUploadResultDto>.ErrorResult(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse<FileUploadResultDto>.ErrorResult("Internal server error", new[] { ex.Message }));
        }
    }

    [HttpGet("download")]
    public async Task<ActionResult> DownloadFile([FromQuery] string fileUrl)
    {
        try
        {
            if (string.IsNullOrEmpty(fileUrl))
            {
                return BadRequest("File URL is required");
            }

            var fileContent = await _fileService.GetFileAsync(fileUrl);
            if (fileContent == null)
            {
                return NotFound("File not found");
            }

            var fileName = Path.GetFileName(fileUrl);
            return File(fileContent, "application/octet-stream", fileName);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    [HttpDelete]
    public async Task<ActionResult<ApiResponse>> DeleteFile([FromQuery] string fileUrl)
    {
        try
        {
            if (string.IsNullOrEmpty(fileUrl))
            {
                return BadRequest(ApiResponse.ErrorResult("File URL is required"));
            }

            var deleted = await _fileService.DeleteFileAsync(fileUrl);
            if (!deleted)
            {
                return NotFound(ApiResponse.ErrorResult("File not found"));
            }

            return Ok(ApiResponse.SuccessResult("File deleted successfully"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse.ErrorResult("Internal server error", new[] { ex.Message }));
        }
    }
}