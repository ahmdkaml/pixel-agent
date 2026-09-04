using Microsoft.Playwright;

namespace PixelAgent.Services;

public class PlaywrightScreenshotService
{
    public async Task<string> Capture(string srcdoc, int width, int height)
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
                }
            }
        );

        await page.SetContentAsync(srcdoc);

        var bytes = await page.ScreenshotAsync(
            new PageScreenshotOptions
            {
                Type = ScreenshotType.Png
            }
        );

        return $"data:image/png;base64,{Convert.ToBase64String(bytes)}";
    }
}
