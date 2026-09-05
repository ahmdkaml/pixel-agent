using OpenCvSharp;
using PixelAgent.Models;

namespace PixelAgent.Services;

public class EdgeDetectionService
{
    public List<DetectedContainer> Detect(
        string design,
        List<DetectedImage> images,
        List<DetectedText> texts)
    {
        var detected = new List<DetectedContainer>();

        if (string.IsNullOrWhiteSpace(design))
        {
            return detected;
        }

        using var designMat = LoadImage(design);

        if (designMat.Empty())
        {
            return detected;
        }

        using var working = designMat.Clone();

        MaskImages(working, images);
        MaskTexts(working, texts);

        using var gray = new Mat();
        using var blurred = new Mat();
        using var edges = new Mat();

        Cv2.CvtColor(
            working,
            gray,
            ColorConversionCodes.BGR2GRAY);

        Cv2.GaussianBlur(
            gray,
            blurred,
            new OpenCvSharp.Size(5, 5),
            0);

        Cv2.Canny(
            blurred,
            edges,
            40,
            120);

        using var kernel = Cv2.GetStructuringElement(
            MorphShapes.Rect,
            new OpenCvSharp.Size(5, 5));

        Cv2.MorphologyEx(
            edges,
            edges,
            MorphTypes.Close,
            kernel);

        Cv2.FindContours(
            edges,
            out var contours,
            out _,
            RetrievalModes.External,
            ContourApproximationModes.ApproxSimple);

        var containerCount = 0;

        foreach (var contour in contours)
        {
            var area = Cv2.ContourArea(contour);

            if (area < designMat.Width * designMat.Height * 0.01)
            {
                continue;
            }

            var perimeter = Cv2.ArcLength(
                contour,
                true);

            if (perimeter <= 0)
            {
                continue;
            }

            var approximation = Cv2.ApproxPolyDP(
                        contour,
                        perimeter * 0.02,
                        true);

            if (approximation.Length < 4)
            {
                continue;
            }

            var rect = Cv2.BoundingRect(contour);

            if (rect.Width < 50 || rect.Height < 50)
            {
                continue;
            }

            var rectangleArea =
                rect.Width * rect.Height;

            var fillRatio =
                area / rectangleArea;

            if (fillRatio < 0.60)
            {
                continue;
            }

            detected.Add(new DetectedContainer
            {
                Id = $"div_{++containerCount}",
                X = rect.X,
                Y = rect.Y,
                Width = rect.Width,
                Height = rect.Height
            });
        }

        return detected;
    }

    private static void MaskImages(
        Mat image,
        List<DetectedImage> images)
    {
        foreach (var item in images)
        {
            MaskRegion(
                image,
                item.X,
                item.Y,
                item.Width,
                item.Height);
        }
    }

    private static void MaskTexts(
        Mat image,
        List<DetectedText> texts)
    {
        foreach (var item in texts)
        {
            MaskRegion(
                image,
                item.X,
                item.Y,
                item.Width,
                item.Height);
        }
    }

    private static void MaskRegion(
        Mat image,
        int x,
        int y,
        int width,
        int height)
    {
        var padding = 3;

        var left = Math.Max(0, x - padding);
        var top = Math.Max(0, y - padding);

        var right = Math.Min(
            image.Width,
            x + width + padding);

        var bottom = Math.Min(
            image.Height,
            y + height + padding);

        if (right <= left || bottom <= top)
        {
            return;
        }

        Cv2.Rectangle(
            image,
            new Rect(
                left,
                top,
                right - left,
                bottom - top),
            Scalar.White,
            -1);
    }

    private static Mat LoadImage(string data)
    {
        var bytes = ExtractImageBytes(data);

        return Cv2.ImDecode(
            bytes,
            ImreadModes.Color);
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
