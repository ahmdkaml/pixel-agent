using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using PixelAgent.Services;

using System.Text.Json;

namespace PixelAgent.Host;

public class WebViewHost
{
    private readonly WebView2 _webView;

    private readonly DesignImageService _designService;

    public WebViewHost(WebView2 webView, DesignImageService designService)
    {
        _webView = webView;
        _designService = designService;
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
        }
    }
}
