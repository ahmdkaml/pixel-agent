using PixelAgent.Services;

public class PixelAgentServices
{
    public DesignImageService Design { get; }
    public RenderService Render { get; }
    public PlaywrightScreenshotService Screenshot { get; }
    public ExportService Export { get; }
    public SimilarityService Similarity { get; }
    public ImageDetectionService Detection { get; }

    public PixelAgentServices(
        DesignImageService design,
        RenderService render,
        PlaywrightScreenshotService screenshot,
        ExportService export,
        SimilarityService similarity,
        ImageDetectionService detection)
    {
        Design = design;
        Render = render;
        Screenshot = screenshot;
        Export = export;
        Similarity = similarity;
        Detection = detection;
    }
}
