using Dapper;
using SnapSearch.Application.Contracts.Infrastructure;
using SnapSearch.Domain.Entities;

namespace SnapSearch.Infrastructure.Repositories
{
    public class AccessLogRepository : IAccessLogRepository
    {
        #region Fields

        private readonly IUnitOfWork _uow;

        #endregion Fields

        #region Public Constructors

        public AccessLogRepository(IUnitOfWork uow) => _uow = uow;

        #endregion Public Constructors

        #region Public Methods

        public async Task<int> CreateAsync(AccessLog log, CancellationToken cancellationToken = default)
        {
            var sql = @"
                INSERT INTO AccessLogs (UserId, Username, Action, FilePath, SearchKeyword, IpAddress, MacAddress, AccessedAt, Details)
                VALUES (@UserId, @Username, @Action, @FilePath, @SearchKeyword, @IpAddress, @MacAddress, @AccessedAt, @Details);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            return await _uow.Connection.ExecuteScalarAsync<int>(
                new CommandDefinition(sql, log, _uow.Transaction, cancellationToken: cancellationToken));
        }

        public async Task<IEnumerable<AccessLog>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var sql = "SELECT * FROM AccessLogs ORDER BY AccessedAt DESC";
            return await _uow.Connection.QueryAsync<AccessLog>(
                new CommandDefinition(sql, transaction: _uow.Transaction, cancellationToken: cancellationToken));
        }

        public async Task<IEnumerable<AccessLog>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
        {
            var sql = "SELECT * FROM AccessLogs WHERE UserId = @UserId ORDER BY AccessedAt DESC";
            return await _uow.Connection.QueryAsync<AccessLog>(
                new CommandDefinition(sql, new { UserId = userId }, _uow.Transaction, cancellationToken: cancellationToken));
        }

        public async Task<IEnumerable<AccessLog>> GetByDateRangeAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default)
        {
            var sql = "SELECT * FROM AccessLogs WHERE AccessedAt BETWEEN @From AND @To ORDER BY AccessedAt DESC";
            return await _uow.Connection.QueryAsync<AccessLog>(
                new CommandDefinition(sql, new { From = from, To = to }, _uow.Transaction, cancellationToken: cancellationToken));
        }

        #endregion Public Methods
    }
}