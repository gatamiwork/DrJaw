using System.Data.Common;
using System.Threading.Tasks;

namespace DrJaw.Services.MSSQL
{
    public interface IMssqlManager
    {
        string ConnectionString { get; }
        DbConnection CreateConnection();
        Task TestConnectionAsync();
    }
}
