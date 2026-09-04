using System.Text.RegularExpressions;
using OpenCvSharp;
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

        _state.WebDesign.DetectedImages = detected;

    }

    public void DetectTexts()
    {
        var detected = _services.TextDetection.Detect(
            _state.WebDesign.Design ?? string.Empty);

        _state.WebDesign.DetectedTexts = detected;

    }
    public void DetectEdges()
    {
        var detected = _services.EdgeDetection.Detect(
            _state.WebDesign.Design ?? string.Empty,
            _state.WebDesign.DetectedImages,
            _state.WebDesign.DetectedTexts);

        _state.WebDesign.DetectedContainers = detected;
    }

    public string? DetectElements()
    {
        _state.WebDesign.AnnotatedDesign = _services.ElementDetection.Annotate(
            _state.WebDesign.Design ?? string.Empty,
            _state.WebDesign.DetectedImages,
            _state.WebDesign.DetectedTexts,
            _state.WebDesign.DetectedContainers);

        return _state.WebDesign.AnnotatedDesign;
    }
    public async Task ColorBackground()
    {
        var backgroundColor =
            _services.StyleDetection.DetectBackgroundColor(
                _state.WebDesign.Design ?? string.Empty);

        var html = _state.WebPage.Html;
        var css = _state.WebPage.Css;

        css = AddOrUpdateBodyBackground(
            css,
            backgroundColor);

        _state.WebPage.Css = css;

        await _services.Render.UpdatePage(
            html,
            css);
    }
    private static string AddOrUpdateBodyBackground(
    string css,
    Scalar color)
    {
        var rgb = $"{(int)color.Val2}, " +
                  $"{(int)color.Val1}, " +
                  $"{(int)color.Val0}";

        var background = $"background-color: rgb({rgb});";

        var bodyMatch = Regex.Match(
            css,
            @"body\s*\{(?<content>[^}]*)\}",
            RegexOptions.IgnoreCase);

        if (!bodyMatch.Success)
        {
            return $"body {{ {background} }}\n{css}";
        }

        var bodyContent = bodyMatch.Groups["content"].Value;

        if (Regex.IsMatch(
                bodyContent,
                @"background-color\s*:",
                RegexOptions.IgnoreCase))
        {
            bodyContent = Regex.Replace(
                bodyContent,
                @"background-color\s*:\s*[^;]+;?",
                background,
                RegexOptions.IgnoreCase);
        }
        else
        {
            bodyContent += $"\n    {background}\n";
        }

        return css.Remove(
                bodyMatch.Groups["content"].Index,
                bodyMatch.Groups["content"].Length)
            .Insert(
                bodyMatch.Groups["content"].Index,
                bodyContent);
    }

    public void ExportApp()
    {
        _services.Export.ExportApp();
    }
}
