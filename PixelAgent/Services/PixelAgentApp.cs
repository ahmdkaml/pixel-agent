using PixelAgent.Models;

namespace PixelAgent.Services;

public class PixelAgentApp
{
    public readonly ExportService _exportService;
    public readonly DesignImageService _designService;
    public readonly RenderService _renderService;

    public PixelAgentApp(
        DesignImageService designService,
        RenderService renderService,
        ExportService exportService)
    {
        _designService = designService;
        _renderService = renderService;
        _exportService = exportService;
    }
}
