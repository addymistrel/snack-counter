using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SnackCounter.Server.Infrastructure.Database;

public sealed class DatabaseHealthCheck(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? configuration["DATABASE_URL"];
        if (string.IsNullOrWhiteSpace(connectionString))
            return HealthCheckResult.Unhealthy("PostgreSQL connection is not configured.");

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SnackCounterDbContext>();
        return await db.Database.CanConnectAsync(cancellationToken)
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("PostgreSQL is unavailable.");
    }
}
