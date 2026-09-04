using PixelAgent.Models;

namespace PixelAgent.Services;

public class PixelAgentApp
{

    public double Similarity { get; private set; }

    private readonly ExportService _exportService;
    private readonly DesignImageService _designService;
    private readonly RenderService _renderService;
    private readonly PlaywrightScreenshotService _playwrightScreenshotService;
    private readonly SimilarityService _similarityService;

    public WebPage WebPage { get; }

    public WebDesign WebDesign { get; }

    private CancellationTokenSource? _similarityDebounce;

    public event EventHandler? SimilarityChanged;

    public PixelAgentApp(
        DesignImageService designService,
        RenderService renderService,
        PlaywrightScreenshotService playwrightScreenshotService,
        ExportService exportService,
        SimilarityService similarityService,
        WebPage webPage,
        WebDesign webDesign)
    {
        _designService = designService;
        _renderService = renderService;
        _playwrightScreenshotService = playwrightScreenshotService;
        _exportService = exportService;
        _similarityService = similarityService;
        WebPage = webPage;
        WebDesign = webDesign;
    }

    public string? OpenDesignDialog()
    {
        var imageData = _designService.OpenDesignDialog();

        if (imageData != null)
        {
            WebDesign.Design = imageData;
        }

        return WebDesign.Design;
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

    private async Task CalculateSimilarityAfterDelay(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);

            if (WebDesign.Design == null)
            {
                return;
            }

            var renderedSnapshot = await _renderService.CaptureSnapshot();
            var renderedImage = await _playwrightScreenshotService.Capture(
                renderedSnapshot.Srcdoc,
                renderedSnapshot.Width,
                renderedSnapshot.Height
            );

            Similarity = await _similarityService.Calculate(
                WebDesign.Design,
                renderedImage
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
