using System.Text.Json;
using OpenCvSharp;
using PixelAgent.Models;

namespace PixelAgent.Services;

public class ImageDetectionService
{
    private const double MatchThreshold = 0.85;

    private readonly List<DetectedImage> _detectedImages = new();
    private readonly List<object> _undetectedImages = new();

    public List<DetectedImage> Detect(
        string design,
        List<object> images)
    {
        _detectedImages.Clear();
        _undetectedImages.Clear();


        if (string.IsNullOrWhiteSpace(design))
        {
            _undetectedImages.AddRange(images);
            return _detectedImages;
        }

        using var designMat = LoadImage(design);

        if (designMat.Empty())
        {
            _undetectedImages.AddRange(images);
            return _detectedImages;
        }

        foreach (var image in images)
        {
            var asset = ReadImageAsset(image);

            if (asset == null)
            {
                _undetectedImages.Add(image);
                continue;
            }

            using var imageMat = LoadImage(asset.Data);

            if (imageMat.Empty())
            {
                _undetectedImages.Add(image);
                continue;
            }

            var detected = FindImage(
                designMat,
                imageMat,
                asset.Name);

            if (detected != null)
            {
                _detectedImages.Add(detected);
            }
            else
            {
                _undetectedImages.Add(image);
            }
        }

        return _detectedImages;
    }

    public List<object> GetUndetectedImages()
    {
        return _undetectedImages;
    }

    private static DetectedImage? FindImage(
        Mat design,
        Mat image,
        string name)
    {
        using var designGray = new Mat();
        using var imageGray = new Mat();

        Cv2.CvtColor(
            design,
            designGray,
            ColorConversionCodes.BGR2GRAY);

        Cv2.CvtColor(
            image,
            imageGray,
            ColorConversionCodes.BGR2GRAY);

        double bestMatch = double.MinValue;
        OpenCvSharp.Point bestLocation = default;
        int bestWidth = 0;
        int bestHeight = 0;

        // Search from original size down to 20%.
        for (double scale = 1.0; scale >= 0.2; scale -= 0.05)
        {
            var width = (int)(imageGray.Width * scale);
            var height = (int)(imageGray.Height * scale);

            if (width < 10 || height < 10)
            {
                continue;
            }

            if (width > designGray.Width ||
                height > designGray.Height)
            {
                continue;
            }

            using var resizedImage = new Mat();

            Cv2.Resize(
                imageGray,
                resizedImage,
                new OpenCvSharp.Size(width, height));

            var resultWidth =
                designGray.Width - resizedImage.Width + 1;

            var resultHeight =
                designGray.Height - resizedImage.Height + 1;

            using var result = new Mat(
                resultHeight,
                resultWidth,
                MatType.CV_32FC1);

            Cv2.MatchTemplate(
                designGray,
                resizedImage,
                result,
                TemplateMatchModes.CCoeffNormed);

            Cv2.MinMaxLoc(
                result,
                out _,
                out var maxValue,
                out _,
                out var maxLocation);

            if (maxValue > bestMatch)
            {
                bestMatch = maxValue;
                bestLocation = maxLocation;
                bestWidth = width;
                bestHeight = height;
            }
        }

        if (bestMatch < MatchThreshold)
        {
            return null;
        }

        return new DetectedImage
        {
            Name = name,
            X = bestLocation.X,
            Y = bestLocation.Y,
            Width = bestWidth,
            Height = bestHeight
        };
    }
    private static Mat LoadImage(string data)
    {
        try
        {
            var bytes = ExtractImageBytes(data);

            return Cv2.ImDecode(
                bytes,
                ImreadModes.Color);
        }
        catch
        {
            return new Mat();
        }
    }

    private static byte[] ExtractImageBytes(string data)
    {
        if (data.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            var commaIndex = data.IndexOf(',');

            if (commaIndex >= 0)
            {
                data = data[(commaIndex + 1)..];
            }
        }

        return Convert.FromBase64String(data);
    }

    private static ImageAsset? ReadImageAsset(object image)
    {
        try
        {
            var json = JsonSerializer.SerializeToElement(image);

            if (!json.TryGetProperty("name", out var nameProperty) &&
                !json.TryGetProperty("Name", out nameProperty))
            {
                return null;
            }

            if (!json.TryGetProperty("data", out var dataProperty) &&
                !json.TryGetProperty("Data", out dataProperty))
            {
                return null;
            }

            var name = nameProperty.GetString();
            var data = dataProperty.GetString();

            if (string.IsNullOrWhiteSpace(name) ||
                string.IsNullOrWhiteSpace(data))
            {
                return null;
            }

            return new ImageAsset(name, data);
        }
        catch
        {
            return null;
        }
    }

    private sealed record ImageAsset(
        string Name,
        string Data);
}
