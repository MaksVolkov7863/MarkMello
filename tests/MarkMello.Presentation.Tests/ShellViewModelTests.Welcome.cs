using MarkMello.Domain;
using MarkMello.Presentation.ViewModels;

namespace MarkMello.Presentation.Tests;

public sealed partial class ShellViewModelTests
{
    [Fact]
    public void SupportLabelRefreshesWhenTheSelectedLanguageChanges()
    {
        var harness = CreateHarness();
        var names = new List<string?>();
        harness.ViewModel.PropertyChanged += (_, e) => names.Add(e.PropertyName);

        Assert.Equal("Support MarkMello", harness.ViewModel.WelcomeSupport);

        harness.ViewModel.SelectedLanguageOption = harness.ViewModel.LanguageOptions
            .Single(option => option.Language == AppLanguage.Russian);

        Assert.Equal("Поддержать MarkMello", harness.ViewModel.WelcomeSupport);
        Assert.Contains(nameof(ShellViewModel.WelcomeSupport), names);

        names.Clear();
        harness.ViewModel.SelectedLanguageOption = harness.ViewModel.LanguageOptions
            .Single(option => option.Language == AppLanguage.English);

        Assert.Equal("Support MarkMello", harness.ViewModel.WelcomeSupport);
        Assert.Contains(nameof(ShellViewModel.WelcomeSupport), names);
    }
}
