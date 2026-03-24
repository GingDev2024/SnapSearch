namespace SnapSearch.Application.Contracts.Infrastructure
{
    public interface IUnitOfWork
    {
        #region Public Methods

        Task CommitAsync(CancellationToken cancellationToken = default);

        Task RollbackAsync(CancellationToken cancellationToken = default);

        ValueTask DisposeAsync();

        #endregion Public Methods
    }
}