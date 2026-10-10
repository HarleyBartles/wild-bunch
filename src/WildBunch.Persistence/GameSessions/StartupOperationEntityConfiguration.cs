using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WildBunch.Persistence.GameSessions;

public sealed class StartupOperationEntityConfiguration : IEntityTypeConfiguration<StartupOperationEntity>
{
    public void Configure(EntityTypeBuilder<StartupOperationEntity> builder)
    {
        builder.ToTable("StartupOperations");
        builder.HasKey(operation => operation.OperationId);
        builder.Property(operation => operation.OperationId).ValueGeneratedNever();
        builder.Property(operation => operation.Kind).IsRequired();
        builder.Property(operation => operation.RequestFingerprint).IsRequired().HasMaxLength(64);
        builder.Property(operation => operation.SessionId).IsRequired();
        builder.HasIndex(operation => new { operation.SessionId, operation.Kind }).IsUnique();
        builder.HasOne<GameSessionEntity>()
            .WithMany()
            .HasForeignKey(operation => operation.SessionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
