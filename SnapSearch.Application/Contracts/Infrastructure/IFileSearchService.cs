using SnapSearch.Application.DTOs;

namespace SnapSearch.Application.Contracts.Infrastructure
{
    public interface IFileSearchService
    {
        #region Public Methods

        Task<IEnumerable<FileResultDto>> SearchAsync(FileSearchRequestDto request, CancellationToken cancellationToken = default);

        Task<IEnumerable<ContentMatchDto>> SearchFileContentAsync(string filePath, string keyword, CancellationToken cancellationToken = default);

        bool CanPreviewFile(string filePath);

        #endregion Public Methods
    }
}