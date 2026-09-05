using PixelAgent.Models;
using Tesseract;

namespace PixelAgent.Services;

public class TextDetectionService
{
    private const float MinimumConfidence = 50f;

    private readonly string _tessDataPath;

    public TextDetectionService()
    {
        _tessDataPath = Path.Combine(
            AppContext.BaseDirectory,
            "tessdata");
    }

    public List<DetectedText> Detect(string design)
    {
        var detectedTexts = new List<DetectedText>();

        if (string.IsNullOrWhiteSpace(design))
        {
            return detectedTexts;
        }

        var imageBytes = ExtractImageBytes(design);

        using var engine = new TesseractEngine(
            _tessDataPath,
            "eng",
            EngineMode.Default);

        using var pix = Pix.LoadFromMemory(imageBytes);
        using var page = engine.Process(pix);

        using var iterator = page.GetIterator();

        iterator.Begin();

        var textCount = 0;
        do
        {
            var text = iterator.GetText(
                PageIteratorLevel.TextLine);

            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            if (!iterator.TryGetBoundingBox(
                    PageIteratorLevel.TextLine,
                    out var bounds))
            {
                continue;
            }

            var confidence = iterator.GetConfidence(
                PageIteratorLevel.TextLine);

            if (confidence < MinimumConfidence)
            {
                continue;
            }

            detectedTexts.Add(new DetectedText
            {
                Id = $"text_{++textCount}",
                Text = text.Trim(),

                X = bounds.X1,
                Y = bounds.Y1,

                Width = bounds.Width,
                Height = bounds.Height,

                Color = "",
                LineSpacing = 0,
                FontWeight = 0,

                // Initial approximation.
                FontSize = bounds.Height
            });
        }
        while (iterator.Next(PageIteratorLevel.TextLine));

        return detectedTexts;
    }

    private static byte[] ExtractImageBytes(string data)
    {
        if (data.StartsWith(
                "data:",
                StringComparison.OrdinalIgnoreCase))
        {
            var commaIndex = data.IndexOf(',');

            if (commaIndex < 0)
            {
                throw new FormatException(
                    "Invalid image data URL.");
            }

            data = data[(commaIndex + 1)..];
        }

        return Convert.FromBase64String(data);
    }
}
