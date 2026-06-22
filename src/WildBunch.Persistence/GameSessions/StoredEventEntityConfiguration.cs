using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WildBunch.Persistence.GameSessions;

public sealed class StoredEventEntityConfiguration : IEntityTypeConfiguration<StoredEventEntity>
{
    public void Configure(EntityTypeBuilder<StoredEventEntity> builder)
    {
        builder.ToTable("GameSessionStoredEvents");
        builder.HasKey(e => new { e.StreamId, e.Sequence });
        builder.Property(e => e.EventId).IsRequired();
        builder.Property(e => e.OccurredAtUtc).IsRequired();
        builder.Property(e => e.EventType).IsRequired().HasMaxLength(128);
        builder.Property(e => e.PayloadJson).IsRequired().HasColumnType("jsonb");
        builder.Property(e => e.CorrelationId);
        builder.Property(e => e.CausationId);
        builder.Property(e => e.SchemaVersion).IsRequired();

        builder.HasIndex(e => e.EventId).IsUnique();
        builder.HasIndex(e => new { e.StreamId, e.Sequence }).IsUnique();

        builder.HasOne(e => e.Session)
            .WithMany(e => e.StoredEvents)
            .HasForeignKey(e => e.StreamId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
