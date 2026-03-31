using SnapSearch.Application.Contracts;
using SnapSearch.Application.DTOs;
using SnapSearch.Presentation.Common;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace SnapSearch.Presentation.ViewModels
{
    public class UserManagementViewModel : BaseViewModel
    {
        #region Fields

        private readonly IUserService _userService;

        private UserDto? _selectedUser;

        // Form fields
        private string _formUsername = string.Empty;
        private string _formRole = "ViewerOnly";
        private string _formPassword = string.Empty;
        private bool _formIsActive = true;
        private DateTime _formCreatedAt = DateTime.UtcNow.AddHours(8);

        #endregion Fields

        #region Public Constructors

        public UserManagementViewModel(IUserService userService)
        {
            _userService = userService;
            LoadUsersCommand = new AsyncRelayCommand(LoadUsersAsync);
            SaveUserCommand = new AsyncRelayCommand(SaveUserAsync, _ => !IsBusy);
            DeleteUserCommand = new AsyncRelayCommand(DeleteUserAsync, _ => SelectedUser != null && !IsBusy);
            NewUserCommand = new RelayCommand(ClearForm);

            _ = LoadUsersAsync(null);
        }

        #endregion Public Constructors

        #region Properties

        public ObservableCollection<UserDto> Users { get; } = new();

        public UserDto? SelectedUser
        {
            get => _selectedUser;
            set
            {
                SetProperty(ref _selectedUser, value);
                if (value != null)
                    PopulateForm(value);
                OnPropertyChanged(nameof(IsEditing));
            }
        }

        public string FormUsername
        {
            get => _formUsername;
            set => SetProperty(ref _formUsername, value);
        }

        public string FormRole
        {
            get => _formRole;
            set => SetProperty(ref _formRole, value);
        }

        public string FormPassword
        {
            get => _formPassword;
            set => SetProperty(ref _formPassword, value);
        }

        public bool FormIsActive
        {
            get => _formIsActive;
            set => SetProperty(ref _formIsActive, value);
        }

        public DateTime FormCreatedAt 
        { 
            get => _formCreatedAt;
            set => SetProperty(ref _formCreatedAt, value); 
        }

        public bool IsEditing => SelectedUser != null;

        public List<string> Roles { get; } = new()
        {
            "Admin", "ViewListOnly", "ViewerOnly", "ViewAndPrint", "Compliance"
        };

        public ICommand LoadUsersCommand { get; }
        public ICommand SaveUserCommand { get; }
        public ICommand DeleteUserCommand { get; }
        public ICommand NewUserCommand { get; }

        #endregion Properties

        #region Private Methods

        private async Task LoadUsersAsync(object? _)
        {
            IsBusy = true;
            try
            {
                Users.Clear();
                var users = await _userService.GetAllUsersAsync();
                foreach (var u in users)
                    Users.Add(u);
                StatusMessage = $"{Users.Count} user(s) loaded.";
            }
            catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
            finally { IsBusy = false; }
        }

        private async Task SaveUserAsync(object? _)
        {
            if (string.IsNullOrWhiteSpace(FormUsername))
            {
                StatusMessage = "Username is required.";
                return;
            }

            IsBusy = true;
            try
            {
                if (IsEditing && SelectedUser != null)
                {
                    var dto = new UpdateUserDto
                    {
                        Id = SelectedUser.Id,
                        Username = FormUsername,
                        Role = FormRole,
                        IsActive = FormIsActive,
                        UpdatedAt = FormCreatedAt,
                        NewPassword = string.IsNullOrWhiteSpace(FormPassword) ? null : FormPassword,
                    };
                    await _userService.UpdateUserAsync(dto);
                    StatusMessage = "User updated successfully.";
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(FormPassword))
                    {
                        StatusMessage = "Password is required for new users.";
                        return;
                    }
                    var dto = new CreateUserDto
                    {
                        Username = FormUsername,
                        Password = FormPassword,
                        Role = FormRole,
                        CreatedAt = FormCreatedAt
                    };
                    await _userService.CreateUserAsync(dto);
                    StatusMessage = "User created successfully.";
                }

                await LoadUsersAsync(null);
                ClearForm(null);
            }
            catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
            finally { IsBusy = false; }
        }

        private async Task DeleteUserAsync(object? _)
        {
            if (SelectedUser == null)
                return;
            IsBusy = true;
            try
            {
                await _userService.DeleteUserAsync(SelectedUser.Id);
                StatusMessage = "User deactivated.";
                await LoadUsersAsync(null);
                ClearForm(null);
            }
            catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
            finally { IsBusy = false; }
        }

        private void PopulateForm(UserDto user)
        {
            FormUsername = user.Username;
            FormRole = user.Role;
            FormIsActive = user.IsActive;
            FormPassword = string.Empty;
            FormCreatedAt = user.CreatedAt;
        }

        private void ClearForm(object? _)
        {
            SelectedUser = null;
            FormUsername = string.Empty;
            FormRole = "ViewerOnly";
            FormPassword = string.Empty;
            FormIsActive = true;
            FormCreatedAt = DateTime.UtcNow.AddHours(8);    
            OnPropertyChanged(nameof(IsEditing));
        }

        #endregion Private Methods
    }
}