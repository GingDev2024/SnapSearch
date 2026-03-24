using Microsoft.Data.SqlClient;
using SnapSearch.Application.Contracts.Infrastructure;
using SnapSearch.Infrastructure.Data;

namespace SnapSearch.Infrastructure.Repositories
{
    public sealed class UnitOfWork : IUnitOfWork
    {
        #region Fields

        private readonly AppDbContext _db;
        private SqlTransaction _transaction;
        private bool _disposed;

        #endregion Fields

        #region Public Constructors

        public UnitOfWork(AppDbContext db)
        {
            _db = db;
            _transaction = _db.Connection.BeginTransaction();
        }

        #endregion Public Constructors

        #region Properties

        public SqlConnection Connection => _db.Connection;
        public SqlTransaction Transaction => _transaction;

        #endregion Properties

        #region Public Methods

        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            if (_transaction != null)
            {
                await _transaction.CommitAsync(cancellationToken);
                await DisposeTransactionAsync();
            }
        }

        public async Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync(cancellationToken);
                await DisposeTransactionAsync();
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                if (_transaction != null)
                    await _transaction.DisposeAsync();

                if (_db != null)
                    await _db.DisposeAsync();

                _disposed = true;
            }
        }

        private async Task DisposeTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        #endregion Public Methods
    }
}