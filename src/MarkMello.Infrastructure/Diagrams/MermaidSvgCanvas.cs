using System.Globalization;
using System.Xml.Linq;

namespace MarkMello.Infrastructure.Diagrams;

internal static class MermaidSvgCanvas
{
    public static string AddBackground(string svg)
    {
        var document = XDocument.Parse(svg);
        var root = document.Root!;
        var bounds = root.Attribute("viewBox")!.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        // Naiad's default palette has black labels and pale boxes. Its native SVG needs
        // the same light canvas as Mermaid's default rendering, including in a dark shell.
        var background = new XElement(root.Name.Namespace + "rect",
            new XAttribute("x", bounds[0]), new XAttribute("y", bounds[1]),
            new XAttribute("width", double.Parse(bounds[2], CultureInfo.InvariantCulture)),
            new XAttribute("height", double.Parse(bounds[3], CultureInfo.InvariantCulture)),
            new XAttribute("fill", "white"));
        root.AddFirst(background);
        return document.ToString(SaveOptions.DisableFormatting);
    }
}
