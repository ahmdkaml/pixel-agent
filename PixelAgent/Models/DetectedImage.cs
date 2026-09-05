namespace PixelAgent.Models;

public class DetectedImage
{
    public string Id { get; set; } = "";

    public string Name { get; set; } = "";

    public int X { get; set; }
    public int Y { get; set; }

    public int Width { get; set; }
    public int Height { get; set; }
}
