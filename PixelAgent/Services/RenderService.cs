using System.Text.Json;
using Microsoft.Web.WebView2.WinForms;
using PixelAgent.Models;

namespace PixelAgent.Services;

public class RenderService
{
    private readonly WebView2 _webView;
    private readonly WebPage _webPage;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public RenderService(WebView2 webView, WebPage webPage)
    {
        _webView = webView;
        _webPage = webPage;
    }

    public async Task RenderPage()
    {
        var html = JsonSerializer.Serialize(_webPage.Html);
        var css = JsonSerializer.Serialize(_webPage.Css);

        var script = $"window.setRenderedContent({html}, {css});";

        await _webView.CoreWebView2.ExecuteScriptAsync(script);
    }

    public async Task UpdatePage(string html, string css)
    {
        _webPage.Html = html;
        _webPage.Css = css;

        await RenderPage();
    }

    public async Task<RenderSnapshot> CaptureSnapshot()
    {
        var script = """
            (() => {
              const frame = document.getElementById("renderFrame");
              if (!frame) {
                return { srcdoc: "", width: 1, height: 1 };
              }
            
              const width = Math.max(1, frame.offsetWidth || frame.clientWidth || 1);
              const height = Math.max(1, frame.offsetHeight || frame.clientHeight || 1);
            
              return {
                srcdoc: frame.srcdoc || "",
                width,
                height
              };
            })();
            """;

        var json = await _webView.CoreWebView2.ExecuteScriptAsync(script);
        var snapshot = JsonSerializer.Deserialize<RenderSnapshot>(json, JsonOptions);

        return snapshot ?? new RenderSnapshot("", 1, 1);
    }
}

public sealed record RenderSnapshot(string Srcdoc, int Width, int Height);
