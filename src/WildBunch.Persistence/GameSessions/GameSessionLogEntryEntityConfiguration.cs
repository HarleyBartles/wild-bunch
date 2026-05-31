using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WildBunch.Persistence.GameSessions;

public sealed class GameSessionLogEntryEntityConfiguration : IEntityTypeConfiguration<GameSessionLogEntryEntity>
{
    public void Configure(EntityTypeBuilder<GameSessionLogEntryEntity> builder)
    {
        builder.ToTable("GameSessionLogEntries");
        builder.HasKey(e => new { e.SessionId, e.Sequence });

        builder.Property(e => e.Kind)
            .IsRequired();

        builder.Property(e => e.Message)
            .IsRequired();

        builder.Property(e => e.Day)
            .IsRequired();

        builder.Property(e => e.Turn)
            .IsRequired();
    }
}
