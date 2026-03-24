using SnapSearch.Application.Contracts.Infrastructure;
using SnapSearch.Domain.Entities;

namespace SnapSearch.Presentation.UnitTest.Mocks
{
    public class MockSearchHistoryRepository : ISearchHistoryRepository
    {
        #region Fields

        private int _nextId = 1;

        #endregion Fields

        #region Properties

        public List<SearchHistory> History { get; } = new();

        #endregion Properties

        #region Public Methods

        public Task<int> CreateAsync(SearchHistory history, CancellationToken cancellationToken = default)
        {
            history.Id = _nextId++;
            History.Add(history);
            return Task.FromResult(history.Id);
        }

        public Task<IEnumerable<SearchHistory>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
            => Task.FromResult<IEnumerable<SearchHistory>>(History.Where(h => h.UserId == userId).ToList());

        public Task<IEnumerable<SearchHistory>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IEnumerable<SearchHistory>>(History.ToList());

        #endregion Public Methods
    }
}