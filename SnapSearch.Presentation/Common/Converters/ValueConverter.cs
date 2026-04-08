using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SnapSearch.Presentation.Common.Converters
{
    /// <summary>
    /// Returns Visible when the value is NOT null, Collapsed when it IS null.
    /// Used to show the selected-file info bar only when a row is selected.
    /// </summary>
    public class NullToVisibilityConverter : IValueConverter
    {
        #region Public Methods

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is null ? Visibility.Collapsed : Visibility.Visible;

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();

        #endregion Public Methods
    }

    /// <summary>
    /// Converts a non-null/non-empty string to Visible, null/empty to Collapsed.
    /// </summary>
    public class StringToVisibilityConverter : IValueConverter
    {
        #region Public Methods

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => string.IsNullOrWhiteSpace(value?.ToString())
                ? Visibility.Collapsed
                : Visibility.Visible;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();

        #endregion Public Methods
    }

    /// <summary>
    /// Converts bool IsEditing -> "Edit User" or "New User"
    /// </summary>
    public class EditLabelConverter : IValueConverter
    {
        #region Public Methods

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is true ? "Edit User" : "New User";

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();

        #endregion Public Methods
    }

    /// <summary>
    /// Returns NavButtonActive style when true, NavButton when false.
    /// Used for sidebar active-state highlighting.
    /// </summary>
    public class NavStyleConverter : IValueConverter
    {
        #region Public Methods

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var isActive = value is true;
            var key = isActive ? "NavButtonActive" : "NavButton";
            return System.Windows.Application.Current.FindResource(key);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();

        #endregion Public Methods
    }

    /// <summary>
    /// Inverts a BooleanToVisibilityConverter (true = Collapsed, false = Visible).
    /// </summary>
    public class InverseBoolToVisibilityConverter : IValueConverter
    {
        #region Public Methods

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is true ? Visibility.Collapsed : Visibility.Visible;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();

        #endregion Public Methods
    }

    /// <summary>
    /// Formats file size bytes into human-readable string.
    /// </summary>
    public class FileSizeConverter : IValueConverter
    {
        #region Public Methods

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not long bytes)
                return "—";
            if (bytes < 1024)
                return $"{bytes} B";
            if (bytes < 1024 * 1024)
                return $"{bytes / 1024.0:F1} KB";
            if (bytes < 1024L * 1024 * 1024)
                return $"{bytes / (1024.0 * 1024):F1} MB";
            return $"{bytes / (1024.0 * 1024 * 1024):F1} GB";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();

        #endregion Public Methods
    }

    /// <summary>
    /// Returns Visibility.Visible when count > 0.
    /// </summary>
    public class CountToVisibilityConverter : IValueConverter
    {
        #region Public Methods

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is int count && count > 0 ? Visibility.Visible : Visibility.Collapsed;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();

        #endregion Public Methods
    }
}