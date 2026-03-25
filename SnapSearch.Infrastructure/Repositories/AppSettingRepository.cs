using Dapper;
using SnapSearch.Application.Contracts.Infrastructure;
using SnapSearch.Domain.Entities;

namespace SnapSearch.Infrastructure.Repositories
{
    public class AppSettingRepository : IAppSettingRepository
    {
        #region Fields

        private readonly IUnitOfWork _uow;

        #endregion Fields

        #region Public Constructors

        public AppSettingRepository(IUnitOfWork uow) => _uow = uow;

        #endregion Public Constructors

        #region Public Methods

        public async Task<AppSetting?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
        {
            var sql = "SELECT * FROM AppSettings WHERE [Key] = @Key";
            return await _uow.Connection.QueryFirstOrDefaultAsync<AppSetting>(
                new CommandDefinition(sql, new { Key = key }, _uow.Transaction, cancellationToken: cancellationToken));
        }

        public async Task<IEnumerable<AppSetting>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var sql = "SELECT * FROM AppSettings ORDER BY [Key]";
            return await _uow.Connection.QueryAsync<AppSetting>(
                new CommandDefinition(sql, transaction: _uow.Transaction, cancellationToken: cancellationToken));
        }

        public async Task<bool> UpsertAsync(AppSetting setting, CancellationToken cancellationToken = default)
        {
            var sql = @"
                IF EXISTS (SELECT 1 FROM AppSettings WHERE [Key] = @Key)
                    UPDATE AppSettings
                    SET [Value] = @Value,
                        [Description] = @Description,
                        UpdatedAt = @UpdatedAt
                    WHERE [Key] = @Key
                ELSE
                    INSERT INTO AppSettings ([Key], [Value], [Description], UpdatedAt)
                    VALUES (@Key, @Value, @Description, @UpdatedAt);";

            var rows = await _uow.Connection.ExecuteAsync(
                new CommandDefinition(sql, new { setting.Key, setting.Value, setting.Description, UpdatedAt = DateTime.UtcNow },
                _uow.Transaction, cancellationToken: cancellationToken));

            return rows > 0;
        }

        public async Task<bool> DeleteAsync(string key, CancellationToken cancellationToken = default)
        {
            var sql = "DELETE FROM AppSettings WHERE [Key] = @Key";
            var rows = await _uow.Connection.ExecuteAsync(
                new CommandDefinition(sql, new { Key = key }, _uow.Transaction, cancellationToken: cancellationToken));
            return rows > 0;
        }

        #endregion Public Methods
    }
}