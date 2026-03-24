using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace SnapSearch.Infrastructure.Data
{
    public sealed class AppDbContext : IAsyncDisposable
    {
        #region Public Constructors

        public AppDbContext(IConfiguration configuration)
        {
            string connectionString = configuration.GetConnectionString("DefaultConnection")!;
            Connection = new SqlConnection(connectionString);
            Connection.Open();
        }

        public AppDbContext(SqlConnection connection)
        {
            Connection = connection;
        }

        #endregion Public Constructors

        #region Properties

        public SqlConnection Connection { get; }

        #endregion Properties

        #region Public Methods

        public async ValueTask DisposeAsync()
        {
            if (Connection != null)
            {
                await Connection.DisposeAsync();
            }
        }

        public void Dispose()
        {
            Connection?.Dispose();
        }

        #endregion Public Methods
    }
}