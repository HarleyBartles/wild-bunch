using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace WildBunch.Persistence;

public sealed class WildBunchDbContextFactory : IDesignTimeDbContextFactory<WildBunchDbContext>
{
    public WildBunchDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<WildBunchDbContext>()
            .UseNpgsql(ResolveDesignTimeConnectionString())
            .Options;

        return new WildBunchDbContext(options);
    }

    private static string ResolveDesignTimeConnectionString()
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__WildBunchPostgresDb");
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        return "Host=localhost;Port=5433;Database=wildbunch_design;Username=postgres";
    }
}
