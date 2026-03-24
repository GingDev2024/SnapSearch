using AutoMapper;
using SnapSearch.Application.Common.Helpers;
using SnapSearch.Application.Contracts;
using SnapSearch.Application.Contracts.Infrastructure;
using SnapSearch.Application.DTOs;
using SnapSearch.Domain.Entities;
using SnapSearch.Domain.Enums;

namespace SnapSearch.Application.Services
{
    public class AccessLogService : IAccessLogService
    {
        #region Fields

        private readonly IAccessLogRepository _accessLogRepository;
        private readonly IMapper _mapper;

        #endregion Fields

        #region Public Constructors

        public AccessLogService(IAccessLogRepository accessLogRepository, IMapper mapper)
        {
            _accessLogRepository = accessLogRepository;
            _mapper = mapper;
        }

        #endregion Public Constructors

        #region Public Methods

        public async Task LogAsync(int? userId, string username, ActionType action,
            string? filePath = null, string? keyword = null, string? details = null,
            CancellationToken cancellationToken = default)
        {
            await _accessLogRepository.CreateAsync(new AccessLog
            {
                UserId = userId,
                Username = username,
                Action = action.ToString(),
                FilePath = filePath,
                SearchKeyword = keyword,
                IpAddress = NetworkHelper.GetLocalIpAddress(),
                MacAddress = NetworkHelper.GetMacAddress(),
                AccessedAt = DateTime.UtcNow,
                Details = details
            }, cancellationToken);
        }

        public async Task<IEnumerable<AccessLogDto>> GetAllLogsAsync(CancellationToken cancellationToken = default)
        {
            var logs = await _accessLogRepository.GetAllAsync(cancellationToken);
            return _mapper.Map<IEnumerable<AccessLogDto>>(logs);
        }

        public async Task<IEnumerable<AccessLogDto>> GetLogsByUserAsync(int userId, CancellationToken cancellationToken = default)
        {
            var logs = await _accessLogRepository.GetByUserIdAsync(userId, cancellationToken);
            return _mapper.Map<IEnumerable<AccessLogDto>>(logs);
        }

        public async Task<IEnumerable<AccessLogDto>> GetLogsByDateRangeAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default)
        {
            var logs = await _accessLogRepository.GetByDateRangeAsync(from, to, cancellationToken);
            return _mapper.Map<IEnumerable<AccessLogDto>>(logs);
        }

        #endregion Public Methods
    }
}