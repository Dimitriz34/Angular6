using Microsoft.Extensions.Configuration;
using Dapper;
using System.Data;

namespace TPEmail.Common.Helpers
{
    public static class AppConfig
    {
        public static IConfiguration? Configuration { get; set; }

        public static void Initialize(IConfiguration config)
        {
            Configuration = config;
        }
    }

    public static class DatabaseHelper
    {
        public static async Task<IList<T>> QueryListAsync<T>(Func<IDbConnection> dbFactory, string sp, object? param = null) where T : class
        {
            try
            {
                using var conn = dbFactory();
                return (await conn.QueryAsync<T>(sp, param, commandType: CommandType.StoredProcedure)).ToList();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Database error executing stored procedure '{sp}'", ex);
            }
        }

        public static async Task<T?> QuerySingleAsync<T>(Func<IDbConnection> dbFactory, string sp, object? param = null)
        {
            try
            {
                using var conn = dbFactory();
                return (typeof(T).IsValueType || typeof(T) == typeof(string))
                    ? await conn.ExecuteScalarAsync<T>(sp, param, commandType: CommandType.StoredProcedure)
                    : await conn.QueryFirstOrDefaultAsync<T>(sp, param, commandType: CommandType.StoredProcedure);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Database error executing stored procedure '{sp}'", ex);
            }
        }
    }
}
