using MudBlazor;

namespace Kesco.Lib.Web.BZ.Controls.Themes;

/// <summary>
/// Корпоративная тема в стиле Lufthansa: глубокий тёмно-синий, золотой акцент, чистый белый.
/// </summary>
public static class KescoTheme
{
    // Lufthansa brand colors
    private const string DarkNavy = "#05164D";
    private const string Navy = "#0A1D3D";
    private const string MidBlue = "#00235F";
    private const string Gold = "#FFAD00";
    private const string GoldDark = "#E69C00";
    private const string White = "#FFFFFF";
    private const string OffWhite = "#F7F8FA";
    private const string LightGrey = "#EBEDF0";
    private const string MidGrey = "#9B9B9B";
    private const string DarkGrey = "#4A4A4A";
    private const string TextDark = "#1A1A2E";
    private const string ErrorRed = "#C62828";
    private const string SuccessGreen = "#2E7D32";

    /// <summary>
    /// Создаёт и возвращает настроенную тему MudBlazor в корпоративном стиле.
    /// </summary>
    /// <returns>Готовая тема <see cref="MudTheme"/>.</returns>
    public static MudTheme Create() => new()
    {
        PaletteLight = new PaletteLight
        {
            // Core
            Primary = DarkNavy,
            PrimaryDarken = "#030F35",
            PrimaryLighten = "#1A2D6B",
            Secondary = Gold,
            SecondaryDarken = GoldDark,
            Tertiary = MidBlue,

            // App bar
            AppbarBackground = DarkNavy,
            AppbarText = White,

            // Background & Surface
            Background = OffWhite,
            Surface = White,

            // Drawer / Sidebar
            DrawerBackground = Navy,
            DrawerText = "#C8CDD8",
            DrawerIcon = "#8A93A8",

            // Text
            TextPrimary = TextDark,
            TextSecondary = DarkGrey,
            TextDisabled = MidGrey,

            // Actions
            ActionDefault = DarkNavy,
            ActionDisabled = "#B4B4B4",
            ActionDisabledBackground = LightGrey,

            // Semantic
            Error = ErrorRed,
            Warning = Gold,
            Info = MidBlue,
            Success = SuccessGreen,

            // Table
            TableLines = LightGrey,
            TableStriped = "#F2F4F7",
            TableHover = "#E8EBF0",

            // Misc
            Divider = LightGrey,
            LinesDefault = LightGrey,
            LinesInputs = "#C4C8D0",
        },

        PaletteDark = new PaletteDark
        {
            Primary = "#4A7CFF",
            Secondary = Gold,
            AppbarBackground = "#0B1529",
            Surface = "#1A1F33",
            Background = "#0E1220",
            DrawerBackground = "#0B1529",
            DrawerText = "#A0A8BF",
            TextPrimary = "#E4E7EF",
            TextSecondary = "#8A93A8",
            TableLines = "#2A3050",
            TableStriped = "#161B2E",
            TableHover = "#1F2540",
            Divider = "#2A3050",
            Error = "#EF5350",
            Warning = "#FFB74D",
            Info = "#42A5F5",
            Success = "#66BB6A",
        },

        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = ["Segoe UI", "Helvetica Neue", "Arial", "sans-serif"],
                FontSize = "0.875rem",
                FontWeight = "400",
                LineHeight = "1.5",
                LetterSpacing = "0.01em",
            },
            H4 = new H4Typography
            {
                FontWeight = "700",
                FontSize = "1.75rem",
                LineHeight = "1.2",
                LetterSpacing = "-0.01em",
            },
            H5 = new H5Typography
            {
                FontWeight = "600",
                FontSize = "1.25rem",
                LineHeight = "1.3",
            },
            H6 = new H6Typography
            {
                FontWeight = "600",
                FontSize = "1.05rem",
                LineHeight = "1.4",
            },
            Body1 = new Body1Typography
            {
                FontSize = "0.9rem",
                LineHeight = "1.6",
            },
            Body2 = new Body2Typography
            {
                FontSize = "0.8125rem",
                LineHeight = "1.5",
            },
            Button = new ButtonTypography
            {
                FontWeight = "600",
                FontSize = "0.8125rem",
                LetterSpacing = "0.04em",
                TextTransform = "uppercase",
            },
        },

        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "2px",
            DrawerWidthLeft = "260px",
        }
    };
}
