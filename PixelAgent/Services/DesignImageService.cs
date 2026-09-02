using System.IO;

namespace PixelAgent.Services;

public class DesignImageService
{
    public string? OpenDesignDialog()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Open Design",
            Filter = "Image Files|*.png;*.jpg;*.jpeg;*.webp"
        };

        if (dialog.ShowDialog() != DialogResult.OK)
        {
            return null;
        }

        var bytes = File.ReadAllBytes(dialog.FileName);
        var base64 = Convert.ToBase64String(bytes);

        return $"data:{GetMimeType(dialog.FileName)};base64,{base64}";
    }

    private static string GetMimeType(string fileName)
    {
        return Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
    }
}
