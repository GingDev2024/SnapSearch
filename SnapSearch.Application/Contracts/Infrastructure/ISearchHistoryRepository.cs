using SnapSearch.Domain.Entities;

namespace SnapSearch.Application.Contracts.Infrastructure
{
    public interface ISearchHistoryRepository
    {
        #region Public Methods

        Task<int> CreateAsync(SearchHistory history, CancellationToken cancellationToken = default);

        Task<IEnumerable<SearchHistory>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);

        Task<IEnumerable<SearchHistory>> GetAllAsync(CancellationToken cancellationToken = default);

        #endregion Public Methods
    }
}