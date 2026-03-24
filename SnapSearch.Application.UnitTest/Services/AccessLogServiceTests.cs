using AutoMapper;
using Moq;
using Shouldly;
using SnapSearch.Application.Contracts.Infrastructure;
using SnapSearch.Application.DTOs;
using SnapSearch.Application.Services;
using SnapSearch.Application.UnitTests.Mocks;
using SnapSearch.Domain.Entities;
using SnapSearch.Domain.Enums;

namespace SnapSearch.Application.UnitTests.Services
{
    public class AccessLogServiceTests
    {
        #region Fields

        private readonly Mock<IAccessLogRepository> _accessLogRepositoryMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly AccessLogService _sut;

        #endregion Fields

        #region Constructors

        public AccessLogServiceTests()
        {
            _accessLogRepositoryMock = RepositoryMocks.GetAccessLogRepository();
            _mapperMock = new Mock<IMapper>();
            _sut = new AccessLogService(_accessLogRepositoryMock.Object, _mapperMock.Object);
        }

        #endregion Constructors

        #region LogAsync

        [Fact]
        public async Task LogAsync_ShouldCallCreateAsync_Once()
        {
            await _sut.LogAsync(1, "alice", ActionType.Login);

            _accessLogRepositoryMock.Verify(
                r => r.CreateAsync(It.IsAny<AccessLog>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task LogAsync_ShouldMap_AllProvidedFields()
        {
            AccessLog? captured = null;

            _accessLogRepositoryMock
                .Setup(r => r.CreateAsync(It.IsAny<AccessLog>(), It.IsAny<CancellationToken>()))
                .Callback<AccessLog, CancellationToken>((log, _) => captured = log)
                .ReturnsAsync(99);

            await _sut.LogAsync(
                userId: 1,
                username: "alice",
                action: ActionType.Search,
                filePath: @"C:\Docs\file.pdf",
                keyword: "invoice",
                details: "some detail");

            captured.ShouldNotBeNull();
            captured!.UserId.ShouldBe(1);
            captured.Username.ShouldBe("alice");
            captured.Action.ShouldBe(ActionType.Search.ToString());
            captured.FilePath.ShouldBe(@"C:\Docs\file.pdf");
            captured.SearchKeyword.ShouldBe("invoice");
            captured.Details.ShouldBe("some detail");
        }

        [Fact]
        public async Task LogAsync_ShouldSet_AccessedAt_ToUtcNow()
        {
            var before = DateTime.UtcNow;
            AccessLog? captured = null;

            _accessLogRepositoryMock
                .Setup(r => r.CreateAsync(It.IsAny<AccessLog>(), It.IsAny<CancellationToken>()))
                .Callback<AccessLog, CancellationToken>((log, _) => captured = log)
                .ReturnsAsync(99);

            await _sut.LogAsync(1, "alice", ActionType.ViewFile);

            var after = DateTime.UtcNow;
            captured.ShouldNotBeNull();
            captured!.AccessedAt.ShouldBeInRange(before, after);
        }

        [Fact]
        public async Task LogAsync_WithNullOptionalFields_ShouldStillPersist()
        {
            await _sut.LogAsync(null, "alice", ActionType.Login);

            _accessLogRepositoryMock.Verify(
                r => r.CreateAsync(
                    It.Is<AccessLog>(l => l.UserId == null && l.FilePath == null && l.SearchKeyword == null),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        #endregion LogAsync

        #region GetAllLogsAsync

        [Fact]
        public async Task GetAllLogsAsync_ShouldReturn_MappedDtos()
        {
            var dtos = new List<AccessLogDto>
            {
                new AccessLogDto { Id = 1, Username = "alice" },
                new AccessLogDto { Id = 2, Username = "alice" },
                new AccessLogDto { Id = 3, Username = "bob" }
            };

            _mapperMock
                .Setup(m => m.Map<IEnumerable<AccessLogDto>>(It.IsAny<IEnumerable<AccessLog>>()))
                .Returns(dtos);

            var result = await _sut.GetAllLogsAsync();

            result.ShouldBeOfType<List<AccessLogDto>>();
            result.Count().ShouldBe(3);
        }

        [Fact]
        public async Task GetAllLogsAsync_ShouldCall_Repository_Once()
        {
            _mapperMock
                .Setup(m => m.Map<IEnumerable<AccessLogDto>>(It.IsAny<IEnumerable<AccessLog>>()))
                .Returns(new List<AccessLogDto>());

            await _sut.GetAllLogsAsync();

            _accessLogRepositoryMock.Verify(
                r => r.GetAllAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }

        #endregion GetAllLogsAsync

        #region GetLogsByUserAsync

        [Fact]
        public async Task GetLogsByUserAsync_ShouldReturn_OnlyLogsForThatUser()
        {
            // Seed data has 2 logs for userId=1
            _mapperMock
                .Setup(m => m.Map<IEnumerable<AccessLogDto>>(It.IsAny<IEnumerable<AccessLog>>()))
                .Returns((IEnumerable<AccessLog> src) =>
                    src.Select(l => new AccessLogDto { Id = l.Id, UserId = l.UserId }).ToList());

            var result = await _sut.GetLogsByUserAsync(1);

            result.ShouldAllBe(dto => dto.UserId == 1);
            result.Count().ShouldBe(2);
        }

        [Fact]
        public async Task GetLogsByUserAsync_ForUnknownUser_ShouldReturnEmpty()
        {
            _mapperMock
                .Setup(m => m.Map<IEnumerable<AccessLogDto>>(It.IsAny<IEnumerable<AccessLog>>()))
                .Returns((IEnumerable<AccessLog> src) => src.Select(_ => new AccessLogDto()).ToList());

            var result = await _sut.GetLogsByUserAsync(999);

            result.ShouldBeEmpty();
        }

        #endregion GetLogsByUserAsync

        #region GetLogsByDateRangeAsync

        [Fact]
        public async Task GetLogsByDateRangeAsync_ShouldReturn_LogsWithinRange()
        {
            // Seed: Id=1 & Id=2 are on 2024-01-10, Id=3 is on 2024-01-15
            var from = new DateTime(2024, 1, 10, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2024, 1, 10, 23, 59, 59, DateTimeKind.Utc);

            _mapperMock
                .Setup(m => m.Map<IEnumerable<AccessLogDto>>(It.IsAny<IEnumerable<AccessLog>>()))
                .Returns((IEnumerable<AccessLog> src) =>
                    src.Select(l => new AccessLogDto { Id = l.Id }).ToList());

            var result = await _sut.GetLogsByDateRangeAsync(from, to);

            result.Count().ShouldBe(2);
        }

        [Fact]
        public async Task GetLogsByDateRangeAsync_OutsideRange_ShouldReturnEmpty()
        {
            var from = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var to = new DateTime(2020, 12, 31, 0, 0, 0, DateTimeKind.Utc);

            _mapperMock
                .Setup(m => m.Map<IEnumerable<AccessLogDto>>(It.IsAny<IEnumerable<AccessLog>>()))
                .Returns((IEnumerable<AccessLog> src) => src.Select(_ => new AccessLogDto()).ToList());

            var result = await _sut.GetLogsByDateRangeAsync(from, to);

            result.ShouldBeEmpty();
        }

        #endregion GetLogsByDateRangeAsync
    }
}