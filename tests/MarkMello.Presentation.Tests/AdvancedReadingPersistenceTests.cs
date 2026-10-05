using MarkMello.Domain;
using MarkMello.Infrastructure.Settings;

namespace MarkMello.Presentation.Tests;

public sealed class AdvancedReadingPersistenceTests
{
    [Fact]
    public async Task FontChoicesAndLetterSpacingSurviveReopeningTheSettingsStore()
    {
        var path = Path.Combine(Path.GetTempPath(), "MarkMello.Tests", Guid.NewGuid().ToString("N"));
        try
        {
            var expected = ReadingPreferences.Default with
            {
                SerifFontFamily = "Georgia",
                SansFontFamily = "Arial",
                MonoFontFamily = "Consolas",
                LetterSpacing = 0.7,
                InterfaceFontSize = 17
            };
            await new JsonSettingsStore(path).SavePreferencesAsync(expected);

            Assert.Equal(expected, await new JsonSettingsStore(path).LoadPreferencesAsync());
        }
        finally
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
    }
}
