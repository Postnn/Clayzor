using MudBlazor;

namespace Kesco.Lib.Web.BZ.Controls.Themes;

/// <summary>
/// Корпоративная тема в стиле Lufthansa: глубокий тёмно-синий, золотой акцент, чистый белый.
/// </summary>
public static class KescoTheme
{
    /// <summary>
    /// Создаёт и возвращает настроенную тему MudBlazor в корпоративном стиле.
    /// Цвета берутся из <see cref="KescoColors"/> — единого источника brand-значений.
    /// </summary>
    /// <returns>Готовая тема <see cref="MudTheme"/>.</returns>
    public static MudTheme Create() => new()
    {
        PaletteLight = new PaletteLight
        {
            // Core
            Primary = KescoColors.Navy,
            PrimaryDarken = KescoColors.NavyDark,
            PrimaryLighten = KescoColors.NavyLight,
            Secondary = KescoColors.Gold,
            SecondaryDarken = KescoColors.GoldDark,
            Tertiary = KescoColors.BlueMid,

            // App bar
            AppbarBackground = KescoColors.Navy,
            AppbarText = KescoColors.White,

            // Background & Surface
            Background = KescoColors.OffWhite,
            Surface = KescoColors.White,
            BackgroundGray = KescoColors.OffWhite,

            // Drawer / Sidebar
            DrawerBackground = KescoColors.Navy2,
            DrawerText = KescoColors.DrawerTextColor,
            DrawerIcon = KescoColors.DrawerIconColor,

            // Text
            TextPrimary = KescoColors.TextDark,
            TextSecondary = KescoColors.GreyDark,
            TextDisabled = KescoColors.GreyMid,

            // Actions
            ActionDefault = KescoColors.Navy,
            ActionDisabled = KescoColors.ActionDisabledColor,
            ActionDisabledBackground = KescoColors.GreyLight,

            // Semantic
            Error = KescoColors.ErrorRed,
            Warning = KescoColors.Gold,
            Info = KescoColors.BlueMid,
            Success = KescoColors.SuccessGreen,

            // Table
            TableLines = KescoColors.GreyLight,
            TableStriped = KescoColors.TableStripedColor,
            TableHover = KescoColors.TableHoverColor,

            // Misc
            Divider = KescoColors.GreyLight,
            DividerLight = KescoColors.GreyLight,
            LinesDefault = KescoColors.GreyLight,
            LinesInputs = KescoColors.LinesInputsColor,
        },

        PaletteDark = new PaletteDark
        {
            Primary = "#4A7CFF",
            Secondary = "#FFAD00",
            AppbarBackground = "#0B1529",
            Surface = "#1A1F33",
            Background = "#0E1220",
            BackgroundGray = "#161B2E",
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
                FontFamily = ["var(--kesco-font-family)"],
                FontSize = "var(--kesco-font-size)",
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
                FontSize = "var(--kesco-font-size)",
                LineHeight = "1.6",
            },
            Body2 = new Body2Typography
            {
                FontSize = "var(--kesco-font-size)",
                LineHeight = "1.5",
            },
            Subtitle1 = new Subtitle1Typography
            {
                FontSize = "var(--kesco-font-size)",
            },
            Subtitle2 = new Subtitle2Typography
            {
                FontSize = "var(--kesco-font-size)",
            },
            Caption = new CaptionTypography
            {
                FontSize = "var(--kesco-font-size)",
            },
            Overline = new OverlineTypography
            {
                FontSize = "var(--kesco-font-size)",
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
