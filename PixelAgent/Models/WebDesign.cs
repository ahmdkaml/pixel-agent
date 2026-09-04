namespace PixelAgent.Models;

public class WebDesign
{
    public string? Design { get; set; }

    public string? AnnotatedDesign { get; set; }

    public List<DetectedImage> DetectedImages { get; set; } = new();

    public List<DetectedText> DetectedTexts { get; set; } = new();

    public List<DetectedContainer> DetectedContainers { get; set; } = new();
}
