using Dapper;
using SnapSearch.Application.Contracts.Infrastructure;
using SnapSearch.Domain.Entities;

namespace SnapSearch.Infrastructure.Repositories
{
    public class AccessLogRepository : IAccessLogRepository
    {
        #region Fields

        private readonly UnitOfWork _uow;

        #endregion Fields

        #region Public Constructors

        public AccessLogRepository(UnitOfWork uow)
        {
            _uow = uow;
        }

        #endregion Public Constructors

        #region Public Methods

        public async Task<int> CreateAsync(AccessLog log, CancellationToken cancellationToken = default)
        {
            var cmd = new CommandDefinition(
                "EXEC dbo.sp_CreateAccessLog @UserId, @Username, @Action, @FilePath, @SearchKeyword, @IpAddress, @MacAddress, @AccessedAt, @Details",
                new
                {
                    log.UserId,
                    log.Username,
                    log.Action,
                    log.FilePath,
                    log.SearchKeyword,
                    log.IpAddress,
                    log.MacAddress,
                    log.AccessedAt,
                    log.Details
                },
                _uow.Transaction,
                cancellationToken: cancellationToken);
            return await _uow.Connection.ExecuteScalarAsync<int>(cmd);
        }

        public async Task<IEnumerable<AccessLog>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var cmd = new CommandDefinition(
                "EXEC dbo.sp_GetAllAccessLogs",
                transaction: _uow.Transaction,
                cancellationToken: cancellationToken);
            return await _uow.Connection.QueryAsync<AccessLog>(cmd);
        }

        public async Task<IEnumerable<AccessLog>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
        {
            var cmd = new CommandDefinition(
                "EXEC dbo.sp_GetAccessLogsByUserId @UserId",
                new { UserId = userId },
                _uow.Transaction,
                cancellationToken: cancellationToken);
            return await _uow.Connection.QueryAsync<AccessLog>(cmd);
        }

        public async Task<IEnumerable<AccessLog>> GetByDateRangeAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default)
        {
            var cmd = new CommandDefinition(
                "EXEC dbo.sp_GetAccessLogsByDateRange @From, @To",
                new { From = from, To = to },
                _uow.Transaction,
                cancellationToken: cancellationToken);
            return await _uow.Connection.QueryAsync<AccessLog>(cmd);
        }

        #endregion Public Methods
    }
}