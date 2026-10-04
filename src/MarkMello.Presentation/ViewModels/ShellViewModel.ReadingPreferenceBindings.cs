using MarkMello.Domain;

namespace MarkMello.Presentation.ViewModels;

public partial class ShellViewModel
{
    public FontFamilyMode SelectedFontFamilyMode
    {
        get => ReadingPreferences.FontFamily;
        set
        {
            if (ReadingPreferences.FontFamily == value)
            {
                return;
            }

            ApplyReadingPreferences(ReadingPreferences with { FontFamily = value });
        }
    }

    public double FontSizeSetting
    {
        get => ReadingPreferences.FontSize;
        set
        {
            var fontSize = (int)Math.Round(value, MidpointRounding.AwayFromZero);
            if (ReadingPreferences.FontSize == fontSize)
            {
                return;
            }

            ApplyReadingPreferences(ReadingPreferences with { FontSize = fontSize });
        }
    }

    public double LineHeightSetting
    {
        get => ReadingPreferences.LineHeight;
        set
        {
            var normalized = Math.Round(
                value / ReadingPreferences.LineHeightStep,
                MidpointRounding.AwayFromZero) * ReadingPreferences.LineHeightStep;

            if (Math.Abs(ReadingPreferences.LineHeight - normalized) < 0.0001)
            {
                return;
            }

            ApplyReadingPreferences(ReadingPreferences with { LineHeight = normalized });
        }
    }

    public double DocumentColumnMaxWidth => ReadingLayoutMetrics.GetDocumentColumnMaxWidth(ReadingPreferences);

    public double ContentWidthSetting
    {
        get => ReadingPreferences.ContentWidth;
        set
        {
            var contentWidth = (int)Math.Round(
                value / ReadingPreferences.ContentWidthStep,
                MidpointRounding.AwayFromZero) * ReadingPreferences.ContentWidthStep;

            if (ReadingPreferences.ContentWidth == contentWidth)
            {
                return;
            }

            ApplyReadingPreferences(ReadingPreferences with { ContentWidth = contentWidth });
        }
    }

    public string FontSizeLabel => $"{ReadingPreferences.FontSize}px";

    public string LineHeightLabel => ReadingPreferences.LineHeight.ToString("0.00", _localization.Culture);

    public bool IsSerifFontSelected
    {
        get => ReadingPreferences.FontFamily == FontFamilyMode.Serif;
        set
        {
            if (!value)
            {
                OnPropertyChanged(nameof(IsSerifFontSelected));
                return;
            }

            SelectedFontFamilyMode = FontFamilyMode.Serif;
        }
    }

    public bool IsSansFontSelected
    {
        get => ReadingPreferences.FontFamily == FontFamilyMode.Sans;
        set
        {
            if (!value)
            {
                OnPropertyChanged(nameof(IsSansFontSelected));
                return;
            }

            SelectedFontFamilyMode = FontFamilyMode.Sans;
        }
    }

    public bool IsMonoFontSelected
    {
        get => ReadingPreferences.FontFamily == FontFamilyMode.Mono;
        set
        {
            if (!value)
            {
                OnPropertyChanged(nameof(IsMonoFontSelected));
                return;
            }

            SelectedFontFamilyMode = FontFamilyMode.Mono;
        }
    }

    public bool IsNarrowWidthSelected
    {
        get => ReadingPreferences.ContentWidth == ReadingPreferences.NarrowContentWidth;
        set
        {
            if (!value)
            {
                OnPropertyChanged(nameof(IsNarrowWidthSelected));
                return;
            }

            ContentWidthSetting = ReadingPreferences.NarrowContentWidth;
        }
    }

    public bool IsMediumWidthSelected
    {
        get => ReadingPreferences.ContentWidth == ReadingPreferences.MediumContentWidth;
        set
        {
            if (!value)
            {
                OnPropertyChanged(nameof(IsMediumWidthSelected));
                return;
            }

            ContentWidthSetting = ReadingPreferences.MediumContentWidth;
        }
    }

    public bool IsWideWidthSelected
    {
        get => ReadingPreferences.ContentWidth == ReadingPreferences.WideContentWidth;
        set
        {
            if (!value)
            {
                OnPropertyChanged(nameof(IsWideWidthSelected));
                return;
            }

            ContentWidthSetting = ReadingPreferences.WideContentWidth;
        }
    }


}
