using SnapSearch.Application.Contracts;
using SnapSearch.Application.DTOs;
using SnapSearch.Presentation.Common;
using System.Windows.Input;

namespace SnapSearch.Presentation.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        #region Fields

        private readonly IAuthService _authService;

        private string _username = string.Empty;
        private string _errorMessage = string.Empty;

        #endregion Fields

        #region Public Constructors

        public LoginViewModel(IAuthService authService)
        {
            _authService = authService;
            LoginCommand = new AsyncRelayCommand(ExecuteLoginAsync, _ => !IsBusy);
        }

        #endregion Public Constructors

        #region Events

        public event Action<UserDto>? LoginSucceeded;

        #endregion Events

        #region Properties

        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public ICommand LoginCommand { get; }

        #endregion Properties

        #region Private Methods

        private async Task ExecuteLoginAsync(object? parameter)
        {
            ErrorMessage = string.Empty;

            // PasswordBox passes password via parameter (CommandParameter binding workaround)
            var password = parameter as string ?? string.Empty;

            if (string.IsNullOrWhiteSpace(Username))
            {
                ErrorMessage = "Username is required.";
                return;
            }
            if (string.IsNullOrWhiteSpace(password))
            {
                ErrorMessage = "Password is required.";
                return;
            }

            IsBusy = true;
            try
            {
                var user = await _authService.LoginAsync(new LoginDto
                {
                    Username = Username,
                    Password = password
                });

                if (user == null)
                {
                    ErrorMessage = "Invalid username or password.";
                    return;
                }

                SessionContext.Instance.CurrentUser = user;
                LoginSucceeded?.Invoke(user);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Login failed: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion Private Methods
    }
}