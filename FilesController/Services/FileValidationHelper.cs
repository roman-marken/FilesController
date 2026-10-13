using Microsoft.AspNetCore.Http;

namespace FileStorageApi.Services;

public static class FileValidationHelper
{
    public static async Task ValidateAsync(IFormFile file, string[] allowedExtensions, long maxSizeBytes)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("Файл не вибрано.");
        }

        if (file.Length > maxSizeBytes)
        {
            throw new ArgumentException("Файл занадто великий.");
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
        {
            throw new ArgumentException("Таке розширення файлу не дозволено.");
        }

        await CheckMagicBytesAsync(file, extension);
    }

    private static async Task CheckMagicBytesAsync(IFormFile file, string extension)
    {
        var buffer = new byte[12];

        await using var stream = file.OpenReadStream();
        var read = await stream.ReadAsync(buffer);

        var ok = extension switch
        {
            ".jpg" or ".jpeg" => read >= 3 && buffer[0] == 0xFF && buffer[1] == 0xD8 && buffer[2] == 0xFF,
            ".png" => read >= 8 && buffer[0] == 0x89 && buffer[1] == 0x50 && buffer[2] == 0x4E && buffer[3] == 0x47,
            ".webp" => read >= 12 && IsText(buffer, 0, "RIFF") && IsText(buffer, 8, "WEBP"),
            ".pdf" => read >= 4 && IsText(buffer, 0, "%PDF"),
            ".docx" => read >= 2 && buffer[0] == 0x50 && buffer[1] == 0x4B,
            _ => false
        };

        if (!ok)
        {
            throw new InvalidOperationException("Файл не пройшов перевірку сигнатури.");
        }
    }

    private static bool IsText(byte[] buffer, int start, string text)
    {
        for (var i = 0; i < text.Length; i++)
        {
            if (buffer[start + i] != text[i])
            {
                return false;
            }
        }

        return true;
    }
}
