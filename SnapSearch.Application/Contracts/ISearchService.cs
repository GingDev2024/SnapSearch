using SnapSearch.Application.DTOs;

namespace SnapSearch.Application.Contracts
{
    public interface ISearchService
    {
        #region Public Methods

        Task<IEnumerable<FileResultDto>> SearchFilesAsync(FileSearchRequestDto request, int userId, CancellationToken cancellationToken = default);

        Task<IEnumerable<ContentMatchDto>> GetContentMatchesAsync(string filePath, string keyword, CancellationToken cancellationToken = default);

        Task<IEnumerable<SearchHistoryDto>> GetSearchHistoryAsync(int userId, CancellationToken cancellationToken = default);

        #endregion Public Methods
    }
}