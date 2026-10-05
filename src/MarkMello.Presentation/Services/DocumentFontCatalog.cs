using Avalonia.Media;
using MarkMello.Domain;

namespace MarkMello.Presentation.Services;

/// <summary>Loads installed fonts only when custom typography is first requested.</summary>
internal sealed class DocumentFontCatalog
{
    private readonly Dictionary<string, FontFamily> _systemFonts;
    private IReadOnlyList<string>? _monospacedNames;

    public static DocumentFontCatalog Shared => SharedCatalog.Value;
    private static readonly Lazy<DocumentFontCatalog> SharedCatalog = new(() => new());

    private DocumentFontCatalog()
    {
        _systemFonts = FontManager.Current.SystemFonts
            .GroupBy(font => font.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        FamilyNames = _systemFonts.Keys.Order(StringComparer.CurrentCultureIgnoreCase).ToArray();
    }

    public IReadOnlyList<string> FamilyNames { get; }

    public IReadOnlyList<string> MonospacedNames => _monospacedNames ??= FamilyNames
        .Where(name => FontManager.Current.TryGetGlyphTypeface(new Typeface(_systemFonts[name]), out var glyph)
            && glyph.Metrics.IsFixedPitch)
        .ToArray();

    public static string BuiltInName(FontFamilyMode mode) => mode switch
    {
        FontFamilyMode.Sans => "Inter Tight",
        FontFamilyMode.Mono => "JetBrains Mono",
        _ => "Source Serif 4"
    };

    public static string ResourceKey(FontFamilyMode mode) => mode switch
    {
        FontFamilyMode.Sans => "MmDocumentSansFontFamily",
        FontFamilyMode.Mono => "MmDocumentMonoFontFamily",
        _ => "MmDocumentSerifFontFamily"
    };

    public FontFamily? Find(string? name)
        => name is not null && _systemFonts.TryGetValue(name, out var font) ? font : null;
}
