using PixelAgent.Models;

namespace PixelAgent.Services;

public class PixelAgentApp
{
    private readonly PixelAgentServices _services;
    private readonly PixelAgentState _state;

    public double Similarity { get; private set; }

    private CancellationTokenSource? _similarityDebounce;

    public event EventHandler? SimilarityChanged;

    public PixelAgentApp(
        PixelAgentServices services,
        PixelAgentState state)
    {
        _services = services;
        _state = state;
    }

    public string? OpenDesignDialog()
    {
        var imageData = _services.Design.OpenDesignDialog();

        if (imageData != null)
        {
            _state.WebDesign.Design = imageData;
        }

        return _state.WebDesign.Design;
    }

    public List<object>? LoadImages()
    {
        var images = _services.Design.LoadImages();

        if (images != null)
        {
            _state.WebAssets.Images = images;
        }

        return _state.WebAssets.Images;
    }

    public async Task UpdatePage(string html, string css)
    {
        await _services.Render.UpdatePage(html, css);

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
                cancellationToken);

            if (_state.WebDesign.Design == null)
            {
                return;
            }

            var renderedSnapshot = await _services.Render.CaptureSnapshot();

            var renderedImage = await _services.Screenshot.Capture(
                renderedSnapshot.Srcdoc,
                renderedSnapshot.Width,
                renderedSnapshot.Height);

            Similarity = await _services.Similarity.Calculate(
                _state.WebDesign.Design,
                renderedImage);

            SimilarityChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (OperationCanceledException)
        {
            // A newer render update replaced this calculation.
        }
    }

    public void DetectImages()
    {
        var detected = _services.Detection.Detect(
            _state.WebDesign.Design ?? string.Empty,
            _state.WebAssets.Images);

    }

    public void DetectTexts()
    {
        var detected = _services.TextDetection.Detect(
            _state.WebDesign.Design ?? string.Empty);

        MessageBox.Show(
    string.Join(
        Environment.NewLine,
        detected.Select(d =>
            $"{d.Text} — X: {d.X}, Y: {d.Y}, W: {d.Width}, H: {d.Height}")));
    }

    public void ExportApp()
    {
        _services.Export.ExportApp();
    }
}
