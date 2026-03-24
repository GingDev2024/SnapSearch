using SnapSearch.Application.DTOs;

namespace SnapSearch.Application.Contracts
{
    public interface ISettingsService
    {
        #region Public Methods

        Task<string?> GetValueAsync(string key, CancellationToken cancellationToken = default);

        Task<IEnumerable<AppSettingDto>> GetAllSettingsAsync(CancellationToken cancellationToken = default);

        Task SaveSettingAsync(AppSettingDto dto, CancellationToken cancellationToken = default);

        Task<string> GetDefaultSearchDirectoryAsync(CancellationToken cancellationToken = default);

        Task SetDefaultSearchDirectoryAsync(string path, CancellationToken cancellationToken = default);

        #endregion Public Methods
    }
}