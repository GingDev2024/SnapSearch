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
    public class SearchServiceTests
    {
        #region Fields

        private readonly Mock<IFileSearchService> _fileSearchServiceMock;
        private readonly Mock<ISearchHistoryRepository> _searchHistoryRepositoryMock;
        private readonly Mock<IAccessLogRepository> _accessLogRepositoryMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly SearchService _sut;

        #endregion Fields

        #region Constructors

        public SearchServiceTests()
        {
            _fileSearchServiceMock = new Mock<IFileSearchService>();
            _searchHistoryRepositoryMock = RepositoryMocks.GetSearchHistoryRepository();
            _accessLogRepositoryMock = RepositoryMocks.GetAccessLogRepository();
            _mapperMock = new Mock<IMapper>();

            _sut = new SearchService(
                _fileSearchServiceMock.Object,
                _searchHistoryRepositoryMock.Object,
                _accessLogRepositoryMock.Object,
                _mapperMock.Object);
        }

        #endregion Constructors

        #region SearchFilesAsync

        [Fact]
        public async Task SearchFilesAsync_ShouldReturn_ResultsFromFileSearchService()
        {
            var request = new FileSearchRequestDto
            {
                Keyword = "invoice",
                SearchDirectory = @"C:\Docs",
                ExtensionFilter = ".pdf"
            };

            var fileResults = new List<FileResultDto>
            {
                new FileResultDto { FilePath = @"C:\Docs\inv1.pdf" },
                new FileResultDto { FilePath = @"C:\Docs\inv2.pdf" }
            };

            _fileSearchServiceMock
                .Setup(s => s.SearchAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(fileResults);

            var result = await _sut.SearchFilesAsync(request, userId: 1);

            result.ShouldBeOfType<List<FileResultDto>>();
            result.Count().ShouldBe(2);
        }

        [Fact]
        public async Task SearchFilesAsync_ShouldCreate_SearchHistoryRecord_WithCorrectData()
        {
            var userId = 1;
            var request = new FileSearchRequestDto
            {
                Keyword = "contract",
                SearchDirectory = @"C:\Legal",
                ExtensionFilter = ".docx"
            };

            var results = new List<FileResultDto> { new(), new(), new() };

            _fileSearchServiceMock
                .Setup(s => s.SearchAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(results);

            SearchHistory? capturedHistory = null;
            _searchHistoryRepositoryMock
                .Setup(r => r.CreateAsync(It.IsAny<SearchHistory>(), It.IsAny<CancellationToken>()))
                .Callback<SearchHistory, CancellationToken>((h, _) => capturedHistory = h)
                .ReturnsAsync(99);

            await _sut.SearchFilesAsync(request, userId);

            capturedHistory.ShouldNotBeNull();
            capturedHistory!.UserId.ShouldBe(userId);
            capturedHistory.Keyword.ShouldBe(request.Keyword);
            capturedHistory.SearchDirectory.ShouldBe(request.SearchDirectory);
            capturedHistory.FileExtensionFilter.ShouldBe(request.ExtensionFilter);
            capturedHistory.ResultCount.ShouldBe(results.Count);
        }

        [Fact]
        public async Task SearchFilesAsync_ShouldCreate_AccessLog_WithSearchAction()
        {
            var userId = 2;
            var request = new FileSearchRequestDto { Keyword = "report", SearchDirectory = @"C:\Reports" };

            _fileSearchServiceMock
                .Setup(s => s.SearchAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<FileResultDto> { new() });

            AccessLog? capturedLog = null;
            _accessLogRepositoryMock
                .Setup(r => r.CreateAsync(It.IsAny<AccessLog>(), It.IsAny<CancellationToken>()))
                .Callback<AccessLog, CancellationToken>((log, _) => capturedLog = log)
                .ReturnsAsync(99);

            await _sut.SearchFilesAsync(request, userId);

            capturedLog.ShouldNotBeNull();
            capturedLog!.Action.ShouldBe(ActionType.Search.ToString());
            capturedLog.UserId.ShouldBe(userId);
            capturedLog.SearchKeyword.ShouldBe(request.Keyword);
            capturedLog.Details.ShouldContain(request.SearchDirectory);
        }

        [Fact]
        public async Task SearchFilesAsync_WithNoResults_ShouldRecord_ZeroResultCount()
        {
            var request = new FileSearchRequestDto { Keyword = "missing", SearchDirectory = @"C:\Empty" };

            _fileSearchServiceMock
                .Setup(s => s.SearchAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<FileResultDto>());

            SearchHistory? capturedHistory = null;
            _searchHistoryRepositoryMock
                .Setup(r => r.CreateAsync(It.IsAny<SearchHistory>(), It.IsAny<CancellationToken>()))
                .Callback<SearchHistory, CancellationToken>((h, _) => capturedHistory = h)
                .ReturnsAsync(99);

            var result = await _sut.SearchFilesAsync(request, userId: 1);

            result.ShouldBeEmpty();
            capturedHistory!.ResultCount.ShouldBe(0);
        }

        [Fact]
        public async Task SearchFilesAsync_ShouldCall_BothRepositories_ExactlyOnce()
        {
            var request = new FileSearchRequestDto { Keyword = "x", SearchDirectory = @"C:\" };

            _fileSearchServiceMock
                .Setup(s => s.SearchAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<FileResultDto>());

            await _sut.SearchFilesAsync(request, userId: 1);

            _searchHistoryRepositoryMock.Verify(
                r => r.CreateAsync(It.IsAny<SearchHistory>(), It.IsAny<CancellationToken>()),
                Times.Once);

            _accessLogRepositoryMock.Verify(
                r => r.CreateAsync(It.IsAny<AccessLog>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        #endregion SearchFilesAsync

        #region GetContentMatchesAsync

        [Fact]
        public async Task GetContentMatchesAsync_ShouldDelegate_ToFileSearchService()
        {
            var filePath = @"C:\docs\report.pdf";
            var keyword = "revenue";

            var expected = new List<ContentMatchDto>
            {
                new ContentMatchDto { LineNumber = 5,  LineContent = "Total revenue Q1" },
                new ContentMatchDto { LineNumber = 12, LineContent = "Revenue projection" }
            };

            _fileSearchServiceMock
                .Setup(s => s.SearchFileContentAsync(filePath, keyword, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expected);

            var result = await _sut.GetContentMatchesAsync(filePath, keyword);

            result.ShouldBe(expected);
            _fileSearchServiceMock.Verify(
                s => s.SearchFileContentAsync(filePath, keyword, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GetContentMatchesAsync_WhenNoMatches_ShouldReturnEmpty()
        {
            _fileSearchServiceMock
                .Setup(s => s.SearchFileContentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ContentMatchDto>());

            var result = await _sut.GetContentMatchesAsync(@"C:\file.txt", "none");

            result.ShouldBeEmpty();
        }

        #endregion GetContentMatchesAsync

        #region GetSearchHistoryAsync

        [Fact]
        public async Task GetSearchHistoryAsync_ShouldReturn_MappedHistoryForUser()
        {
            // Seed has 2 entries for userId=1
            var dtos = new List<SearchHistoryDto>
            {
                new SearchHistoryDto { Keyword = "invoice" },
                new SearchHistoryDto { Keyword = "contract" }
            };

            _mapperMock
                .Setup(m => m.Map<IEnumerable<SearchHistoryDto>>(It.IsAny<IEnumerable<SearchHistory>>()))
                .Returns(dtos);

            var result = await _sut.GetSearchHistoryAsync(userId: 1);

            result.ShouldBeOfType<List<SearchHistoryDto>>();
            result.Count().ShouldBe(2);
        }

        [Fact]
        public async Task GetSearchHistoryAsync_ForUnknownUser_ShouldReturnEmpty()
        {
            _mapperMock
                .Setup(m => m.Map<IEnumerable<SearchHistoryDto>>(It.IsAny<IEnumerable<SearchHistory>>()))
                .Returns((IEnumerable<SearchHistory> src) =>
                    src.Select(_ => new SearchHistoryDto()).ToList());

            var result = await _sut.GetSearchHistoryAsync(userId: 999);

            result.ShouldBeEmpty();
        }

        #endregion GetSearchHistoryAsync
    }
}