using System.Text.Json;
using Microsoft.Web.WebView2.WinForms;
using PixelAgent.Models;

namespace PixelAgent.Services;

public class RenderService
{
    public async Task RenderPage(WebView2 webView, WebPage page)
    {
        var html = JsonSerializer.Serialize(page.Html);
        var css = JsonSerializer.Serialize(page.Css);

        var script = $"window.setRenderedContent({html}, {css});";

        await webView.CoreWebView2.ExecuteScriptAsync(script);
    }
}
