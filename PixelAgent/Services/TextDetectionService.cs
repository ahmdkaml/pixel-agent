using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PixelAgent.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Tesseract;
using ImageSharpImage = SixLabors.ImageSharp.Image;

namespace PixelAgent.Services;

public class TextDetectionService
{
    private const float MinimumConfidence = 50f;
    private readonly string _tessDataPath;

    public TextDetectionService()
    {
        _tessDataPath = Path.Combine(AppContext.BaseDirectory, "tessdata");
    }

    public List<DetectedText> Detect(string design)
    {
        var rawLines = new List<DetectedText>();

        if (string.IsNullOrWhiteSpace(design))
        {
            return rawLines;
        }

        var imageBytes = ExtractImageBytes(design);

        // Load image into ImageSharp for color sampling
        using var sharpImage = ImageSharpImage.Load<Rgba32>(imageBytes);

        using var engine = new TesseractEngine(_tessDataPath, "eng", EngineMode.Default);
        using var pix = Pix.LoadFromMemory(imageBytes);
        using var page = engine.Process(pix);
        using var iterator = page.GetIterator();

        iterator.Begin();

        var textCount = 0;
        do
        {
            var text = iterator.GetText(PageIteratorLevel.TextLine);

            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            if (!iterator.TryGetBoundingBox(PageIteratorLevel.TextLine, out var bounds))
            {
                continue;
            }

            var confidence = iterator.GetConfidence(PageIteratorLevel.TextLine);
            if (confidence < MinimumConfidence)
            {
                continue;
            }

            var hexColor = ExtractTextColor(sharpImage, bounds);

            rawLines.Add(new DetectedText
            {
                Id = $"line_{++textCount}",
                Text = text.Trim(),
                X = bounds.X1,
                Y = bounds.Y1,
                Width = bounds.Width,
                Height = bounds.Height,
                Color = hexColor,
                LineSpacing = 0,
                FontWeight = 400,
                FontSize = bounds.Height
            });
        }
        while (iterator.Next(PageIteratorLevel.TextLine));

        return GroupDetectedTexts(rawLines);
    }

    private static List<DetectedText> GroupDetectedTexts(List<DetectedText> items)
    {
        if (items.Count <= 1)
        {
            return items;
        }

        var sorted = items
            .OrderBy(t => t.Y)
            .ThenBy(t => t.X)
            .ToList();

        var clusters = new List<List<DetectedText>>();

        foreach (var item in sorted)
        {
            List<DetectedText>? matchedCluster = null;

            foreach (var cluster in clusters)
            {
                // Anchor to the lowest element in the cluster
                var anchor = cluster.OrderByDescending(c => c.Y + c.Height).First();

                if (CanGroup(anchor, item))
                {
                    matchedCluster = cluster;
                    break;
                }
            }

            if (matchedCluster != null)
            {
                matchedCluster.Add(item);
            }
            else
            {
                clusters.Add(new List<DetectedText> { item });
            }
        }

        return clusters.Select((cluster, index) => MergeCluster(cluster, index + 1)).ToList();
    }

    private static bool CanGroup(DetectedText a, DetectedText b)
    {
        // 1. Color check
        if (!ColorsMatch(a.Color, b.Color, tolerance: 35.0))
        {
            return false;
        }

        // 2. Font Size check (allow ±30% deviation)
        double avgSize = (a.FontSize + b.FontSize) / 2.0;
        if (Math.Abs(a.FontSize - b.FontSize) > avgSize * 0.30)
        {
            return false;
        }

        // 3. Font Weight check
        if (a.FontWeight != b.FontWeight)
        {
            return false;
        }

        // 4. Proximity check
        int verticalGap = b.Y - (a.Y + a.Height);
        int maxVerticalGap = (int)Math.Round(a.FontSize * 2.2);

        if (verticalGap < -4 || verticalGap > maxVerticalGap)
        {
            return false;
        }

        int horizontalOverlap = Math.Min(a.X + a.Width, b.X + b.Width) - Math.Max(a.X, b.X);
        double aCenter = a.X + (a.Width / 2.0);
        double bCenter = b.X + (b.Width / 2.0);
        double centerOffset = Math.Abs(aCenter - bCenter);
        int leftOffset = Math.Abs(a.X - b.X);

        return horizontalOverlap > 0 ||
               centerOffset <= (Math.Max(a.Width, b.Width) * 0.5) ||
               leftOffset <= (int)Math.Round(a.FontSize * 3.0);
    }

    private static DetectedText MergeCluster(List<DetectedText> cluster, int idIndex)
    {
        if (cluster.Count == 1)
        {
            var single = cluster[0];
            single.Id = $"text_{idIndex}";
            return single;
        }

        int minX = cluster.Min(c => c.X);
        int minY = cluster.Min(c => c.Y);
        int maxX = cluster.Max(c => c.X + c.Width);
        int maxY = cluster.Max(c => c.Y + c.Height);

        int totalTextHeight = cluster.Sum(c => c.Height);
        int totalVerticalGap = (maxY - minY) - totalTextHeight;

        int lineSpacing = cluster.Count > 1
            ? (int)Math.Round((double)totalVerticalGap / (cluster.Count - 1))
            : 0;

        return new DetectedText
        {
            Id = $"text_{idIndex}",
            Text = string.Join(" ", cluster.Select(c => c.Text)),
            X = minX,
            Y = minY,
            Width = maxX - minX,
            Height = maxY - minY,
            Color = cluster[0].Color,
            FontSize = (int)Math.Round(cluster.Average(c => (double)c.FontSize)),
            FontWeight = (int)Math.Round(cluster.Average(c => (double)c.FontWeight)),
            LineSpacing = lineSpacing
        };
    }
    private static string ExtractTextColor(Image<Rgba32> image, Rect bounds)
    {
        var startX = Math.Clamp(bounds.X1, 0, image.Width - 1);
        var startY = Math.Clamp(bounds.Y1, 0, image.Height - 1);
        var width = Math.Clamp(bounds.Width, 1, image.Width - startX);
        var height = Math.Clamp(bounds.Height, 1, image.Height - startY);

        // Sample pixels to find the darkest/most prominent text color
        var pixelColors = new List<Rgba32>();
        for (var y = startY; y < startY + height; y += 2)
        {
            for (var x = startX; x < startX + width; x += 2)
            {
                pixelColors.Add(image[x, y]);
            }
        }

        if (pixelColors.Count == 0)
        {
            return "#000000";
        }

        // Drop typical white/near-white card backgrounds to isolate text strokes
        var textPixels = pixelColors
            .Where(c => (c.R + c.G + c.B) / 3 < 235)
            .ToList();

        var targetColor = textPixels.Count > 0
            ? textPixels.OrderBy(c => (c.R + c.G + c.B) / 3).First()
            : pixelColors.First();

        return $"#{targetColor.R:X2}{targetColor.G:X2}{targetColor.B:X2}";
    }

    private static bool ColorsMatch(string hex1, string hex2, double tolerance)
    {
        if (string.Equals(hex1, hex2, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.IsNullOrEmpty(hex1) || string.IsNullOrEmpty(hex2) || hex1.Length < 7 || hex2.Length < 7)
        {
            return false;
        }

        var r1 = Convert.ToInt32(hex1.Substring(1, 2), 16);
        var g1 = Convert.ToInt32(hex1.Substring(3, 2), 16);
        var b1 = Convert.ToInt32(hex1.Substring(5, 2), 16);

        var r2 = Convert.ToInt32(hex2.Substring(1, 2), 16);
        var g2 = Convert.ToInt32(hex2.Substring(3, 2), 16);
        var b2 = Convert.ToInt32(hex2.Substring(5, 2), 16);

        var distance = Math.Sqrt(Math.Pow(r1 - r2, 2) + Math.Pow(g1 - g2, 2) + Math.Pow(b1 - b2, 2));
        return distance <= tolerance;
    }

    private static byte[] ExtractImageBytes(string data)
    {
        if (data.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            var commaIndex = data.IndexOf(',');
            if (commaIndex < 0)
            {
                throw new FormatException("Invalid image data URL.");
            }
            data = data[(commaIndex + 1)..];
        }

        return Convert.FromBase64String(data);
    }
}
