using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace PixelAgent.Host;

public class WebViewHost
{
    private readonly WebView2 _webView;

    public WebViewHost(WebView2 webView)
    {
        _webView = webView;
    }

    public void Initialize()
    {
        _webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
    }

    private void OnWebMessageReceived(
        object? sender,
        CoreWebView2WebMessageReceivedEventArgs e)
    {
        MessageBox.Show("WebViewHost received a message!");
    }
}
