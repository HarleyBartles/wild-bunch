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
        modelBuilder.Entity<GameSessionEntity>(entity =>
        {
            entity.ToTable("GameSessions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.StateJson).IsRequired();
            entity.Property(e => e.Status).IsRequired().HasMaxLength(32);
            entity.Property(e => e.CreatedAtUtc).IsRequired();
            entity.Property(e => e.UpdatedAtUtc).IsRequired();
        });
    }
}
