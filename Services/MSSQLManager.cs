using System;
using System.Data;
using System.Data.SqlClient;

namespace DrJaw
{
    public static class MSSQLManager
    {
        private static string _connectionString = string.Empty;

        public static void Initialize(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public static async Task<(bool ok, string? error)> TestConnectionAsync(CancellationToken cancellationToken)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);
                return (true, null);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public static async Task<DataTable> ExecuteQueryAsync(string sql, List<SqlParameter> parameters = null)
        {
            await using var conn = new SqlConnection(_connectionString);
            await using var cmd = new SqlCommand(sql, conn);
            if (parameters?.Count > 0)
                cmd.Parameters.AddRange(parameters.ToArray());

            await conn.OpenAsync();

            await using var reader = await cmd.ExecuteReaderAsync();
            var table = new DataTable();
            table.Load(reader);
            return table;
        }

        public static async Task<int> ExecuteNonQueryAsync(string sql, List<SqlParameter> parameters = null)
        {
            await using var conn = new SqlConnection(_connectionString);
            await using var cmd = new SqlCommand(sql, conn);
            if (parameters?.Count > 0)
                cmd.Parameters.AddRange(parameters.ToArray());

            await conn.OpenAsync();
            return await cmd.ExecuteNonQueryAsync();
        }

        public static async Task<object?> ExecuteScalarAsync(string sql, List<SqlParameter> parameters = null)
        {
            await using var conn = new SqlConnection(_connectionString);
            await using var cmd = new SqlCommand(sql, conn);
            if (parameters?.Count > 0)
                cmd.Parameters.AddRange(parameters.ToArray());

            await conn.OpenAsync();
            return await cmd.ExecuteScalarAsync();
        }
    }
}
