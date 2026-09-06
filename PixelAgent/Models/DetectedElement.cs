using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PixelAgent.Models;

public class DetectedElement
{
    public string Id { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public int X { get; set; }
    public int Y { get; set; }

    public int Width { get; set; }
    public int Height { get; set; }

    public string? Name { get; set; }

    public string? Content { get; set; }

    public string? Color { get; set; }

    public double FontSize { get; set; }

    public int FontWeight { get; set; }

    [JsonIgnore]
    public string? ParentId { get; set; }

    [JsonIgnore]
    public List<string> ChildrenIds { get; set; } = new();

    public List<DetectedElement>? Children { get; set; }
}
