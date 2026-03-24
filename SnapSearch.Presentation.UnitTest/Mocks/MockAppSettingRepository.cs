using SnapSearch.Application.Contracts.Infrastructure;
using SnapSearch.Domain.Entities;

namespace SnapSearch.Presentation.UnitTest.Mocks
{
    public class MockAppSettingRepository : IAppSettingRepository
    {
        #region Fields

        private readonly List<AppSetting> _settings = new()
        {
            new AppSetting { Id = 1, Key = "DefaultSearchDirectory", Value = @"C:\", Description = "Default search directory" }
        };

        #endregion Fields

        #region Public Methods

        public Task<AppSetting?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
            => Task.FromResult(_settings.FirstOrDefault(s => s.Key == key));

        public Task<IEnumerable<AppSetting>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IEnumerable<AppSetting>>(_settings.ToList());

        public Task<bool> UpsertAsync(AppSetting setting, CancellationToken cancellationToken = default)
        {
            var existing = _settings.FirstOrDefault(s => s.Key == setting.Key);
            if (existing != null)
            {
                existing.Value = setting.Value;
                existing.Description = setting.Description;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _settings.Add(setting);
            }
            return Task.FromResult(true);
        }

        public Task<bool> DeleteAsync(string key, CancellationToken cancellationToken = default)
        {
            var setting = _settings.FirstOrDefault(s => s.Key == key);
            if (setting == null)
                return Task.FromResult(false);
            _settings.Remove(setting);
            return Task.FromResult(true);
        }

        #endregion Public Methods
    }
}