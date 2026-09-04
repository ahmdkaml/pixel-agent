using PixelAgent.Models;

namespace PixelAgent;

public class PixelAgentState
{
    public WebPage WebPage { get; }
    public WebDesign WebDesign { get; }
    public WebAssets WebAssets { get; }

    public PixelAgentState(
        WebPage webPage,
        WebDesign webDesign,
        WebAssets webAssets)
    {
        WebPage = webPage;
        WebDesign = webDesign;
        WebAssets = webAssets;
    }
}
