using SnapSearch.Presentation.Common;
using SnapSearch.Presentation.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace SnapSearch.Presentation.Views
{
    public partial class MainShellWindow : Window
    {
        #region Fields

        private readonly MainShellViewModel _vm;

        #endregion Fields

        #region Public Constructors

        public MainShellWindow(MainShellViewModel vm)
        {
            InitializeComponent();
            _vm = vm;
            DataContext = _vm;
            _vm.LogoutRequested += OnLogoutRequested;
            Closed += OnClosed;
        }

        #endregion Public Constructors

        #region Public Methods

        public void Initialize() => _vm.Initialize();

        #endregion Public Methods

        #region Private Methods

        private void OnClosed(object? sender, EventArgs e)
        {
            _vm.LogoutRequested -= OnLogoutRequested;
            Closed -= OnClosed;
        }

        private void OnLogoutRequested()
        {
            foreach (Window w in System.Windows.Application.Current.Windows
                             .OfType<FilePreviewWindow>().ToList())
            {
                w.Close();
            }
            var login = App.GetService<LoginWindow>();
            System.Windows.Application.Current.MainWindow = login;
            SessionPersistence.Clear();
            login.Show();
            Close();
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
            => WindowState = WindowState.Minimized;

        private void CloseButton_Click(object sender, RoutedEventArgs e)
            => Close();

        #endregion Private Methods
    }
}