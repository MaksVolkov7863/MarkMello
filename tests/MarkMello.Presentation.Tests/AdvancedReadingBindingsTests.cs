using Avalonia.Controls;
using Avalonia.LogicalTree;
using MarkMello.Domain;
using MarkMello.Presentation.Views;

namespace MarkMello.Presentation.Tests;

[Collection(AvaloniaHeadlessTestGroup.Name)]
public sealed class AdvancedReadingBindingsTests(AvaloniaHeadlessFixture fixture)
{
    [Fact]
    public Task SharedTypographyControlsUpdateTheSameReadingPreferencesInBothDirections()
    {
        return fixture.RunAsync(() =>
        {
            var shell = ShellViewModelTests.CreateAdvancedReadingTestShell();
            shell.ReadingPreferences = ReadingPreferences.Default with { LetterSpacing = 0.5 };
            var typography = new ReadingTypographyControlsView();
            var spacing = new ReadingLetterSpacingView();
            var window = new Window
            {
                DataContext = shell,
                Content = new StackPanel { Children = { typography, spacing } }
            };
            window.Show();
            try
            {
                var sliders = typography.GetLogicalDescendants().OfType<Slider>().ToArray();
                var spacingSlider = spacing.GetLogicalDescendants().OfType<Slider>().Single();

                Assert.Equal(18, sliders[0].Value);
                Assert.Equal(1.7, sliders[1].Value);
                Assert.Equal(0.5, spacingSlider.Value);

                sliders[0].Value = 22;
                sliders[1].Value = 1.8;
                spacingSlider.Value = 0.8;

                Assert.Equal(22, shell.ReadingPreferences.FontSize);
                Assert.Equal(1.8, shell.ReadingPreferences.LineHeight);
                Assert.Equal(0.8, shell.ReadingPreferences.LetterSpacing);

                shell.FontSizeSetting = 17;
                shell.LineHeightSetting = 1.6;
                shell.LetterSpacingSetting = 0.3;

                Assert.Equal(17, sliders[0].Value);
                Assert.Equal(1.6, sliders[1].Value);
                Assert.Equal(0.3, spacingSlider.Value);
            }
            finally
            {
                window.Close();
            }

            return Task.CompletedTask;
        });
    }

    [Fact]
    public Task FontSelectorsApplyEachFamilyAndStayInSyncWithPreferences()
    {
        return fixture.RunAsync(() =>
        {
            var shell = ShellViewModelTests.CreateAdvancedReadingTestShell();
            var fonts = new ReadingFontSettingsView();
            fonts.DataContext = shell;
            var items = Assert.IsType<ItemsControl>(fonts.Content);
            Assert.Same(shell.DocumentFonts, items.ItemsSource);
            // The existing headless harness does not materialize list containers.
            // Build the production item template to check its real ComboBox bindings.
            var rows = new StackPanel();
            foreach (var setting in shell.DocumentFonts)
            {
                var row = Assert.IsAssignableFrom<Control>(items.ItemTemplate!.Build(setting));
                row.DataContext = setting;
                rows.Children.Add(row);
            }
            var window = new Window { Content = rows };
            window.Show();
            try
            {
                var selectors = rows.GetLogicalDescendants().OfType<ComboBox>().ToArray();
                Assert.Equal(3, selectors.Length);
                for (var index = 0; index < selectors.Length; index++)
                {
                    var setting = shell.DocumentFonts[index];
                    Assert.Equal(setting.Options[0], selectors[index].SelectedItem);
                    var custom = setting.Options.First(option => option.FamilyName is not null);
                    selectors[index].SelectedItem = custom;
                    Assert.Equal(custom.FamilyName, shell.ReadingPreferences.GetFontFamilyName((FontFamilyMode)index));
                }

                shell.ReadingPreferences = ReadingPreferences.Default;
                for (var index = 0; index < selectors.Length; index++)
                {
                    Assert.Equal(shell.DocumentFonts[index].Options[0], selectors[index].SelectedItem);
                }
            }
            finally
            {
                window.Close();
            }

            return Task.CompletedTask;
        });
    }

    [Fact]
    public Task UnavailableFontKeepsItsNameUntilTheUserSelectsTheBuiltInFallback()
    {
        return fixture.RunAsync(() =>
        {
            var shell = ShellViewModelTests.CreateAdvancedReadingTestShell();
            shell.ReadingPreferences = ReadingPreferences.Default with
            {
                SerifFontFamily = "MarkMello Missing Font",
                SansFontFamily = "Arial"
            };
            var serif = shell.DocumentFonts[0];

            Assert.Equal("MarkMello Missing Font", serif.SelectedOption?.FamilyName);
            Assert.NotEmpty(serif.Hint);

            serif.SelectedOption = serif.Options[0];

            Assert.Null(shell.ReadingPreferences.SerifFontFamily);
            Assert.Equal("Arial", shell.ReadingPreferences.SansFontFamily);
            Assert.Empty(serif.Hint);
            return Task.CompletedTask;
        });
    }
}
