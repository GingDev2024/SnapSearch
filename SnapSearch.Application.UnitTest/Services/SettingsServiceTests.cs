using AutoMapper;
using Moq;
using Shouldly;
using SnapSearch.Application.Contracts.Infrastructure;
using SnapSearch.Application.DTOs;
using SnapSearch.Application.Services;
using SnapSearch.Application.UnitTests.Mocks;
using SnapSearch.Domain.Entities;

namespace SnapSearch.Application.UnitTests.Services
{
    public class SettingsServiceTests
    {
        #region Fields

        private const string DefaultDirectoryKey = "DefaultSearchDirectory";

        private readonly Mock<IAppSettingRepository> _settingRepositoryMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly SettingsService _sut;

        #endregion Fields

        #region Constructors

        public SettingsServiceTests()
        {
            _settingRepositoryMock = RepositoryMocks.GetAppSettingRepository();
            _mapperMock = new Mock<IMapper>();
            _sut = new SettingsService(_settingRepositoryMock.Object, _mapperMock.Object);
        }

        #endregion Constructors

        #region GetValueAsync

        [Fact]
        public async Task GetValueAsync_ExistingKey_ShouldReturn_StoredValue()
        {
            // "Theme" → "Dark" is in seed data
            var result = await _sut.GetValueAsync("Theme");

            result.ShouldBe("Dark");
        }

        [Fact]
        public async Task GetValueAsync_NonExistentKey_ShouldReturnNull()
        {
            var result = await _sut.GetValueAsync("NonExistentKey");

            result.ShouldBeNull();
        }

        [Fact]
        public async Task GetValueAsync_ShouldCall_Repository_WithCorrectKey()
        {
            await _sut.GetValueAsync("MaxResults");

            _settingRepositoryMock.Verify(
                r => r.GetByKeyAsync("MaxResults", It.IsAny<CancellationToken>()),
                Times.Once);
        }

        #endregion GetValueAsync

        #region GetAllSettingsAsync

        [Fact]
        public async Task GetAllSettingsAsync_ShouldReturn_MappedDtos()
        {
            var dtos = new List<AppSettingDto>
            {
                new AppSettingDto { Key = "DefaultSearchDirectory", Value = @"C:\Documents" },
                new AppSettingDto { Key = "Theme",                  Value = "Dark" },
                new AppSettingDto { Key = "MaxResults",             Value = "100" }
            };

            _mapperMock
                .Setup(m => m.Map<IEnumerable<AppSettingDto>>(It.IsAny<IEnumerable<AppSetting>>()))
                .Returns(dtos);

            var result = await _sut.GetAllSettingsAsync();

            result.ShouldBeOfType<List<AppSettingDto>>();
            result.Count().ShouldBe(3);
        }

        [Fact]
        public async Task GetAllSettingsAsync_ShouldCall_Repository_Once()
        {
            _mapperMock
                .Setup(m => m.Map<IEnumerable<AppSettingDto>>(It.IsAny<IEnumerable<AppSetting>>()))
                .Returns(new List<AppSettingDto>());

            await _sut.GetAllSettingsAsync();

            _settingRepositoryMock.Verify(
                r => r.GetAllAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        #endregion GetAllSettingsAsync

        #region SaveSettingAsync

        [Fact]
        public async Task SaveSettingAsync_ShouldMap_ThenUpsert()
        {
            var dto = new AppSettingDto { Key = "MaxResults", Value = "200" };
            var entity = new AppSetting { Key = "MaxResults", Value = "200" };

            _mapperMock.Setup(m => m.Map<AppSetting>(dto)).Returns(entity);

            await _sut.SaveSettingAsync(dto);

            _mapperMock.Verify(m => m.Map<AppSetting>(dto), Times.Once);
            _settingRepositoryMock.Verify(
                r => r.UpsertAsync(entity, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SaveSettingAsync_NewKey_ShouldInsert_ViaUpsert()
        {
            var dto = new AppSettingDto { Key = "NewSetting", Value = "xyz" };
            var entity = new AppSetting { Key = "NewSetting", Value = "xyz" };

            _mapperMock.Setup(m => m.Map<AppSetting>(dto)).Returns(entity);

            await _sut.SaveSettingAsync(dto);

            _settingRepositoryMock.Verify(
                r => r.UpsertAsync(It.Is<AppSetting>(s => s.Key == "NewSetting"), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        #endregion SaveSettingAsync

        #region GetDefaultSearchDirectoryAsync

        [Fact]
        public async Task GetDefaultSearchDirectoryAsync_WhenSet_ShouldReturn_StoredPath()
        {
            // Seed data has DefaultSearchDirectory = @"C:\Documents"
            var result = await _sut.GetDefaultSearchDirectoryAsync();

            result.ShouldBe(@"C:\Documents");
        }

        [Fact]
        public async Task GetDefaultSearchDirectoryAsync_WhenMissing_ShouldFallback_ToMyDocuments()
        {
            // Override the seed so the key is absent
            _settingRepositoryMock
                .Setup(r => r.GetByKeyAsync(DefaultDirectoryKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync((AppSetting?) null);

            var result = await _sut.GetDefaultSearchDirectoryAsync();

            var expected = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            result.ShouldBe(expected);
        }

        #endregion GetDefaultSearchDirectoryAsync

        #region SetDefaultSearchDirectoryAsync

        [Fact]
        public async Task SetDefaultSearchDirectoryAsync_ShouldUpsert_WithCorrectKeyAndValue()
        {
            var path = @"D:\Archive";

            AppSettingDto? capturedDto = null;

            _mapperMock
                .Setup(m => m.Map<AppSetting>(It.IsAny<object>()))
                .Callback<object>(src => capturedDto = src as AppSettingDto)
                .Returns(new AppSetting());

            await _sut.SetDefaultSearchDirectoryAsync(path);

            _settingRepositoryMock.Verify(
                r => r.UpsertAsync(It.IsAny<AppSetting>(), It.IsAny<CancellationToken>()),
                Times.Once);

            capturedDto.ShouldNotBeNull();
            capturedDto!.Key.ShouldBe(DefaultDirectoryKey);
            capturedDto.Value.ShouldBe(path);
        }

        [Fact]
        public async Task SetDefaultSearchDirectoryAsync_ShouldInclude_NonEmptyDescription()
        {
            AppSettingDto? capturedDto = null;

            _mapperMock
                .Setup(m => m.Map<AppSetting>(It.IsAny<object>()))
                .Callback<object>(src => capturedDto = src as AppSettingDto)
                .Returns(new AppSetting());

            await _sut.SetDefaultSearchDirectoryAsync(@"C:\Docs");

            capturedDto.ShouldNotBeNull();
            capturedDto!.Description.ShouldNotBeNullOrWhiteSpace();
        }

        #endregion SetDefaultSearchDirectoryAsync
    }
}