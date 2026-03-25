using Dapper;
using SnapSearch.Application.Contracts.Infrastructure;
using SnapSearch.Domain.Entities;

namespace SnapSearch.Infrastructure.Repositories
{
    public class SearchHistoryRepository : ISearchHistoryRepository
    {
        #region Fields

        private readonly IUnitOfWork _uow;

        #endregion Fields

        #region Public Constructors

        public SearchHistoryRepository(IUnitOfWork uow) => _uow = uow;

        #endregion Public Constructors

        #region Public Methods

        public async Task<int> CreateAsync(SearchHistory history, CancellationToken cancellationToken = default)
        {
            var sql = @"
                INSERT INTO SearchHistory (UserId, Keyword, SearchDirectory, FileExtensionFilter, ResultCount, SearchedAt)
                VALUES (@UserId, @Keyword, @SearchDirectory, @FileExtensionFilter, @ResultCount, @SearchedAt);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            return await _uow.Connection.ExecuteScalarAsync<int>(
                new CommandDefinition(sql, history, _uow.Transaction, cancellationToken: cancellationToken));
        }

        public async Task<IEnumerable<SearchHistory>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
        {
            var sql = "SELECT * FROM SearchHistory WHERE UserId = @UserId ORDER BY SearchedAt DESC";
            return await _uow.Connection.QueryAsync<SearchHistory>(
                new CommandDefinition(sql, new { UserId = userId }, _uow.Transaction, cancellationToken: cancellationToken));
        }

        public async Task<IEnumerable<SearchHistory>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var sql = "SELECT * FROM SearchHistory ORDER BY SearchedAt DESC";
            return await _uow.Connection.QueryAsync<SearchHistory>(
                new CommandDefinition(sql, transaction: _uow.Transaction, cancellationToken: cancellationToken));
        }

        #endregion Public Methods
    }
}