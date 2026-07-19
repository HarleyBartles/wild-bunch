using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WildBunch.Persistence.GameSessions;

public sealed class GameSessionDiaryDayEntityConfiguration : IEntityTypeConfiguration<GameSessionDiaryDayEntity>
{
    public void Configure(EntityTypeBuilder<GameSessionDiaryDayEntity> builder)
    {
        builder.ToTable("GameSessionTravelDiaryDays");
        builder.HasKey(e => new { e.SessionId, e.Sequence });

        builder.Property(e => e.PayloadJson)
            .IsRequired()
            .HasColumnType("jsonb");

        builder.Property(e => e.RecordedAtUtc)
            .IsRequired();

        builder.Property(e => e.SchemaVersion)
            .IsRequired();
    }
}
