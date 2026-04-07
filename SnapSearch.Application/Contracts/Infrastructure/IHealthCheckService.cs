using SnapSearch.Application.Common.Health;

namespace SnapSearch.Application.Contracts.Infrastructure
{
    public interface IHealthCheckService
    {
        #region Public Methods

        Task<HealthReport> CheckAsync();

        #endregion Public Methods
    }
}