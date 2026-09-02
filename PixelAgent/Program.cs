using Microsoft.Web.WebView2.WinForms;

namespace PixelAgent;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        var form = new Form
        {
            Icon = new Icon("icon.ico"),
            Text = "Pixel Agent",
            Width = 1000,
            Height = 700
        };

        var webView = new WebView2
        {
            Dock = DockStyle.Fill
        };

        form.Controls.Add(webView);

        form.Shown += async (_, _) =>
        {
            try
            {
                await webView.EnsureCoreWebView2Async();

                var uiPath = Path.Combine(
                    AppContext.BaseDirectory,
                    "pixel-agent-ui",
                    "index.html"
                );

                webView.Source = new Uri(uiPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.ToString(),
                    "Pixel Agent Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        };

        Application.Run(form);
    }
}
