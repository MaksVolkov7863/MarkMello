using CommunityToolkit.Mvvm.ComponentModel;
using MarkMello.Domain;
using MarkMello.Presentation.Localization;
using MarkMello.Presentation.Services;

namespace MarkMello.Presentation.ViewModels;

public sealed record DocumentFontOption(string Label, string? FamilyName);

public sealed class DocumentFontSetting : ObservableObject
{
    private readonly FontFamilyMode _mode;
    private readonly Func<ReadingPreferences> _read;
    private readonly Action<ReadingPreferences> _apply;
    private readonly ILocalizationService _localization;
    private readonly List<DocumentFontOption> _options;

    internal DocumentFontSetting(
        FontFamilyMode mode,
        Func<ReadingPreferences> read,
        Action<ReadingPreferences> apply,
        ILocalizationService localization)
    {
        _mode = mode;
        _read = read;
        _apply = apply;
        _localization = localization;
        var catalog = DocumentFontCatalog.Shared;
        var names = mode == FontFamilyMode.Mono ? catalog.MonospacedNames : catalog.FamilyNames;
        _options = [new(DocumentFontCatalog.BuiltInName(mode), null)];
        _options.AddRange(names
            .Where(name => !string.Equals(name, DocumentFontCatalog.BuiltInName(mode), StringComparison.OrdinalIgnoreCase))
            .Select(name => new DocumentFontOption(name, name)));
        Refresh();
    }

    public string Label => _localization[_mode switch
    {
        FontFamilyMode.Sans => "ReadingFontSans",
        FontFamilyMode.Mono => "ReadingFontMono",
        _ => "ReadingFontSerif"
    }];

    public IReadOnlyList<DocumentFontOption> Options => _options;

    public DocumentFontOption? SelectedOption
    {
        get => _options.FirstOrDefault(option => string.Equals(
            option.FamilyName, _read().GetFontFamilyName(_mode), StringComparison.OrdinalIgnoreCase));
        set
        {
            if (value is not null)
            {
                _apply(_read().WithFontFamilyName(_mode, value.FamilyName));
            }
        }
    }

    public string Hint => DocumentFontCatalog.Shared.Find(_read().GetFontFamilyName(_mode)) is null
        && _read().GetFontFamilyName(_mode) is not null
            ? _localization.Format("AdvancedFontUnavailable", DocumentFontCatalog.BuiltInName(_mode))
            : string.Empty;

    public void Refresh()
    {
        var selectedName = _read().GetFontFamilyName(_mode);
        if (selectedName is not null && !_options.Any(option => string.Equals(
            option.FamilyName, selectedName, StringComparison.OrdinalIgnoreCase)))
        {
            _options.Add(new DocumentFontOption(selectedName, selectedName));
            OnPropertyChanged(nameof(Options));
        }

        OnPropertyChanged(nameof(Label));
        OnPropertyChanged(nameof(Hint));
        OnPropertyChanged(nameof(SelectedOption));
    }
}
