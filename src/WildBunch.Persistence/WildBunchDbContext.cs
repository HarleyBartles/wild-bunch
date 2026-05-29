using Microsoft.EntityFrameworkCore;
using WildBunch.Persistence.GameSessions;

namespace WildBunch.Persistence;

public sealed class WildBunchDbContext : DbContext
{
    public WildBunchDbContext(DbContextOptions<WildBunchDbContext> options)
        : base(options)
    {
    }

    public DbSet<GameSessionEntity> GameSessions => Set<GameSessionEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WildBunchDbContext).Assembly);
    }
}
