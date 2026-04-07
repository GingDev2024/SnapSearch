using SnapSearch.Presentation.ViewModels;

namespace SnapSearch.Presentation.Views
{
    public partial class IniEncryptorView : System.Windows.Controls.UserControl
    {
        #region Public Constructors

        public IniEncryptorView()
        {
            InitializeComponent();
        }

        #endregion Public Constructors

        #region Private Methods

        // PasswordBox cannot be bound two-way in WPF — push value into VM on every keystroke
        private void PasswordInput_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is IniEncryptorViewModel vm)
                vm.Password = PasswordInput.Password;
        }

        #endregion Private Methods
    }
}