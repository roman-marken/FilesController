namespace FileStorageApi.Dtos;

public class FileResponseDto
{
    public int Id { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public bool IsPublic { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? PublicUrl { get; set; }
    public DateTime UploadedAt { get; set; }
}
