using SnapSearch.Domain.Entities;

namespace SnapSearch.Application.Contracts.Infrastructure
{
    public interface IAppSettingRepository
    {
        #region Public Methods

        Task<AppSetting?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);

        Task<IEnumerable<AppSetting>> GetAllAsync(CancellationToken cancellationToken = default);

        Task<bool> UpsertAsync(AppSetting setting, CancellationToken cancellationToken = default);

        Task<bool> DeleteAsync(string key, CancellationToken cancellationToken = default);

        #endregion Public Methods
    }
}