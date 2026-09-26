using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace MarkMello.Infrastructure.Diagrams.Sequence;

internal static partial class SequenceSvgGeometry
{
    public static double Number(XElement element, string attribute)
        => double.Parse(element.Attribute(attribute)!.Value, CultureInfo.InvariantCulture);

    public static void Set(XElement element, string attribute, double value)
        => element.SetAttributeValue(attribute, Format(value));

    public static string Format(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    public static void Transform(XElement element, Func<double, double> mapX, Func<double, double> mapY)
    {
        foreach (var name in new[] { "x", "x1", "x2", "cx" })
        {
            if (element.Attribute(name) is not null)
            {
                Set(element, name, mapX(Number(element, name)));
            }
        }

        foreach (var name in new[] { "y", "y1", "y2", "cy" })
        {
            if (element.Attribute(name) is not null)
            {
                Set(element, name, mapY(Number(element, name)));
            }
        }

        if (element.Attribute("points") is { } points)
        {
            points.Value = string.Join(' ', points.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(point => TransformPoint(point, mapX, mapY)));
        }

        if (element.Attribute("d") is { } path)
        {
            path.Value = PathPointPattern().Replace(path.Value, match => match.Groups[1].Value
                + TransformPoint(match.Groups[2].Value + "," + match.Groups[3].Value, mapX, mapY));
        }
    }

    private static string TransformPoint(string point, Func<double, double> mapX, Func<double, double> mapY)
    {
        var values = point.Split(',');
        return Format(mapX(double.Parse(values[0], CultureInfo.InvariantCulture))) + ","
            + Format(mapY(double.Parse(values[1], CultureInfo.InvariantCulture)));
    }

    [GeneratedRegex(@"([ML])(-?\d+(?:\.\d+)?),(-?\d+(?:\.\d+)?)", RegexOptions.CultureInvariant)]
    private static partial Regex PathPointPattern();
}
