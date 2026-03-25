using SnapSearch.Presentation.ViewModels;
using System.Windows.Controls;

namespace SnapSearch.Presentation.Views
{
    public partial class UserManagementView : System.Windows.Controls.UserControl
    {
        #region Public Constructors

        public UserManagementView()
        {
            InitializeComponent();
        }

        #endregion Public Constructors

        #region Private Methods

        private void PasswordBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is UserManagementViewModel vm)
                vm.FormPassword = PasswordBox.Password;
        }

        #endregion Private Methods
    }
}