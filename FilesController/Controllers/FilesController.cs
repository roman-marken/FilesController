using FileStorageApi.Dtos;
using FileStorageApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace FileStorageApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FilesController : ControllerBase
{
    private readonly IFileStorageService _storageService;

    private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
    private static readonly string[] AllowedDocExtensions = { ".pdf", ".docx" };

    private const long MaxImageSize = 5 * 1024 * 1024;
    private const long MaxDocumentSize = 20 * 1024 * 1024;

    public FilesController(IFileStorageService storageService)
    {
        _storageService = storageService;
    }

    [HttpGet]
    public async Task<ActionResult<List<FileResponseDto>>> GetAllFiles()
    {
        var files = await _storageService.GetAllFilesAsync();
        return Ok(files);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<FileResponseDto>> GetFileById(int id)
    {
        var file = await _storageService.GetFileMetadataAsync(id);
        if (file == null)
        {
            return NotFound(new { error = "Файл не знайдено." });
        }

        return Ok(file);
    }

    [HttpPost("avatar")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxImageSize)]
    [ProducesResponseType(typeof(FileResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<FileResponseDto>> UploadAvatar([FromForm] AvatarUploadDto request)
    {
        try
        {
            var result = await _storageService.SaveFileAsync(
                file: request.File,
                subFolder: "avatars",
                isPublic: true,
                category: "Avatar",
                description: "Аватар користувача",
                allowedExtensions: AllowedImageExtensions,
                maxSizeBytes: MaxImageSize);

            return CreatedAtAction(nameof(GetFileById), new { id = result.Id }, result);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("document")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxDocumentSize)]
    public async Task<ActionResult<FileResponseDto>> UploadDocument([FromForm] DocumentUploadDto request)
    {
        try
        {
            var result = await _storageService.SaveFileAsync(
                file: request.File,
                subFolder: "documents",
                isPublic: false,
                category: "Document",
                description: request.Description,
                allowedExtensions: AllowedDocExtensions,
                maxSizeBytes: MaxDocumentSize);

            return CreatedAtAction(nameof(GetFileById), new { id = result.Id }, result);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{id:int}/download")]
    public async Task<IActionResult> DownloadFile(int id)
    {
        try
        {
            var (stream, contentType, downloadFileName) = await _storageService.GetFileForDownloadAsync(id);
            return File(stream, contentType, downloadFileName);
        }
        catch (Exception ex) when (ex is KeyNotFoundException or FileNotFoundException)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpGet("{id:int}/preview")]
    public async Task<IActionResult> PreviewFile(int id)
    {
        try
        {
            var (stream, contentType) = await _storageService.GetFileForPreviewAsync(id);
            return File(stream, contentType);
        }
        catch (Exception ex) when (ex is KeyNotFoundException or FileNotFoundException)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteFile(int id)
    {
        var deleted = await _storageService.DeleteFileAsync(id);
        if (!deleted)
        {
            return NotFound(new { error = "Файл не знайдено." });
        }

        return NoContent();
    }
}
