using MarkMello.Domain;

namespace MarkMello.Presentation.ViewModels;

public partial class ShellViewModel
{
    partial void OnReadingPreferencesChanged(ReadingPreferences value)
    {
        RefreshAdvancedReadingProperties();
        RefreshInterfaceTypographyProperties();
        var documentRenderingPreferences = GetDocumentRenderingPreferences(value);
        var documentRenderingPreferencesChanged = documentRenderingPreferences != _documentReadingPreferences;
        _documentReadingPreferences = documentRenderingPreferences;

        if (documentRenderingPreferencesChanged)
        {
            EditorSession?.UpdateReadingPreferences(value);
            OnPropertyChanged(nameof(DocumentReadingPreferences));
        }

        OnPropertyChanged(nameof(SelectedFontFamilyMode));
        OnPropertyChanged(nameof(FontSizeSetting));
        OnPropertyChanged(nameof(LineHeightSetting));
        OnPropertyChanged(nameof(ContentWidthSetting));
        OnPropertyChanged(nameof(DocumentColumnMaxWidth));
        OnPropertyChanged(nameof(FontSizeLabel));
        OnPropertyChanged(nameof(LineHeightLabel));
        OnPropertyChanged(nameof(IsSerifFontSelected));
        OnPropertyChanged(nameof(IsSansFontSelected));
        OnPropertyChanged(nameof(IsMonoFontSelected));
        OnPropertyChanged(nameof(IsNarrowWidthSelected));
        OnPropertyChanged(nameof(IsMediumWidthSelected));
        OnPropertyChanged(nameof(IsWideWidthSelected));
        OnPropertyChanged(nameof(SelectedDocumentMinimapMode));
        OnPropertyChanged(nameof(IsDocumentMinimapAutoSelected));
        OnPropertyChanged(nameof(IsDocumentMinimapOnSelected));
        OnPropertyChanged(nameof(IsDocumentMinimapOffSelected));
    }

    private static ReadingPreferences GetDocumentRenderingPreferences(ReadingPreferences preferences)
    {
        var normalized = ReadingPreferences.Normalize(preferences);
        return normalized with
        {
            DocumentMinimapMode = ReadingPreferences.Default.DocumentMinimapMode,
            InterfaceFontSize = ReadingPreferences.DefaultInterfaceFontSize
        };
    }

    private void ApplyReadingPreferences(ReadingPreferences preferences)
    {
        var normalized = ReadingPreferences.Normalize(preferences);
        if (normalized == ReadingPreferences)
        {
            return;
        }

        ReadingPreferences = normalized;
        PersistReadingPreferences(normalized);
    }

    private void PersistReadingPreferences(ReadingPreferences preferences)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await _settings.SavePreferencesAsync(preferences).ConfigureAwait(false);
            }
            catch
            {
                // Persistence remains best-effort; failed saving of reading
                // preferences must never interrupt the viewer or editor loop.
            }
        });
    }

}
