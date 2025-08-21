using System.Data.Common;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace DrJaw.Services.MSSQL
{
    public sealed class MssqlManager : IMssqlManager
    {
        public string ConnectionString { get; }

        public MssqlManager(string connectionString)
        {
            ConnectionString = connectionString;
        }

        public DbConnection CreateConnection() => new SqlConnection(ConnectionString);

        public async Task TestConnectionAsync()
        {
            await using var cn = CreateConnection();
            await cn.OpenAsync();
        }
    }
}
