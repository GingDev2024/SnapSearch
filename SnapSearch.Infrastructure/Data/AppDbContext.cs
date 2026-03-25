using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace SnapSearch.Infrastructure.Data
{
    public sealed class AppDbContext : IAsyncDisposable
    {
        #region Public Constructors

        public AppDbContext(IConfiguration configuration)
        {
            Connection = new SqlConnection(configuration.GetConnectionString("DefaultConnection")!);
        }

        #endregion Public Constructors

        #region Properties

        public SqlConnection Connection { get; }

        #endregion Properties

        #region Public Methods

        public async Task OpenAsync()
        {
            if (Connection.State != System.Data.ConnectionState.Open)
                await Connection.OpenAsync();
        }

        public async ValueTask DisposeAsync()
        {
            if (Connection != null)
                await Connection.DisposeAsync();
        }

        #endregion Public Methods
    }
}