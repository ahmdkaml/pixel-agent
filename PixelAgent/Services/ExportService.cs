using PixelAgent.Models;

namespace PixelAgent.Services;

public class ExportService
{
    private readonly WebPage _webPage;

    public ExportService(WebPage webPage)
    {
        _webPage = webPage;
    }

    public void ExportApp()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Select export folder",
            UseDescriptionForTitle = true
        };

        if (dialog.ShowDialog() != DialogResult.OK)
        {
            return;
        }

        var appDirectory = Path.Combine(dialog.SelectedPath, "app");

        Directory.CreateDirectory(appDirectory);

        File.WriteAllText(
            Path.Combine(appDirectory, "index.html"),
            _webPage.Html
        );

        File.WriteAllText(
            Path.Combine(appDirectory, "styles.css"),
            _webPage.Css
        );
    }
}
