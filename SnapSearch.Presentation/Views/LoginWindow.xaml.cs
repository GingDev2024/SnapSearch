using SnapSearch.Application.Common.Helpers;
using SnapSearch.Presentation.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SnapSearch.Presentation.Views
{
    public partial class LoginWindow : Window
    {
        #region Fields

        private readonly LoginViewModel _vm;

        #endregion Fields

        #region Public Constructors

        public LoginWindow(LoginViewModel vm)
        {
            InitializeComponent();
            _vm = vm;
            DataContext = _vm;
            _vm.LoginSucceeded += OnLoginSucceeded;
        }

        #endregion Public Constructors

        #region Private Methods

        private void OnLoginSucceeded(Application.DTOs.UserDto user)
        {
            var shell = App.GetService<MainShellWindow>();
            shell.Initialize();
            System.Windows.Application.Current.MainWindow = shell;
            shell.Show();
            Close();
        }

        private void DragBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

        private void Input_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
                PasswordBox.Focus();
        }

        private void PasswordBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
                TriggerLogin();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show($"{PasswordHelper.Hash}");
            TriggerLogin();
        }

        private void TriggerLogin()
        {
            if (_vm.LoginCommand.CanExecute(PasswordBox.Password))
                _vm.LoginCommand.Execute(PasswordBox.Password);
        }

        #endregion Private Methods      
    }
}