using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace SnapSearch.Infrastructure.Data
{
    public sealed class AppDbContext : IDisposable
    {
        #region Public Constructors

        public AppDbContext(IConfiguration configuration)
        {
            string connectionString = configuration.GetConnectionString("DefaultConnection")!;
            Connection = new SqlConnection(connectionString);
            Connection.Open();
        }

        #endregion Public Constructors

        #region Properties

        public SqlConnection Connection { get; }

        #endregion Properties

        #region Public Methods

        public void Dispose()
        {
            if (Connection.State != ConnectionState.Closed)
                Connection.Close();
            Connection.Dispose();
        }

        #endregion Public Methods
    }
}