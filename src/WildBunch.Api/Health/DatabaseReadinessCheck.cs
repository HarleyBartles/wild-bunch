using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using WildBunch.Persistence;

namespace WildBunch.Api.Health;

public sealed class DatabaseReadinessCheck(WildBunchDbContext database) : IHealthCheck
{
    private static readonly TimeSpan CommandTimeout = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan CheckTimeout = TimeSpan.FromSeconds(3);

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(CheckTimeout);
        database.Database.SetCommandTimeout(CommandTimeout);

        try
        {
            if (!await database.Database.CanConnectAsync(timeout.Token))
            {
                return HealthCheckResult.Unhealthy();
            }

            var requiredMigrations = database.Database.GetMigrations().ToHashSet(StringComparer.Ordinal);
            var appliedMigrations = (await database.Database.GetAppliedMigrationsAsync(timeout.Token))
                .ToHashSet(StringComparer.Ordinal);

            return requiredMigrations.IsSubsetOf(appliedMigrations)
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy();
        }
        catch (Exception)
        {
            return HealthCheckResult.Unhealthy();
        }
    }
}
