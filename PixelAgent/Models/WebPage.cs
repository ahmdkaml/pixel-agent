namespace PixelAgent.Models;

public class WebPage
{
    public string Html { get; set; } = """
        <!DOCTYPE html>
        <html>
        <head>
        </head>
        <body>
        </body>
        </html>
        """;

    public string Css { get; set; } = """
        html,
        body {
            margin: 0;
            min-height: 100%;
        }

        body {
            background: white;
        }
        """;
}
