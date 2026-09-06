using System;
using System.Collections.Generic;
using System.Linq;
using PixelAgent.Models;

namespace PixelAgent.Services;

public class ElementDetectionService
{
    public List<DetectedElement> Detect(
        List<DetectedImage> images,
        List<DetectedText> texts,
        List<DetectedContainer> containers)
    {
        var elements = new List<DetectedElement>();

        foreach (var image in images)
        {
            elements.Add(new DetectedElement
            {
                Id = image.Id,
                Type = "image",
                Name = image.Name,
                X = image.X,
                Y = image.Y,
                Width = image.Width,
                Height = image.Height
            });
        }

        foreach (var text in texts)
        {
            elements.Add(new DetectedElement
            {
                Id = text.Id,
                Type = "text",
                Content = text.Text,
                Color = text.Color,
                FontSize = text.FontSize,
                FontWeight = text.FontWeight,
                X = text.X,
                Y = text.Y,
                Width = text.Width,
                Height = text.Height
            });
        }

        foreach (var container in containers)
        {
            elements.Add(new DetectedElement
            {
                Id = container.Id,
                Type = "container",
                X = container.X,
                Y = container.Y,
                Width = container.Width,
                Height = container.Height
            });
        }

        return BuildHierarchyTree(elements);
    }

    private static List<DetectedElement> BuildHierarchyTree(List<DetectedElement> elements)
    {
        var containers = elements.Where(e => e.Type == "container").ToList();

        foreach (var element in elements)
        {
            var parent = containers
                .Where(c => c.Id != element.Id && IsInsideContainer(c, element))
                .OrderBy(c => c.Width * c.Height)
                .FirstOrDefault();

            if (parent != null)
            {
                element.ParentId = parent.Id;
                parent.ChildrenIds.Add(element.Id);

                parent.Children ??= new List<DetectedElement>();
                parent.Children.Add(element);
            }
        }

        foreach (var container in containers.Where(c => c.Children != null))
        {
            container.Children = container.Children!
                .OrderBy(c => c.Y)
                .ThenBy(c => c.X)
                .ToList();
        }

        return elements
            .Where(e => e.ParentId == null)
            .OrderBy(e => e.Y)
            .ThenBy(e => e.X)
            .ToList();
    }

    private static bool IsInsideContainer(DetectedElement container, DetectedElement child)
    {
        const int tolerance = 6;

        bool withinTolerantBounds =
            child.X >= container.X - tolerance &&
            child.Y >= container.Y - tolerance &&
            child.X + child.Width <= container.X + container.Width + tolerance &&
            child.Y + child.Height <= container.Y + container.Height + tolerance;

        if (withinTolerantBounds)
        {
            return true;
        }

        int overlapX1 = Math.Max(container.X, child.X);
        int overlapY1 = Math.Max(container.Y, child.Y);
        int overlapX2 = Math.Min(container.X + container.Width, child.X + child.Width);
        int overlapY2 = Math.Min(container.Y + container.Height, child.Y + child.Height);

        if (overlapX2 > overlapX1 && overlapY2 > overlapY1)
        {
            double overlapArea = (overlapX2 - overlapX1) * (overlapY2 - overlapY1);
            double childArea = child.Width * child.Height;
            return (overlapArea / childArea) >= 0.70;
        }

        return false;
    }
}
