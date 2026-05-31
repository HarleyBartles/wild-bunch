using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WildBunch.Persistence.GameSessions;

public sealed class GameSessionComponentEntityConfiguration : IEntityTypeConfiguration<GameSessionComponentEntity>
{
    public void Configure(EntityTypeBuilder<GameSessionComponentEntity> builder)
    {
        builder.ToTable("GameSessionComponents");
        builder.HasKey(e => new { e.SessionId, e.ComponentName });

        builder.Property(e => e.ComponentName)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(e => e.ComponentVersion)
            .IsRequired();

        builder.Property(e => e.PayloadJson)
            .IsRequired()
            .HasColumnType("jsonb");

        builder.Property(e => e.UpdatedAtUtc)
            .IsRequired();
    }
}
