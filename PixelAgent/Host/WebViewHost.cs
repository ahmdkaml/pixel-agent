using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using PixelAgent.Services;

using System.Text.Json;

namespace PixelAgent.Host;

public class WebViewHost
{
    private readonly WebView2 _webView;

    private readonly DesignImageService _designService;

    private readonly RenderService _renderService;

    public WebViewHost(WebView2 webView, DesignImageService designService, RenderService renderService)
    {
        _webView = webView;
        _designService = designService;
        _renderService = renderService;
    }

    public void Initialize()
    {
        _webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
    }

    private async void OnWebMessageReceived(
        object? sender,
        CoreWebView2WebMessageReceivedEventArgs e)
    {
        var message = e.WebMessageAsJson;

        using var document = JsonDocument.Parse(message);

        var action = document.RootElement
            .GetProperty("action")
            .GetString();

        switch (action)
        {
            case "open_design_dialog":

                var imageData = _designService.OpenDesignDialog();

                if (imageData != null)
                {
                    var script = $"window.setDesignImage({JsonSerializer.Serialize(imageData)});";

                    await _webView.CoreWebView2.ExecuteScriptAsync(script);
                }

                break;
            case "code_changed":
                var html = document.RootElement.GetProperty("html").GetString() ?? "";
                var css = document.RootElement.GetProperty("css").GetString() ?? "";

                await _renderService.UpdatePage(html, css);

                break;
        }
    }
}
