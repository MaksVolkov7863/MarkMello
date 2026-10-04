using System.Globalization;
using Avalonia.Controls;
using MarkMello.Domain;

namespace MarkMello.Presentation.Services;

/// <summary>Scales interface text while preserving the baseline size hierarchy.</summary>
internal static class InterfaceTypography
{
    private static readonly double[] BaselineSizes = [10, 10.5, 11, 12, 12.5, 13, 14, 15, 20, 26, 44];

    public static void Apply(IResourceDictionary resources, int fontSize)
    {
        var normalizedSize = Math.Clamp(fontSize, ReadingPreferences.MinInterfaceFontSize, ReadingPreferences.MaxInterfaceFontSize);
        var scale = normalizedSize / (double)ReadingPreferences.DefaultInterfaceFontSize;
        if (resources.TryGetResource("MmInterfaceFontSize", null, out var current) && Equals(current, (double)normalizedSize))
        {
            return;
        }

        resources["MmInterfaceFontSize"] = (double)normalizedSize;
        foreach (var baselineSize in BaselineSizes)
        {
            var suffix = baselineSize.ToString(CultureInfo.InvariantCulture).Replace('.', '_');
            resources[$"MmUiFontSize{suffix}"] = (baselineSize + 1) * scale;
        }

        resources["MmReadingSettingsWidth"] = 420 * scale;
    }
}
