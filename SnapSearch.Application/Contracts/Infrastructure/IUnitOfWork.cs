using Microsoft.Data.SqlClient;

namespace SnapSearch.Application.Contracts.Infrastructure
{
    public interface IUnitOfWork
    {
        #region Public Methods

        SqlConnection Connection { get; }
        SqlTransaction Transaction { get; }

        Task CommitAsync(CancellationToken cancellationToken = default);

        Task RollbackAsync(CancellationToken cancellationToken = default);

        ValueTask DisposeAsync();

        #endregion Public Methods
    }
}