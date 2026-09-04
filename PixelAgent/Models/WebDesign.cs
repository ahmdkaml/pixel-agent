namespace PixelAgent.Models;

public class WebDesign
{
    public string? Design { get; set; }

    public List<DetectedImage> DetectedImages { get; set; } = new();

    public List<DetectedText> DetectedTexts { get; set; } = new();
}
