using PixelAgent.Services;

public class PixelAgentServices
{
    public DesignImageService Design { get; }
    public RenderService Render { get; }
    public PlaywrightScreenshotService Screenshot { get; }
    public ExportService Export { get; }
    public SimilarityService Similarity { get; }
    public ImageDetectionService Detection { get; }
    public TextDetectionService TextDetection { get; }
    public EdgeDetectionService EdgeDetection { get; } = new EdgeDetectionService();
    public ElementDetectionService ElementDetection { get; } = new ElementDetectionService();

    public StyleDetectionService StyleDetection { get; } = new StyleDetectionService();

    public PixelAgentServices(
        DesignImageService design,
        RenderService render,
        PlaywrightScreenshotService screenshot,
        ExportService export,
        SimilarityService similarity,
        ImageDetectionService detection,
        TextDetectionService textDetection,
        EdgeDetectionService edgeDetection,
        ElementDetectionService elementDetection,
        StyleDetectionService styleDetection)
    {
        Design = design;
        Render = render;
        Screenshot = screenshot;
        Export = export;
        Similarity = similarity;
        Detection = detection;
        TextDetection = textDetection;
        EdgeDetection = edgeDetection;
        ElementDetection = elementDetection;
        StyleDetection = styleDetection;
    }
}
