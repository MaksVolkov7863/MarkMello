using System.Text.Json;

namespace MarkMello.Infrastructure.Diagrams.Sequence;

internal sealed record SequenceDiagramSettings(double Width = 150, double ActorMargin = 50, double MessageMargin = 35)
{
    public static (string Source, SequenceDiagramSettings Settings) Read(string source)
    {
        var remaining = source.TrimStart();
        var settings = new SequenceDiagramSettings();
        while (remaining.StartsWith("%%{", StringComparison.Ordinal))
        {
            var end = remaining.IndexOf("}%%", StringComparison.Ordinal);
            if (end < 0)
            {
                break;
            }

            var directive = remaining[3..end].Trim();
            if (directive.StartsWith("init:", StringComparison.Ordinal))
            {
                settings = ReadJson(directive[5..], settings);
            }

            remaining = remaining[(end + 3)..].TrimStart();
        }

        return (remaining, settings);
    }

    private static SequenceDiagramSettings ReadJson(string json, SequenceDiagramSettings current)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object
                || !document.RootElement.TryGetProperty("sequence", out var sequence)
                || sequence.ValueKind != JsonValueKind.Object)
            {
                return current;
            }

            return new SequenceDiagramSettings(
                ReadNumber(sequence, "width", current.Width),
                ReadNumber(sequence, "actorMargin", current.ActorMargin),
                ReadNumber(sequence, "messageMargin", current.MessageMargin));
        }
        catch (JsonException)
        {
            // Naiad ignores init directives; malformed configuration keeps its defaults.
            return current;
        }
    }

    private static double ReadNumber(JsonElement element, string name, double fallback)
        => element.TryGetProperty(name, out var value)
            && value.ValueKind == JsonValueKind.Number
            && value.TryGetDouble(out var number)
            && double.IsFinite(number)
            && number > 0
            && number <= 10000
                ? number
                : fallback;
}
