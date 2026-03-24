using SnapSearch.Application.DTOs;

namespace SnapSearch.Application.Contracts
{
    public interface IAuthService
    {
        #region Properties

        UserDto? CurrentUser { get; }

        bool IsAuthenticated { get; }

        #endregion Properties

        #region Public Methods

        Task<UserDto?> LoginAsync(LoginDto loginDto, CancellationToken cancellationToken = default);

        Task LogoutAsync(int userId, CancellationToken cancellationToken = default);

        bool HasPermission(string permission);

        #endregion Public Methods
    }
}