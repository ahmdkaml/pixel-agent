using PixelAgent.Models;

namespace PixelAgent.Services;

public class PixelAgentApp
{
    public string? Design { get; private set; }

    public double Similarity { get; private set; }

    private readonly ExportService _exportService;
    private readonly DesignImageService _designService;
    private readonly RenderService _renderService;
    private readonly SimilarityService _similarityService;

    public WebPage WebPage { get; } = new WebPage();

    private CancellationTokenSource? _similarityDebounce;

    public event EventHandler? SimilarityChanged;

    public PixelAgentApp(
        DesignImageService designService,
        RenderService renderService,
        ExportService exportService,
        SimilarityService similarityService,
        WebPage webPage)
    {
        _designService = designService;
        _renderService = renderService;
        _exportService = exportService;
        _similarityService = similarityService;
        WebPage = webPage;
    }

    public string? OpenDesignDialog()
    {
        var imageData = _designService.OpenDesignDialog();

        if (imageData != null)
        {
            Design = imageData;
        }

        return imageData;
    }

    public List<object>? LoadImages()
    {
        return _designService.LoadImages();
    }

    public async Task UpdatePage(string html, string css)
    {
        await _renderService.UpdatePage(html, css);

        _similarityDebounce?.Cancel();
        _similarityDebounce?.Dispose();

        _similarityDebounce = new CancellationTokenSource();

        _ = CalculateSimilarityAfterDelay(_similarityDebounce.Token);
    }

    private async Task CalculateSimilarityAfterDelay(
        CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(
                TimeSpan.FromSeconds(5),
                cancellationToken
            );

            if (Design == null)
            {
                return;
            }

            Similarity = await _similarityService.Calculate(
                Design,
                WebPage.Html,
                WebPage.Css
            );

            SimilarityChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (OperationCanceledException)
        {
            // A newer render update replaced this calculation.
        }
    }

    public void ExportApp()
    {
        _exportService.ExportApp();
    }
}
