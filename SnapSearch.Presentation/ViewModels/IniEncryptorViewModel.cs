using Microsoft.Extensions.Configuration;
using SnapSearch.Application.Common.Helpers;
using SnapSearch.Presentation.Common;
using System.Windows.Input;

namespace SnapSearch.Presentation.ViewModels
{
    public class IniEncryptorViewModel : BaseViewModel
    {
        #region Fields

        private readonly IConfiguration _configuration;

        private string _username = string.Empty;
        private string _password = string.Empty;
        private string _encryptedUser = string.Empty;
        private string _encryptedPass = string.Empty;
        private string _status = string.Empty;
        private bool _statusIsError;
        private bool _isEncrypting;

        #endregion Fields

        #region Public Constructors

        public IniEncryptorViewModel(IConfiguration configuration)
        {
            _configuration = configuration;
            EncryptCommand = new RelayCommand(_ => ExecuteEncrypt(), _ => CanEncrypt());
            CopyUserCommand = new RelayCommand(_ => CopyToClipboard(EncryptedUser));
            CopyPasswordCommand = new RelayCommand(_ => CopyToClipboard(EncryptedPass));
        }

        #endregion Public Constructors

        #region Properties

        public string Username
        {
            get => _username;
            set
            {
                SetProperty(ref _username, value);
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                SetProperty(ref _password, value);
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public string EncryptedUser
        {
            get => _encryptedUser;
            private set => SetProperty(ref _encryptedUser, value);
        }

        public string EncryptedPass
        {
            get => _encryptedPass;
            private set => SetProperty(ref _encryptedPass, value);
        }

        public string Status
        {
            get => _status;
            private set => SetProperty(ref _status, value);
        }

        public bool StatusIsError
        {
            get => _statusIsError;
            private set => SetProperty(ref _statusIsError, value);
        }

        public bool IsEncrypting
        {
            get => _isEncrypting;
            private set => SetProperty(ref _isEncrypting, value);
        }

        public string MachineName => Environment.MachineName;

        public ICommand EncryptCommand { get; }
        public ICommand CopyUserCommand { get; }
        public ICommand CopyPasswordCommand { get; }

        #endregion Properties

        #region Private Methods

        private static void CopyToClipboard(string text)
        {
            if (!string.IsNullOrWhiteSpace(text))
                System.Windows.Clipboard.SetText(text);
        }

        private bool CanEncrypt() =>
                    !IsEncrypting &&
            !string.IsNullOrWhiteSpace(Username) &&
            !string.IsNullOrWhiteSpace(Password);

        private void ExecuteEncrypt()
        {
            var key = _configuration["EncryptionKey"];
            if (string.IsNullOrWhiteSpace(key))
            {
                SetStatus("EncryptionKey is missing from appsettings.json.", isError: true);
                return;
            }

            IsEncrypting = true;
            SetStatus("Encrypting…");

            try
            {
                EncryptedUser = IniEncryptionHelper.Encrypt(Username, key);
                EncryptedPass = IniEncryptionHelper.Encrypt(Password, key);
                SetStatus("✓ Encrypted! Copy the values into snapsearch.ini.");
            }
            catch (Exception ex)
            {
                SetStatus($"✗ {ex.Message}", isError: true);
            }
            finally
            {
                IsEncrypting = false;
            }
        }

        private void SetStatus(string msg, bool isError = false)
        {
            Status = msg;
            StatusIsError = isError;
        }

        #endregion Private Methods
    }
}