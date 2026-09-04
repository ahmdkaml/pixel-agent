using OpenCvSharp;
using PixelAgent.Models;

namespace PixelAgent.Services;

public class ElementDetectionService
{
    private static readonly Scalar ImageColor =
        new(255, 80, 160); // reddish pink

    private static readonly Scalar TextColor =
        new(0, 255, 255); // yellow

    private static readonly Scalar ContainerColor =
        new(255, 120, 0); // blue

    public string? Annotate(
        string design,
        List<DetectedImage> images,
        List<DetectedText> texts,
        List<DetectedContainer> containers)
    {
        if (string.IsNullOrWhiteSpace(design))
        {
            return null;
        }

        using var designMat = LoadImage(design);

        if (designMat.Empty())
        {
            return null;
        }

        foreach (var image in images)
        {
            DrawBox(
                designMat,
                image.X,
                image.Y,
                image.Width,
                image.Height,
                ImageColor);
        }

        foreach (var text in texts)
        {
            DrawBox(
                designMat,
                text.X,
                text.Y,
                text.Width,
                text.Height,
                TextColor);
        }
        foreach (var container in containers)
        {
            DrawBox(
                designMat,
                container.X,
                container.Y,
                container.Width,
                container.Height,
                ContainerColor);
        }

        return ConvertToDataUrl(designMat);
    }

    private static void DrawBox(
        Mat image,
        int x,
        int y,
        int width,
        int height,
        Scalar color)
    {
        Cv2.Rectangle(
            image,
            new Rect(x, y, width, height),
            color,
            3);
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

    private static string ConvertToDataUrl(Mat image)
    {
        Cv2.ImEncode(
            ".png",
            image,
            out var bytes);

        return $"data:image/png;base64,{Convert.ToBase64String(bytes)}";
    }
}
