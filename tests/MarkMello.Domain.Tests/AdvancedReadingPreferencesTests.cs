namespace MarkMello.Domain.Tests;

public sealed class AdvancedReadingPreferencesTests
{
    [Theory]
    [InlineData(0, ReadingPreferences.MinInterfaceFontSize)]
    [InlineData(ReadingPreferences.DefaultInterfaceFontSize, ReadingPreferences.DefaultInterfaceFontSize)]
    [InlineData(99, ReadingPreferences.MaxInterfaceFontSize)]
    public void InterfaceFontSizeIsClampedWithoutChangingDocumentFontSize(int value, int expected)
    {
        var preferences = (ReadingPreferences.Default with { InterfaceFontSize = value }).Normalize();
        Assert.Equal(expected, preferences.InterfaceFontSize);
        Assert.Equal(ReadingPreferences.Default.FontSize, preferences.FontSize);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(3, 2)]
    [InlineData(0.36, 0.4)]
    [InlineData(double.NaN, 0)]
    [InlineData(double.PositiveInfinity, 0)]
    public void LetterSpacingIsNormalizedToSafeSliderSteps(double value, double expected)
    {
        var preferences = ReadingPreferences.Default with { LetterSpacing = value };

        Assert.Equal(expected, preferences.Normalize().LetterSpacing);
    }

    [Fact]
    public void FontChoicesStayIndependentWhenTheActiveFamilyChanges()
    {
        var preferences = ReadingPreferences.Default
            .WithFontFamilyName(FontFamilyMode.Serif, " Georgia ")
            .WithFontFamilyName(FontFamilyMode.Sans, "Arial")
            .WithFontFamilyName(FontFamilyMode.Mono, "Consolas")
            .Normalize();

        Assert.Equal("Georgia", preferences.GetFontFamilyName(FontFamilyMode.Serif));
        Assert.Equal("Arial", preferences.GetFontFamilyName(FontFamilyMode.Sans));
        Assert.Equal("Consolas", preferences.GetFontFamilyName(FontFamilyMode.Mono));
        Assert.Equal("Arial", (preferences with { FontFamily = FontFamilyMode.Sans }).SansFontFamily);
        Assert.Null((preferences with { SerifFontFamily = "  " }).Normalize().SerifFontFamily);
    }
}
