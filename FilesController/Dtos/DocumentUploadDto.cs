using Microsoft.AspNetCore.Http;

namespace FileStorageApi.Dtos;

public class DocumentUploadDto
{
    public IFormFile File { get; set; } = null!;
    public string? Description { get; set; }
}
