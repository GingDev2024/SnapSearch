using SnapSearch.Domain.Entities;

namespace SnapSearch.Application.Contracts.Infrastructure
{
    public interface IAccessLogRepository
    {
        #region Public Methods

        Task<int> CreateAsync(AccessLog log, CancellationToken cancellationToken = default);

        Task<IEnumerable<AccessLog>> GetAllAsync(CancellationToken cancellationToken = default);

        Task<IEnumerable<AccessLog>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);

        Task<IEnumerable<AccessLog>> GetByDateRangeAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default);

        #endregion Public Methods
    }
}