using Microsoft.Data.SqlClient;

namespace SnapSearch.Infrastructure.Data
{
    public sealed class UnitOfWork : IDisposable
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

        public void Commit()
        {
            _transaction?.Commit();
            DisposeTransaction();
        }

        public void Rollback()
        {
            _transaction?.Rollback();
            DisposeTransaction();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _transaction?.Dispose();
                _db?.Dispose();
                _disposed = true;
            }
        }

        #endregion Public Methods

        #region Private Methods

        private void DisposeTransaction()
        {
            _transaction?.Dispose();
            _transaction = null;
        }

        #endregion Private Methods
    }
}