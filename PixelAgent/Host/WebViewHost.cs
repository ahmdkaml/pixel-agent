using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using PixelAgent.Services;

using System.Text.Json;

namespace PixelAgent.Host;

public class WebViewHost
{
    private readonly WebView2 _webView;
    private readonly PixelAgentApp _app;

    public WebViewHost(WebView2 webView, PixelAgentApp app)
    {
        _webView = webView;
        _app = app;
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

                var imageData = _app.OpenDesignDialog();

                if (imageData != null)
                {
                    await _webView.CoreWebView2.ExecuteScriptAsync(
                        $"window.setDesignImage({JsonSerializer.Serialize(imageData)});");
                }

                _app.DetectTexts();
                _app.DetectEdges();
                _app.DetectImages();

                var elements = _app.DetectElements();

                await _webView.CoreWebView2.ExecuteScriptAsync(
                    $"window.setDetectedElements({JsonSerializer.Serialize(elements)});");

                break;


            case "load_images":

                var images = _app.LoadImages();

                if (images != null)
                {
                    var script = $"window.addImages({JsonSerializer.Serialize(images)});";
                    await _webView.CoreWebView2.ExecuteScriptAsync(script);
                }
                break;


            case "color_background":
                await _app.ColorBackground();
                break;

            case "code_changed":
                var html = document.RootElement.GetProperty("html").GetString() ?? "";
                var css = document.RootElement.GetProperty("css").GetString() ?? "";

                await _app.UpdatePage(html, css);

                break;
            case "export_app":
                _app.ExportApp();
                break;
        }
    }
}
