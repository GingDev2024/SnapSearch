using System.Windows;

namespace SnapSearch.Presentation.Common
{
    public enum AppTheme
    { Dark, Light }

    public static class ThemeManager
    {
        #region Fields

        private const string DarkThemeUri = "Themes/Dark.xaml";
        private const string LightThemeUri = "Themes/Light.xaml";

        #endregion Fields

        #region Events

        public static event Action<AppTheme>? ThemeChanged;

        #endregion Events

        #region Properties

        public static AppTheme CurrentTheme { get; private set; } = AppTheme.Dark;
        public static bool IsDark => CurrentTheme == AppTheme.Dark;

        #endregion Properties

        #region Public Methods

        public static void Apply(AppTheme theme)
        {
            CurrentTheme = theme;

            var mergedDicts = System.Windows.Application.Current.Resources.MergedDictionaries;

            // Find and replace the current colour theme dictionary (index 0)
            var themeUri = new Uri(
                theme == AppTheme.Dark ? DarkThemeUri : LightThemeUri,
                UriKind.Relative);

            var newDict = new ResourceDictionary { Source = themeUri };

            if (mergedDicts.Count > 0)
                mergedDicts[0] = newDict;   // index 0 is always the colour theme
            else
                mergedDicts.Add(newDict);

            ThemeChanged?.Invoke(theme);
        }

        public static void Toggle() =>
            Apply(CurrentTheme == AppTheme.Dark ? AppTheme.Light : AppTheme.Dark);

        #endregion Public Methods
    }
}