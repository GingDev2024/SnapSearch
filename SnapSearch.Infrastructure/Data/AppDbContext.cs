using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace SnapSearch.Infrastructure.Data
{
    public sealed class AppDbContext : IDisposable
    {
        #region Properties

        public SqlConnection Connection { get; }

        #endregion Properties

        #region Public Constructors

        public AppDbContext()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            string connectionString = configuration.GetConnectionString("DefaultConnection")!;
            Connection = new SqlConnection(connectionString);
            Connection.Open();
        }

        #endregion Public Constructors

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