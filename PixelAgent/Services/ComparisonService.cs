using System.Buffers.Binary;

namespace PixelAgent.Services;

public interface IRenderedScreenshotService
{
    Task<string> Capture(string html, string css, int width, int height);
}

public interface IImageSimilarityService
{
    Task<double> Calculate(string design, string rendered);
}

public class ComparisonService
{
    private readonly IRenderedScreenshotService _renderedScreenshotService;
    private readonly IImageSimilarityService _similarityService;

    public ComparisonService(
        IRenderedScreenshotService renderedScreenshotService,
        IImageSimilarityService similarityService)
    {
        _renderedScreenshotService = renderedScreenshotService;
        _similarityService = similarityService;
    }

    public async Task<ComparisonResult> Compare(
        string design,
        string html,
        string css,
        int designWidth,
        int designHeight)
    {
        if (designWidth <= 0 || designHeight <= 0)
        {
            throw new InvalidOperationException("Design dimensions must be greater than zero.");
        }

        var rendered = await _renderedScreenshotService.Capture(
            html,
            css,
            designWidth,
            designHeight
        );

        var (renderedWidth, renderedHeight) = ImageDataUrlUtilities.GetPngDimensions(rendered);

        if (renderedWidth != designWidth || renderedHeight != designHeight)
        {
            throw new InvalidOperationException("Rendered screenshot dimensions must match design dimensions.");
        }

        var similarity = await _similarityService.Calculate(
            design,
            rendered
        );

        return new ComparisonResult(rendered, similarity);
    }
}

public sealed record ComparisonResult(string RenderedImage, double Similarity);

public static class ImageDataUrlUtilities
{
    public static (int Width, int Height) GetPngDimensions(string dataUrl)
    {
        const string pngPrefix = "data:image/png;base64,";

        if (!dataUrl.StartsWith(pngPrefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Rendered screenshot must be a PNG data URL.");
        }

        var pngBytes = Convert.FromBase64String(dataUrl[pngPrefix.Length..]);

        if (pngBytes.Length < 24)
        {
            throw new InvalidOperationException("Rendered screenshot PNG is invalid.");
        }

        ReadOnlySpan<byte> signature = [137, 80, 78, 71, 13, 10, 26, 10];

        if (!pngBytes.AsSpan(0, 8).SequenceEqual(signature))
        {
            throw new InvalidOperationException("Rendered screenshot PNG signature is invalid.");
        }

        var width = BinaryPrimitives.ReadInt32BigEndian(pngBytes.AsSpan(16, 4));
        var height = BinaryPrimitives.ReadInt32BigEndian(pngBytes.AsSpan(20, 4));

        return (width, height);
    }
}
