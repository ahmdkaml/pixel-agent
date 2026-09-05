namespace PixelAgent.Models;

public class DetectedText
{
    public string Id { get; set; } = "";
    public string Text { get; set; } = "";

    public int X { get; set; }
    public int Y { get; set; }

    public int Width { get; set; }
    public int Height { get; set; }

    public string Color { get; set; } = "";

    public double LineSpacing { get; set; }

    public int FontWeight { get; set; }

    public double FontSize { get; set; }

}
