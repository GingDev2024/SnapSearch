using Dapper;
using SnapSearch.Application.Contracts.Infrastructure;
using SnapSearch.Domain.Entities;

namespace SnapSearch.Infrastructure.Repositories
{
    public class SearchHistoryRepository : ISearchHistoryRepository
    {
        #region Fields

        private readonly UnitOfWork _uow;

        #endregion Fields

        #region Public Constructors

        public SearchHistoryRepository(UnitOfWork uow)
        {
            _uow = uow;
        }

        #endregion Public Constructors

        #region Public Methods

        public async Task<int> CreateAsync(SearchHistory history, CancellationToken cancellationToken = default)
        {
            var cmd = new CommandDefinition(
                @"INSERT INTO dbo.SearchHistory(
                    UserId,
                    Keyword,
                    SearchDirectory,
                    FileExtensionFilter,
                    ResultCount,
                    SearchedAt)

                VALUES(
                    @UserId,
                    @Keyword,
                    @SearchDirectory,
                    @FileExtensionFilter,
                    @ResultCount,
                    @SearchedAt);

                SELECT CAST(SCOPE_IDENTITY() AS INT);",
                new
                {
                    history.UserId,
                    history.Keyword,
                    history.SearchDirectory,
                    history.FileExtensionFilter,
                    history.ResultCount,
                    history.SearchedAt
                },
                _uow.Transaction,
                cancellationToken: cancellationToken);

            return await _uow.Connection.ExecuteScalarAsync<int>(cmd);
        }

        public async Task<IEnumerable<SearchHistory>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
        {
            var cmd = new CommandDefinition(
                @"SELECT Id, UserId, Keyword, SearchDirectory, FileExtensionFilter, ResultCount, SearchedAt
                FROM dbo.SearchHistory
                WHERE UserId = @UserId
                ORDER BY SearchedAt DESC;",
                new { UserId = userId },
                _uow.Transaction,
                cancellationToken: cancellationToken);

            return await _uow.Connection.QueryAsync<SearchHistory>(cmd);
        }

        public async Task<IEnumerable<SearchHistory>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var cmd = new CommandDefinition(
                @"SELECT Id, UserId, Keyword, SearchDirectory, FileExtensionFilter, ResultCount, SearchedAt
                FROM dbo.SearchHistory
                ORDER BY SearchedAt DESC;",
                transaction: _uow.Transaction,
                cancellationToken: cancellationToken);

            return await _uow.Connection.QueryAsync<SearchHistory>(cmd);
        }

        #endregion Public Methods
    }
}