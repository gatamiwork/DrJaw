using System;
using System.Data.Common;
using DrJaw.Models;

namespace DrJaw.Services.MSSQL
{
    public static class MssqlConverters
    {
        public static MSSQLUser ToUser(DbDataReader r) => new MSSQLUser
        {
            Id = r.GetInt32(r.GetOrdinal("Id")),
            Name = r.GetString(r.GetOrdinal("Name")),
            Role = r.GetString(r.GetOrdinal("Role")),
            Display = r.GetBoolean(r.GetOrdinal("Display")),
            CreatedAt = r.GetDateTime(r.GetOrdinal("CreatedAt")),
            UpdatedAt = r.IsDBNull(r.GetOrdinal("UpdatedAt")) ? (DateTime?)null : r.GetDateTime(r.GetOrdinal("UpdatedAt"))
        };

        public static MSSQLMart ToMart(DbDataReader r) => new MSSQLMart
        {
            Id = r.GetInt32(r.GetOrdinal("Id")),
            Name = r.GetString(r.GetOrdinal("Name"))
        };
        public static MSSQLMetal ToMetal(DbDataReader r) => new MSSQLMetal
        {
            Id = r.GetInt32(r.GetOrdinal("Id")),
            Name = r.GetString(r.GetOrdinal("Name"))
        };
    }
}
