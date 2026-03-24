using SnapSearch.Domain.Entities;

namespace SnapSearch.Application.Contracts.Infrastructure
{
    public interface IUserRepository
    {
        #region Public Methods

        Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);

        Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken = default);

        Task<int> CreateAsync(User user, CancellationToken cancellationToken = default);

        Task<bool> UpdateAsync(User user, CancellationToken cancellationToken = default);

        Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);

        Task<User?> AuthenticateAsync(string username, string passwordHash, CancellationToken cancellationToken = default);

        #endregion Public Methods
    }
}