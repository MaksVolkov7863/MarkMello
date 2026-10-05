using MarkMello.Presentation.ViewModels;

namespace MarkMello.Presentation.Tests;

public sealed partial class ShellViewModelTests
{
    internal static ShellViewModel CreateAdvancedReadingTestShell() => CreateHarness().ViewModel;

    [Fact]
    public void LetterSpacingChangesTheDocumentPreferencesAndNotifiesTheSettingsBinding()
    {
        var shell = CreateHarness().ViewModel;
        var names = new List<string?>();
        shell.PropertyChanged += (_, e) => names.Add(e.PropertyName);

        shell.LetterSpacingSetting = 0.74;

        Assert.Equal(0.7, shell.ReadingPreferences.LetterSpacing);
        Assert.Equal(0.7, shell.DocumentReadingPreferences.LetterSpacing);
        Assert.Contains(nameof(ShellViewModel.LetterSpacingSetting), names);
        Assert.Contains(nameof(ShellViewModel.LetterSpacingLabel), names);
        Assert.Contains(nameof(ShellViewModel.DocumentReadingPreferences), names);
    }
}
