using Dapper;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Data;

namespace TPEmail.Common.Helpers
{
    public class RefreshTokenCleanupService : BackgroundService
    {
        private readonly Func<IDbConnection> _tpmailerdb;
        private readonly ILogger<RefreshTokenCleanupService> _logger;

        public RefreshTokenCleanupService(Func<IDbConnection> dbFactory, ILogger<RefreshTokenCleanupService> logger)
        {
            _tpmailerdb = dbFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var initDelay = int.TryParse(Environment.GetEnvironmentVariable("tptokencleanupdelaymin"), out var d) ? d : 5;
            await Task.Delay(TimeSpan.FromMinutes(initDelay), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var conn = _tpmailerdb();
                    var deletedCount = await conn.ExecuteScalarAsync<int>(
                        "commit_refreshtoken",
                        new { action = "CLEANUP" },
                        commandType: CommandType.StoredProcedure);
                    _logger.LogInformation("Refresh token cleanup completed. Removed {DeletedCount} expired tokens.", deletedCount);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during refresh token cleanup: {Message}", ex.Message);
                }

                var intervalHrs = int.TryParse(Environment.GetEnvironmentVariable("tptokencleanupintervalhrs"), out var h) ? h : 24;
                await Task.Delay(TimeSpan.FromHours(intervalHrs), stoppingToken);
            }
        }
    }
}
