using SnapSearch.Application.Contracts;
using SnapSearch.Presentation.Common;
using System.Windows.Input;

namespace SnapSearch.Presentation.ViewModels
{
    public class MainShellViewModel : BaseViewModel
    {
        #region Fields

        private readonly IAuthService _authService;

        // View factories injected
        private readonly Func<SearchViewModel> _searchVmFactory;

        private readonly Func<HealthViewModel> _healthVmFactory;
        private readonly Func<IniEncryptorViewModel> _iniEncryptorVmFactory;
        private readonly Func<UserManagementViewModel> _usersVmFactory;
        private readonly Func<AccessLogViewModel> _logsVmFactory;
        private readonly Func<SettingsViewModel> _settingsVmFactory;
        private object? _currentView;
        private string _currentUserDisplay = string.Empty;

        private bool _isSearchActive = true;

        #endregion Fields

        #region Public Constructors

        public MainShellViewModel(
            IAuthService authService,
            Func<SearchViewModel> searchVmFactory,
            Func<UserManagementViewModel> usersVmFactory,
            Func<AccessLogViewModel> logsVmFactory,
            Func<SettingsViewModel> settingsVmFactory,
            Func<IniEncryptorViewModel> iniEncryptorVmFactory,
            Func<HealthViewModel> healthVmFactory)
        {
            _authService = authService;
            _searchVmFactory = searchVmFactory;
            _usersVmFactory = usersVmFactory;
            _logsVmFactory = logsVmFactory;
            _settingsVmFactory = settingsVmFactory;
            _iniEncryptorVmFactory = iniEncryptorVmFactory;
            _healthVmFactory = healthVmFactory;

            NavigateSearchCommand = new RelayCommand(_ => NavigateTo("Search"));
            NavigateUsersCommand = new RelayCommand(_ => NavigateTo("Users"), _ => IsAdmin);
            NavigateLogsCommand = new RelayCommand(_ => NavigateTo("Logs"), _ => IsAdmin);
            NavigateSettingsCommand = new RelayCommand(_ => NavigateTo("Settings"), _ => IsAdmin);
            NavigateIniEncryptorCommand = new RelayCommand(_ => NavigateTo("IniEncryptor"), _ => IsAdmin);
            NavigateHealthCommand = new RelayCommand(_ => NavigateTo("Health"), _ => IsAdmin); // ← new
            LogoutCommand = new AsyncRelayCommand(ExecuteLogoutAsync);
        }

        #endregion Public Constructors

        #region Events

        public event Action? LogoutRequested;

        #endregion Events

        #region Properties

        public object? CurrentView
        {
            get => _currentView;
            set => SetProperty(ref _currentView, value);
        }

        public string CurrentUserDisplay
        {
            get => _currentUserDisplay;
            set => SetProperty(ref _currentUserDisplay, value);
        }

        public bool IsSearchActive
        {
            get => _isSearchActive;
            set => SetProperty(ref _isSearchActive, value);
        }

        public bool IsAdmin => SessionContext.Instance.CurrentUser?.Role == "Admin";
        public bool CanViewFiles => SessionContext.Instance.HasPermission("ViewFile");

        public ICommand NavigateSearchCommand { get; }
        public ICommand NavigateUsersCommand { get; }
        public ICommand NavigateLogsCommand { get; }
        public ICommand NavigateSettingsCommand { get; }
        public ICommand NavigateIniEncryptorCommand { get; }
        public ICommand NavigateHealthCommand { get; }
        public ICommand LogoutCommand { get; }

        #endregion Properties

        #region Public Methods

        public void Initialize()
        {
            var user = SessionContext.Instance.CurrentUser;
            CurrentUserDisplay = user != null ? $"{user.Username} - {user.Role}" : string.Empty;
            OnPropertyChanged(nameof(IsAdmin));
            NavigateTo("Search");
        }

        #endregion Public Methods

        #region Private Methods

        private void NavigateTo(string view)
        {
            IsSearchActive = view == "Search";
            CurrentView = view switch
            {
                "Search" => _searchVmFactory(),
                "Users" => _usersVmFactory(),
                "Logs" => _logsVmFactory(),
                "Settings" => _settingsVmFactory(),
                "IniEncryptor" => _iniEncryptorVmFactory(),
                "Health" => _healthVmFactory(),
                _ => null
            };
        }

        private async Task ExecuteLogoutAsync(object? _)
        {
            var userId = SessionContext.Instance.CurrentUser?.Id ?? 0;
            await _authService.LogoutAsync(userId);
            SessionContext.Instance.Clear();
            LogoutRequested?.Invoke();
        }

        #endregion Private Methods
    }
}