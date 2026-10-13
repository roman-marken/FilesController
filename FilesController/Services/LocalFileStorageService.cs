using FileStorageApi.Data;
using FileStorageApi.Dtos;
using FileStorageApi.Models;
using Microsoft.EntityFrameworkCore;

namespace FileStorageApi.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly FileDbContext _dbContext;
    private readonly IWebHostEnvironment _environment;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public LocalFileStorageService(
        FileDbContext dbContext,
        IWebHostEnvironment environment,
        IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _environment = environment;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<FileResponseDto> SaveFileAsync(
        IFormFile file,
        string subFolder,
        bool isPublic,
        string category,
        string? description,
        string[] allowedExtensions,
        long maxSizeBytes)
    {
        await FileValidationHelper.ValidateAsync(file, allowedExtensions, maxSizeBytes);

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var storedFileName = $"{Guid.NewGuid()}{extension}";

        var relativeFolder = isPublic
            ? Path.Combine("wwwroot", "uploads", subFolder)
            : Path.Combine("App_Data", "PrivateUploads", subFolder);

        var fullFolder = Path.Combine(_environment.ContentRootPath, relativeFolder);
        Directory.CreateDirectory(fullFolder);

        var relativePath = Path.Combine(relativeFolder, storedFileName);
        var physicalPath = Path.Combine(_environment.ContentRootPath, relativePath);

        await using (var stream = new FileStream(physicalPath, FileMode.CreateNew))
        {
            await file.CopyToAsync(stream);
        }

        var storedFile = new StoredFile
        {
            OriginalFileName = Path.GetFileName(file.FileName),
            StoredFileName = storedFileName,
            ContentType = string.IsNullOrWhiteSpace(file.ContentType)
                ? "application/octet-stream"
                : file.ContentType,
            RelativePath = relativePath,
            SizeBytes = file.Length,
            IsPublic = isPublic,
            Category = category,
            Description = description,
            UploadedAt = DateTime.UtcNow
        };

        _dbContext.StoredFiles.Add(storedFile);
        await _dbContext.SaveChangesAsync();

        return ToDto(storedFile);
    }

    public async Task<(Stream Stream, string ContentType, string DownloadFileName)> GetFileForDownloadAsync(int id)
    {
        var storedFile = await FindFileOrThrowAsync(id);
        var physicalPath = GetPhysicalPath(storedFile);

        var stream = new FileStream(physicalPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return (stream, storedFile.ContentType, storedFile.OriginalFileName);
    }

    public async Task<(Stream Stream, string ContentType)> GetFileForPreviewAsync(int id)
    {
        var storedFile = await FindFileOrThrowAsync(id);
        var physicalPath = GetPhysicalPath(storedFile);

        var stream = new FileStream(physicalPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return (stream, storedFile.ContentType);
    }

    public async Task<List<FileResponseDto>> GetAllFilesAsync()
    {
        var files = await _dbContext.StoredFiles
            .OrderByDescending(x => x.UploadedAt)
            .ToListAsync();

        return files.Select(ToDto).ToList();
    }

    public async Task<FileResponseDto?> GetFileMetadataAsync(int id)
    {
        var storedFile = await _dbContext.StoredFiles.FindAsync(id);
        return storedFile == null ? null : ToDto(storedFile);
    }

    public async Task<bool> DeleteFileAsync(int id)
    {
        var storedFile = await _dbContext.StoredFiles.FindAsync(id);
        if (storedFile == null)
        {
            return false;
        }

        var physicalPath = Path.Combine(_environment.ContentRootPath, storedFile.RelativePath);
        if (File.Exists(physicalPath))
        {
            File.Delete(physicalPath);
        }

        _dbContext.StoredFiles.Remove(storedFile);
        await _dbContext.SaveChangesAsync();

        return true;
    }

    private async Task<StoredFile> FindFileOrThrowAsync(int id)
    {
        var storedFile = await _dbContext.StoredFiles.FindAsync(id);
        if (storedFile == null)
        {
            throw new KeyNotFoundException("Файл не знайдено.");
        }

        var physicalPath = GetPhysicalPath(storedFile);
        if (!File.Exists(physicalPath))
        {
            throw new FileNotFoundException("Файл не знайдено на диску.");
        }

        return storedFile;
    }

    private string GetPhysicalPath(StoredFile storedFile)
    {
        return Path.Combine(_environment.ContentRootPath, storedFile.RelativePath);
    }

    private FileResponseDto ToDto(StoredFile file)
    {
        return new FileResponseDto
        {
            Id = file.Id,
            OriginalFileName = file.OriginalFileName,
            ContentType = file.ContentType,
            SizeBytes = file.SizeBytes,
            IsPublic = file.IsPublic,
            Category = file.Category,
            Description = file.Description,
            UploadedAt = file.UploadedAt,
            PublicUrl = file.IsPublic ? BuildPublicUrl(file.RelativePath) : null
        };
    }

    private string? BuildPublicUrl(string relativePath)
    {
        var publicPath = relativePath
            .Replace("\\", "/")
            .Replace("wwwroot/", string.Empty);

        var request = _httpContextAccessor.HttpContext?.Request;
        if (request == null)
        {
            return "/" + publicPath;
        }

        return $"{request.Scheme}://{request.Host}/{publicPath}";
    }
}
