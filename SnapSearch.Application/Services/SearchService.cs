using AutoMapper;
using SnapSearch.Application.Common.Helpers;
using SnapSearch.Application.Contracts;
using SnapSearch.Application.Contracts.Infrastructure;
using SnapSearch.Application.DTOs;
using SnapSearch.Domain.Entities;
using SnapSearch.Domain.Enums;
using SnapSearch.Domain.Helpers;

namespace SnapSearch.Application.Services
{
    public class SearchService : ISearchService
    {
        #region Fields

        private readonly IFileSearchService _fileSearchService;
        private readonly ISearchHistoryRepository _searchHistoryRepository;
        private readonly IAccessLogRepository _accessLogRepository;
        private readonly IMapper _mapper;

        #endregion Fields

        #region Constructor

        public SearchService(
            IFileSearchService fileSearchService,
            ISearchHistoryRepository searchHistoryRepository,
            IAccessLogRepository accessLogRepository,
            IMapper mapper)
        {
            _fileSearchService = fileSearchService;
            _searchHistoryRepository = searchHistoryRepository;
            _accessLogRepository = accessLogRepository;
            _mapper = mapper;
        }

        #endregion Constructor

        #region Public Methods

        public async Task<IEnumerable<FileResultDto>> SearchFilesAsync(
            FileSearchRequestDto request, int userId,
            CancellationToken cancellationToken = default)
        {
            var results = (await _fileSearchService.SearchAsync(request, cancellationToken)).ToList();

            await _searchHistoryRepository.CreateAsync(new SearchHistory
            {
                UserId = userId,
                Keyword = request.Keyword,
                SearchDirectory = request.SearchDirectory,
                FileExtensionFilter = request.ExtensionFilter,
                ResultCount = results.Count,
                SearchedAt = TimeHelper.Now
            }, cancellationToken);

            await _accessLogRepository.CreateAsync(new AccessLog
            {
                UserId = userId,
                Username = string.Empty,
                Action = ActionType.Search.ToString(),
                SearchKeyword = request.Keyword,
                IpAddress = NetworkHelper.GetLocalIpAddress(),
                MacAddress = NetworkHelper.GetMacAddress(),
                AccessedAt = TimeHelper.Now,
                Details = $"Directory: {request.SearchDirectory}, Results: {results.Count}" +
                               (request.UseRegex ? " [regex]" : "")
            }, cancellationToken);

            return results;
        }

        public async Task<IEnumerable<ContentMatchDto>> GetContentMatchesAsync(
            string filePath, string keyword,
            CancellationToken cancellationToken = default)
        {
            return await _fileSearchService.SearchFileContentAsync(filePath, keyword, cancellationToken);
        }

        public async Task<IEnumerable<SearchHistoryDto>> GetSearchHistoryAsync(
            int userId, CancellationToken cancellationToken = default)
        {
            var history = await _searchHistoryRepository.GetByUserIdAsync(userId, cancellationToken);
            return _mapper.Map<IEnumerable<SearchHistoryDto>>(history);
        }

        #endregion Public Methods
    }
}