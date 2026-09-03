using System.IO;

namespace PixelAgent.Services;

public class DesignImageService
{
    public string? OpenDesignDialog()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Open Design",
            Filter = "Image Files|*.png;*.jpg;*.jpeg;*.webp;*.svg"
        };

        if (dialog.ShowDialog() != DialogResult.OK)
        {
            return null;
        }

        var bytes = File.ReadAllBytes(dialog.FileName);
        var base64 = Convert.ToBase64String(bytes);

        return $"data:{GetMimeType(dialog.FileName)};base64,{base64}";
    }

    public List<object>? LoadImages()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Load Images",
            Filter = "Image Files|*.png;*.jpg;*.jpeg;*.webp;*.svg",
            Multiselect = true
        };

        if (dialog.ShowDialog() != DialogResult.OK)
        {
            return null;
        }

        return dialog.FileNames
            .Select(fileName =>
            {
                var bytes = File.ReadAllBytes(fileName);
                var base64 = Convert.ToBase64String(bytes);

                return new
                {
                    Name = Path.GetFileName(fileName),
                    Data = $"data:{GetMimeType(fileName)};base64,{base64}"
                };
            })
            .Cast<object>()
            .ToList();
    }
    private static string GetMimeType(string fileName)
    {
        return Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            ".svg" => "image/svg+xml",
            _ => "application/octet-stream"
        };
    }
}
