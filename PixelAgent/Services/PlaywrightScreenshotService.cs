using Microsoft.Playwright;

namespace PixelAgent.Services;

public class PlaywrightScreenshotService : IRenderedScreenshotService
{
    public Task<string> Capture(string html, int width, int height)
    {
        return Capture(html, string.Empty, width, height);
    }

    public async Task<string> Capture(string html, string css, int width, int height)
    {
        var viewportWidth = Math.Max(1, width);
        var viewportHeight = Math.Max(1, height);

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(
            new BrowserTypeLaunchOptions
            {
                Headless = true
            }
        );

        var page = await browser.NewPageAsync(
            new BrowserNewPageOptions
            {
                ViewportSize = new ViewportSize
                {
                    Width = viewportWidth,
                    Height = viewportHeight
                },
                DeviceScaleFactor = 1
            }
        );

        var content = string.IsNullOrWhiteSpace(css)
    ? html
    : $$"""
      <!DOCTYPE html>
      <html>
        <head>
          <meta charset="utf-8">
          <style>
            *, *::before, *::after { box-sizing: border-box; }
            body { margin: 0; padding: 0; }
            {{css}}
          </style>
        </head>
        <body>{{html}}</body>
      </html>
      """;
        await page.SetContentAsync(content);

        var bytes = await page.ScreenshotAsync(
            new PageScreenshotOptions
            {
                Type = ScreenshotType.Png,
                FullPage = false
            }
        );

        await page.CloseAsync();

        return $"data:image/png;base64,{Convert.ToBase64String(bytes)}";
    }
}
