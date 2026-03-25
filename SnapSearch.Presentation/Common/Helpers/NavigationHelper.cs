using System.Windows;

namespace SnapSearch.Presentation.Common.Helpers
{
    public static class NavigationHelper
    {
        #region Public Methods

        public static void SwitchWindow(Window newWindow)
        {
            var current = System.Windows.Application.Current.MainWindow;

            System.Windows.Application.Current.MainWindow = newWindow;
            newWindow.Show();

            current?.Close();
        }

        #endregion Public Methods
    }
}