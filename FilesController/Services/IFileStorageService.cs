using FileStorageApi.Dtos;
using Microsoft.AspNetCore.Http;

namespace FileStorageApi.Services;

public interface IFileStorageService
{
    Task<FileResponseDto> SaveFileAsync(
        IFormFile file,
        string subFolder,
        bool isPublic,
        string category,
        string? description,
        string[] allowedExtensions,
        long maxSizeBytes);

    Task<(Stream Stream, string ContentType, string DownloadFileName)> GetFileForDownloadAsync(int id);
    Task<(Stream Stream, string ContentType)> GetFileForPreviewAsync(int id);
    Task<List<FileResponseDto>> GetAllFilesAsync();
    Task<FileResponseDto?> GetFileMetadataAsync(int id);
    Task<bool> DeleteFileAsync(int id);
}
