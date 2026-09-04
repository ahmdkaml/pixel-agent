using OpenCvSharp;

namespace PixelAgent.Services;

public class StyleDetectionService
{
    public Scalar DetectBackgroundColor(string design)
    {
        if (string.IsNullOrWhiteSpace(design))
        {
            return Scalar.Black;
        }

        try
        {
            var bytes = ExtractImageBytes(design);

            using var image = Cv2.ImDecode(
                bytes,
                ImreadModes.Color);

            if (image.Empty() ||
                image.Width < 2 ||
                image.Height < 2)
            {
                return Scalar.Black;
            }

            var topLeft = image.At<Vec3b>(0, 0);
            var topRight = image.At<Vec3b>(0, image.Width - 1);
            var bottomLeft = image.At<Vec3b>(image.Height - 1, 0);
            var bottomRight = image.At<Vec3b>(
                image.Height - 1,
                image.Width - 1);

            if (!ColorsMatch(topLeft, topRight) ||
                !ColorsMatch(topLeft, bottomLeft) ||
                !ColorsMatch(topLeft, bottomRight))
            {
                return Scalar.Black;
            }

            return new Scalar(
                topLeft.Item0,
                topLeft.Item1,
                topLeft.Item2);
        }
        catch
        {
            return Scalar.Black;
        }
    }

    private static bool ColorsMatch(
        Vec3b first,
        Vec3b second)
    {
        const int tolerance = 5;

        return Math.Abs(first.Item0 - second.Item0) <= tolerance &&
               Math.Abs(first.Item1 - second.Item1) <= tolerance &&
               Math.Abs(first.Item2 - second.Item2) <= tolerance;
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
