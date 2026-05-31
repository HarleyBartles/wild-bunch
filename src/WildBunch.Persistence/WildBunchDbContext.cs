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

    public DbSet<GameSessionComponentEntity> GameSessionComponents => Set<GameSessionComponentEntity>();

    public DbSet<GameSessionLogEntryEntity> GameSessionLogEntries => Set<GameSessionLogEntryEntity>();

    public DbSet<GameSessionDiaryDayEntity> GameSessionDiaryDays => Set<GameSessionDiaryDayEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WildBunchDbContext).Assembly);
    }
}
