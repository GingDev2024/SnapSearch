using SnapSearch.Application.DTOs;

namespace SnapSearch.Application.Contracts
{
    public interface IUserService
    {
        #region Public Methods

        Task<IEnumerable<UserDto>> GetAllUsersAsync(CancellationToken cancellationToken = default);

        Task<UserDto?> GetUserByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<UserDto> CreateUserAsync(CreateUserDto dto, CancellationToken cancellationToken = default);

        Task<bool> UpdateUserAsync(UpdateUserDto dto, CancellationToken cancellationToken = default);

        Task<bool> DeleteUserAsync(int id, CancellationToken cancellationToken = default);

        #endregion Public Methods
    }
}