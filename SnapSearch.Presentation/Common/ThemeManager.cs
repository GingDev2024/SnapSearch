using System.Windows;
using System.Windows.Media;

namespace SnapSearch.Presentation.Common
{
    public enum AppTheme
    { Dark, Light }

    public static class ThemeManager
    {
        #region Fields

        // ── Colour definitions ──────────────────────────────────────────────
        private static readonly Dictionary<string, System.Windows.Media.Color> DarkColors = new()
        {
            ["PrimaryColor"] = System.Windows.Media.Color.FromRgb(0x1A, 0x1F, 0x2E),
            ["PrimaryLightColor"] = System.Windows.Media.Color.FromRgb(0x25, 0x2C, 0x40),
            ["AccentColor"] = System.Windows.Media.Color.FromRgb(0x4F, 0x8E, 0xF7),
            ["AccentHoverColor"] = System.Windows.Media.Color.FromRgb(0x3A, 0x7A, 0xE4),
            ["SurfaceColor"] = System.Windows.Media.Color.FromRgb(0x1E, 0x24, 0x38),
            ["SurfaceLightColor"] = System.Windows.Media.Color.FromRgb(0x2A, 0x32, 0x50),
            ["BorderColor"] = System.Windows.Media.Color.FromRgb(0x3A, 0x42, 0x68),
            ["TextPrimaryColor"] = System.Windows.Media.Color.FromRgb(0xE8, 0xEC, 0xF5),
            ["TextSecondaryColor"] = Colors.White,
            ["TextMutedColor"] = Colors.Gray,
            ["SuccessColor"] = System.Windows.Media.Color.FromRgb(0x2E, 0xCC, 0x71),
            ["WarningColor"] = System.Windows.Media.Color.FromRgb(0xF3, 0x9C, 0x12),
            ["DangerColor"] = System.Windows.Media.Color.FromRgb(0xE7, 0x4C, 0x3C),
            ["InfoColor"] = System.Windows.Media.Color.FromRgb(0x34, 0x98, 0xDB),
        };

        private static readonly Dictionary<string, System.Windows.Media.Color> LightColors = new()
        {
            ["PrimaryColor"] = System.Windows.Media.Color.FromRgb(0xF0, 0xF4, 0xFF),
            ["PrimaryLightColor"] = System.Windows.Media.Color.FromRgb(0xFF, 0xFF, 0xFF),
            ["AccentColor"] = System.Windows.Media.Color.FromRgb(0x2E, 0x6F, 0xD8),
            ["AccentHoverColor"] = System.Windows.Media.Color.FromRgb(0x1E, 0x5C, 0xC0),
            ["SurfaceColor"] = System.Windows.Media.Color.FromRgb(0xFF, 0xFF, 0xFF),
            ["SurfaceLightColor"] = System.Windows.Media.Color.FromRgb(0xEA, 0xF0, 0xFF),
            ["BorderColor"] = System.Windows.Media.Color.FromRgb(0xC5, 0xD0, 0xE8),
            ["TextPrimaryColor"] = System.Windows.Media.Color.FromRgb(0x0F, 0x14, 0x22),
            ["TextSecondaryColor"] = System.Windows.Media.Color.FromRgb(0x2A, 0x32, 0x52),
            ["TextMutedColor"] = System.Windows.Media.Color.FromRgb(0x7A, 0x85, 0xA0),
            ["SuccessColor"] = System.Windows.Media.Color.FromRgb(0x1A, 0x9E, 0x55),
            ["WarningColor"] = System.Windows.Media.Color.FromRgb(0xC0, 0x78, 0x00),
            ["DangerColor"] = System.Windows.Media.Color.FromRgb(0xC0, 0x39, 0x2B),
            ["InfoColor"] = System.Windows.Media.Color.FromRgb(0x1A, 0x78, 0xC0),
        };

        // Gradient stop pairs: [start, end]
        private static readonly (System.Windows.Media.Color Start, System.Windows.Media.Color End) DarkAccentGradient =
            (System.Windows.Media.Color.FromRgb(0x4F, 0x8E, 0xF7), System.Windows.Media.Color.FromRgb(0x7B, 0x5F, 0xF5));

        private static readonly (System.Windows.Media.Color Start, System.Windows.Media.Color End) LightAccentGradient =
            (System.Windows.Media.Color.FromRgb(0x2E, 0x6F, 0xD8), System.Windows.Media.Color.FromRgb(0x6A, 0x4F, 0xD8));

        private static readonly (System.Windows.Media.Color Start, System.Windows.Media.Color End) DarkSidebarGradient =
            (System.Windows.Media.Color.FromRgb(0x14, 0x19, 0x29), System.Windows.Media.Color.FromRgb(0x1A, 0x20, 0x35));

        private static readonly (System.Windows.Media.Color Start, System.Windows.Media.Color End) LightSidebarGradient =
            (System.Windows.Media.Color.FromRgb(0xDD, 0xE6, 0xFF), System.Windows.Media.Color.FromRgb(0xEA, 0xF0, 0xFF));

        #endregion Fields

        #region Events

        public static event Action<AppTheme>? ThemeChanged;

        #endregion Events

        #region Properties

        public static AppTheme CurrentTheme { get; private set; } = AppTheme.Dark;
        public static bool IsDark => CurrentTheme == AppTheme.Dark;

        #endregion Properties

        #region Public Methods

        // ── Public API ──────────────────────────────────────────────────────
        public static void Toggle() =>
            Apply(CurrentTheme == AppTheme.Dark ? AppTheme.Light : AppTheme.Dark);

        public static void Apply(AppTheme theme)
        {
            CurrentTheme = theme;
            var colors = theme == AppTheme.Dark ? DarkColors : LightColors;
            var accentGrad = theme == AppTheme.Dark ? DarkAccentGradient : LightAccentGradient;
            var sidebarGrad = theme == AppTheme.Dark ? DarkSidebarGradient : LightSidebarGradient;

            var res = System.Windows.Application.Current.Resources;

            // 1. Update Color resources
            foreach (var (key, color) in colors)
                if (res.Contains(key))
                    res[key] = color;

            // 2. Update SolidColorBrush resources in-place so bound elements refresh
            UpdateBrush(res, "PrimaryBrush", colors["PrimaryColor"]);
            UpdateBrush(res, "PrimaryLightBrush", colors["PrimaryLightColor"]);
            UpdateBrush(res, "AccentBrush", colors["AccentColor"]);
            UpdateBrush(res, "AccentHoverBrush", colors["AccentHoverColor"]);
            UpdateBrush(res, "SurfaceBrush", colors["SurfaceColor"]);
            UpdateBrush(res, "SurfaceLightBrush", colors["SurfaceLightColor"]);
            UpdateBrush(res, "BorderBrush", colors["BorderColor"]);
            UpdateBrush(res, "TextPrimaryBrush", colors["TextPrimaryColor"]);
            UpdateBrush(res, "TextSecondaryBrush", colors["TextSecondaryColor"]);
            UpdateBrush(res, "TextMutedBrush", colors["TextMutedColor"]);
            UpdateBrush(res, "SuccessBrush", colors["SuccessColor"]);
            UpdateBrush(res, "WarningBrush", colors["WarningColor"]);
            UpdateBrush(res, "DangerBrush", colors["DangerColor"]);

            // 3. Update LinearGradientBrush resources in-place
            UpdateGradient(res, "AccentGradient", accentGrad.Start, accentGrad.End);
            UpdateGradient(res, "SidebarGradient", sidebarGrad.Start, sidebarGrad.End);

            ThemeChanged?.Invoke(theme);
        }

        #endregion Public Methods

        #region Private Methods

        // ── Helpers ─────────────────────────────────────────────────────────
        private static void UpdateBrush(ResourceDictionary res, string key, System.Windows.Media.Color color)
        {
            if (res[key] is SolidColorBrush brush && !brush.IsFrozen)
                brush.Color = color;
            else
                res[key] = new SolidColorBrush(color);
        }

        private static void UpdateGradient(ResourceDictionary res, string key,
                                           System.Windows.Media.Color start, System.Windows.Media.Color end)
        {
            if (res[key] is LinearGradientBrush grad && !grad.IsFrozen)
            {
                grad.GradientStops[0].Color = start;
                grad.GradientStops[1].Color = end;
            }
            else
            {
                res[key] = new LinearGradientBrush(
                    new GradientStopCollection
                    {
                        new GradientStop(start, 0),
                        new GradientStop(end,   1)
                    });
            }
        }

        #endregion Private Methods
    }
}