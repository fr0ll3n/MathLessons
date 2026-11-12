using MathematicsLessons.Infrastructure.Data;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace MathematicsLessons.Services;

/// <summary>Reports whether the SQL Server database can actually be reached.</summary>
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly ApplicationDbContext _ctx;

    public DatabaseHealthCheck(ApplicationDbContext ctx) => _ctx = ctx;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _ctx.Database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy("Database reachable.")
                : HealthCheckResult.Unhealthy("Database not reachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database health check failed.", ex);
        }
    }
}
