using MarkMello.Domain;

namespace MarkMello.Presentation.ViewModels;

public partial class ShellViewModel
{
    private IReadOnlyList<DocumentFontSetting>? _documentFonts;

    public IReadOnlyList<DocumentFontSetting> DocumentFonts => _documentFonts ??=
        Enum.GetValues<FontFamilyMode>()
            .Select(mode => new DocumentFontSetting(mode, () => ReadingPreferences, ApplyReadingPreferences, _localization))
            .ToArray();

    public string AdvancedReadingLabel => _localization["AdvancedReadingLabel"];
    public string AdvancedReadingHeader => _localization["AdvancedReadingHeader"];
    public string ReadingLetterSpacingLabel => _localization["ReadingLetterSpacingLabel"];
    public string ReadingLetterSpacingHint => _localization["ReadingLetterSpacingHint"];
    public string LetterSpacingLabel => $"{ReadingPreferences.LetterSpacing.ToString("0.0", _localization.Culture)}px";

    public double LetterSpacingSetting
    {
        get => ReadingPreferences.LetterSpacing;
        set => ApplyReadingPreferences(ReadingPreferences with { LetterSpacing = value });
    }

    private void RefreshAdvancedReadingProperties()
    {
        OnPropertyChanged(nameof(AdvancedReadingLabel));
        OnPropertyChanged(nameof(AdvancedReadingHeader));
        OnPropertyChanged(nameof(ReadingLetterSpacingLabel));
        OnPropertyChanged(nameof(ReadingLetterSpacingHint));
        OnPropertyChanged(nameof(LetterSpacingSetting));
        OnPropertyChanged(nameof(LetterSpacingLabel));
        if (_documentFonts is not null)
        {
            foreach (var font in _documentFonts)
            {
                font.Refresh();
            }
        }
    }
}
