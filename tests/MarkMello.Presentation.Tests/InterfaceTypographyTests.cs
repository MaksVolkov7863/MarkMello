using Avalonia.Controls;
using Avalonia.LogicalTree;
using MarkMello.Domain;
using MarkMello.Presentation.Services;
using MarkMello.Presentation.Views;

namespace MarkMello.Presentation.Tests;

[Collection(AvaloniaHeadlessTestGroup.Name)]
public sealed class InterfaceTypographyTests(AvaloniaHeadlessFixture fixture)
{
    [Fact]
    public Task InterfaceResourcesKeepTheBaselineAndUpdateExistingTextBindings()
    {
        return fixture.RunAsync(() =>
        {
            var resources = new ResourceDictionary();
            InterfaceTypography.Apply(resources, ReadingPreferences.DefaultInterfaceFontSize);
            Assert.Equal(13d, resources["MmUiFontSize12"]);
            Assert.Equal(420d, resources["MmReadingSettingsWidth"]);
            var text = new TextBlock { Text = "Меню" };
            var window = new Window { Resources = resources, Content = text };
            text.Bind(TextBlock.FontSizeProperty, text.GetResourceObservable("MmUiFontSize12"));
            window.Show();
            try
            {
                InterfaceTypography.Apply(resources, 18);
                Assert.Equal(13d * 18 / ReadingPreferences.DefaultInterfaceFontSize, text.FontSize, 6);
                InterfaceTypography.Apply(resources, ReadingPreferences.DefaultInterfaceFontSize);
                Assert.Equal(13, text.FontSize);
            }
            finally
            {
                window.Close();
            }
            return Task.CompletedTask;
        });
    }

    [Fact]
    public Task InterfaceSliderAndResetSynchronizeWithoutChangingDocumentTypography()
    {
        return fixture.RunAsync(() =>
        {
            var shell = ShellViewModelTests.CreateAdvancedReadingTestShell();
            var interfaceSettings = new InterfaceFontSizeSettingsView();
            var window = new Window { DataContext = shell, Content = interfaceSettings };
            window.Show();
            try
            {
                var documentPreferences = shell.DocumentReadingPreferences;
                var slider = interfaceSettings.GetLogicalDescendants().OfType<Slider>().Single();
                slider.Value = 18;
                Assert.Equal(18, shell.ReadingPreferences.InterfaceFontSize);
                Assert.Equal(documentPreferences, shell.DocumentReadingPreferences);

                shell.ReadingPreferences = shell.ReadingPreferences with
                {
                    SerifFontFamily = "Georgia", SansFontFamily = "Arial", MonoFontFamily = "Consolas",
                    FontSize = 24, LineHeight = 2, LetterSpacing = 1.5, ContentWidth = 1080,
                    DocumentMinimapMode = DocumentMinimapMode.On
                };
                shell.ResetReadingSettingsCommand.Execute(null);

                Assert.Equal(ReadingPreferences.Default, shell.ReadingPreferences);
                Assert.Equal(ReadingPreferences.DefaultInterfaceFontSize, slider.Value);
            }
            finally
            {
                window.Close();
            }
            return Task.CompletedTask;
        });
    }
}
