using SnapSearch.Application.Contracts.Infrastructure;
using SnapSearch.Domain.Entities;

namespace SnapSearch.Presentation.UnitTest.Mocks
{
    public class MockAccessLogRepository : IAccessLogRepository
    {
        #region Fields

        private int _nextId = 1;

        #endregion Fields

        #region Properties

        public List<AccessLog> Logs { get; } = new();

        #endregion Properties

        #region Public Methods

        public Task<int> CreateAsync(AccessLog log, CancellationToken cancellationToken = default)
        {
            log.Id = _nextId++;
            Logs.Add(log);
            return Task.FromResult(log.Id);
        }

        public Task<IEnumerable<AccessLog>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IEnumerable<AccessLog>>(Logs.ToList());

        public Task<IEnumerable<AccessLog>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
            => Task.FromResult<IEnumerable<AccessLog>>(Logs.Where(l => l.UserId == userId).ToList());

        public Task<IEnumerable<AccessLog>> GetByDateRangeAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default)
            => Task.FromResult<IEnumerable<AccessLog>>(Logs.Where(l => l.AccessedAt >= from && l.AccessedAt <= to).ToList());

        #endregion Public Methods
    }
}