using System.Text.Json;
using Microsoft.Web.WebView2.WinForms;
using PixelAgent.Models;

namespace PixelAgent.Services;

public class RenderService
{
    private readonly WebView2 _webView;
    private readonly WebPage _webPage;

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
}
