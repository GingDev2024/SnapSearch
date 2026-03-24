using Dapper;
using SnapSearch.Application.Contracts.Infrastructure;
using SnapSearch.Domain.Entities;

namespace SnapSearch.Infrastructure.Repositories
{
    public class AppSettingRepository : IAppSettingRepository
    {
        #region Fields

        private readonly UnitOfWork _uow;

        #endregion Fields

        #region Public Constructors

        public AppSettingRepository(UnitOfWork uow)
        {
            _uow = uow;
        }

        #endregion Public Constructors

        #region Public Methods

        public async Task<AppSetting?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
        {
            var cmd = new CommandDefinition(
                "EXEC dbo.sp_GetSettingByKey @Key",
                new { Key = key },
                _uow.Transaction,
                cancellationToken: cancellationToken);
            return await _uow.Connection.QueryFirstOrDefaultAsync<AppSetting>(cmd);
        }

        public async Task<IEnumerable<AppSetting>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var cmd = new CommandDefinition(
                "EXEC dbo.sp_GetAllSettings",
                transaction: _uow.Transaction,
                cancellationToken: cancellationToken);
            return await _uow.Connection.QueryAsync<AppSetting>(cmd);
        }

        public async Task<bool> UpsertAsync(AppSetting setting, CancellationToken cancellationToken = default)
        {
            var cmd = new CommandDefinition(
                "EXEC dbo.sp_UpsertSetting @Key, @Value, @Description, @UpdatedAt",
                new
                {
                    setting.Key,
                    setting.Value,
                    setting.Description,
                    UpdatedAt = DateTime.UtcNow
                },
                _uow.Transaction,
                cancellationToken: cancellationToken);
            var rows = await _uow.Connection.ExecuteAsync(cmd);
            return rows > 0;
        }

        public async Task<bool> DeleteAsync(string key, CancellationToken cancellationToken = default)
        {
            var cmd = new CommandDefinition(
                "EXEC dbo.sp_DeleteSetting @Key",
                new { Key = key },
                _uow.Transaction,
                cancellationToken: cancellationToken);
            var rows = await _uow.Connection.ExecuteAsync(cmd);
            return rows > 0;
        }

        #endregion Public Methods
    }
}