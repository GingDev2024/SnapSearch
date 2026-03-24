using AutoMapper;
using SnapSearch.Application.Contracts;
using SnapSearch.Application.Contracts.Infrastructure;
using SnapSearch.Application.DTOs;
using SnapSearch.Domain.Entities;

namespace SnapSearch.Application.Services
{
    public class SettingsService : ISettingsService
    {
        #region Fields

        private const string DefaultDirectoryKey = "DefaultSearchDirectory";
        private readonly IAppSettingRepository _settingRepository;
        private readonly IMapper _mapper;

        #endregion Fields

        #region Public Constructors

        public SettingsService(IAppSettingRepository settingRepository, IMapper mapper)
        {
            _settingRepository = settingRepository;
            _mapper = mapper;
        }

        #endregion Public Constructors

        #region Public Methods

        public async Task<string?> GetValueAsync(string key, CancellationToken cancellationToken = default)
        {
            var setting = await _settingRepository.GetByKeyAsync(key, cancellationToken);
            return setting?.Value;
        }

        public async Task<IEnumerable<AppSettingDto>> GetAllSettingsAsync(CancellationToken cancellationToken = default)
        {
            var settings = await _settingRepository.GetAllAsync(cancellationToken);
            return _mapper.Map<IEnumerable<AppSettingDto>>(settings);
        }

        public async Task SaveSettingAsync(AppSettingDto dto, CancellationToken cancellationToken = default)
        {
            var setting = _mapper.Map<AppSetting>(dto);
            await _settingRepository.UpsertAsync(setting, cancellationToken);
        }

        public async Task<string> GetDefaultSearchDirectoryAsync(CancellationToken cancellationToken = default)
        {
            var value = await GetValueAsync(DefaultDirectoryKey, cancellationToken);
            return value ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        }

        public async Task SetDefaultSearchDirectoryAsync(string path, CancellationToken cancellationToken = default)
        {
            await SaveSettingAsync(new AppSettingDto
            {
                Key = DefaultDirectoryKey,
                Value = path,
                Description = "Default directory for file searches"
            }, cancellationToken);
        }

        #endregion Public Methods
    }
}