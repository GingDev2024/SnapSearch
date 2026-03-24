using SnapSearch.Application.DTOs;
using SnapSearch.Domain.Enums;

namespace SnapSearch.Application.Contracts
{
    public interface IAccessLogService
    {
        #region Public Methods

        Task LogAsync(int? userId, string username, ActionType action, string? filePath = null,
            string? keyword = null, string? details = null, CancellationToken cancellationToken = default);

        Task<IEnumerable<AccessLogDto>> GetAllLogsAsync(CancellationToken cancellationToken = default);

        Task<IEnumerable<AccessLogDto>> GetLogsByUserAsync(int userId, CancellationToken cancellationToken = default);

        Task<IEnumerable<AccessLogDto>> GetLogsByDateRangeAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default);

        #endregion Public Methods
    }
}