using Microsoft.AspNetCore.Http;

namespace FileStorageApi.Dtos;

public class AvatarUploadDto
{
    public IFormFile File { get; set; } = null!;
}
