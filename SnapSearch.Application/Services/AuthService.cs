using AutoMapper;
using SnapSearch.Application.Common.Helpers;
using SnapSearch.Application.Contracts;
using SnapSearch.Application.Contracts.Infrastructure;
using SnapSearch.Application.DTOs;
using SnapSearch.Domain.Enums;
using SnapSearch.Domain.Helpers;

namespace SnapSearch.Application.Services
{
    public class AuthService : IAuthService
    {
        #region Fields

        private readonly IUserRepository _userRepository;
        private readonly IAccessLogRepository _accessLogRepository;
        private readonly IMapper _mapper;

        #endregion Fields

        #region Public Constructors

        public AuthService(IUserRepository userRepository, IAccessLogRepository accessLogRepository, IMapper mapper)
        {
            _userRepository = userRepository;
            _accessLogRepository = accessLogRepository;
            _mapper = mapper;
        }

        #endregion Public Constructors

        #region Properties

        public UserDto? CurrentUser { get; private set; }
        public bool IsAuthenticated => CurrentUser != null;

        #endregion Properties

        #region Public Methods

        public async Task<UserDto?> LoginAsync(LoginDto loginDto, CancellationToken cancellationToken = default)
        {
            var hash = PasswordHelper.Hash(loginDto.Password);
            var user = await _userRepository.AuthenticateAsync(loginDto.Username, hash, cancellationToken);

            if (user == null)
                return null;

            CurrentUser = _mapper.Map<UserDto>(user);

            await _accessLogRepository.CreateAsync(new Domain.Entities.AccessLog
            {
                UserId = user.Id,
                Username = user.Username,
                Action = ActionType.Login.ToString(),
                IpAddress = NetworkHelper.GetLocalIpAddress(),
                MacAddress = NetworkHelper.GetMacAddress(),
                AccessedAt = TimeHelper.Now
            }, cancellationToken);

            return CurrentUser;
        }

        public void RestoreSession(UserDto user)
        {
            CurrentUser = user;
        }

        public async Task LogoutAsync(int userId, CancellationToken cancellationToken = default)
        {
            if (CurrentUser != null)
            {
                await _accessLogRepository.CreateAsync(new Domain.Entities.AccessLog
                {
                    UserId = userId,
                    Username = CurrentUser.Username,
                    Action = ActionType.Logout.ToString(),
                    IpAddress = NetworkHelper.GetLocalIpAddress(),
                    MacAddress = NetworkHelper.GetMacAddress(),
                    AccessedAt = TimeHelper.Now
                }, cancellationToken);
            }

            CurrentUser = null;
        }

        public bool HasPermission(string permission)
        {
            if (CurrentUser == null)
                return false;

            var role = CurrentUser.Role;

            return permission switch
            {
                "ChangeSettings" => role == UserRole.Admin.ToString(),
                "ManageUsers" => role == UserRole.Admin.ToString(),
                "ViewLogs" => role == UserRole.Admin.ToString(),
                "Search" => true,
                "ViewFile" => role != UserRole.ViewListOnly.ToString(),
                "PrintFile" => role == UserRole.Admin.ToString() || role == UserRole.ViewAndPrint.ToString(),
                "ExportFile" => role == UserRole.Admin.ToString() || role == UserRole.Compliance.ToString(),
                "CopyFile" => role == UserRole.Admin.ToString() || role == UserRole.Compliance.ToString(),
                "SaveFile" => role == UserRole.Admin.ToString() || role == UserRole.Compliance.ToString(),
                _ => false
            };
        }

        #endregion Public Methods
    }
}