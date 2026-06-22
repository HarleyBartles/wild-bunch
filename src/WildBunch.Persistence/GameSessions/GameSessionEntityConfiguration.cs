using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WildBunch.Persistence.GameSessions;

public sealed class GameSessionEntityConfiguration : IEntityTypeConfiguration<GameSessionEntity>
{
    public void Configure(EntityTypeBuilder<GameSessionEntity> builder)
    {
        builder.ToTable("GameSessions");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .ValueGeneratedNever();

        builder.Property(e => e.CreatedAtUtc)
            .IsRequired();

        builder.Property(e => e.UpdatedAtUtc)
            .IsRequired();

        builder.Property(e => e.Status)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(e => e.TravelDifficulty)
            .IsRequired();

        builder.Property(e => e.SchemaVersion)
            .IsRequired();

        builder.Property(e => e.StreamVersion)
            .IsRequired();

        builder.Property(e => e.SnapshotVersion);

        builder.HasMany(e => e.Components)
            .WithOne(e => e.Session)
            .HasForeignKey(e => e.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.LogEntries)
            .WithOne(e => e.Session)
            .HasForeignKey(e => e.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.TravelDiaryDays)
            .WithOne(e => e.Session)
            .HasForeignKey(e => e.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.StoredEvents)
            .WithOne(e => e.Session)
            .HasForeignKey(e => e.StreamId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
