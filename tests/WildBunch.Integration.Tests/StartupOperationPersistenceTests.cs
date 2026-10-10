using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WildBunch.Integration.Tests.TestInfrastructure;
using WildBunch.Persistence;
using WildBunch.Persistence.GameSessions;

namespace WildBunch.Integration.Tests;

public sealed class StartupOperationPersistenceTests
{
    [Fact]
    public async Task FailedOperationReceiptInsertRollsBackSessionAndEventAppend()
    {
        using var factory = new PostgreSqlApiFactory();
        var sessionId = Guid.NewGuid();
        var operationId = Guid.NewGuid();
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<WildBunchDbContext>();
            var session = new GameSessionEntity
            {
                Id = sessionId,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
                Status = "Active",
                GameDifficulty = 0,
                StreamVersion = 1
            };
            db.GameSessions.Add(session);
            db.StoredEvents.Add(new StoredEventEntity
            {
                StreamId = sessionId,
                Sequence = 1,
                EventId = Guid.NewGuid(),
                OccurredAtUtc = DateTime.UtcNow,
                EventType = "PlayerSetupCompleted",
                PayloadJson = "{}",
                SchemaVersion = 1,
                Session = session
            });
            db.StartupOperations.Add(new StartupOperationEntity
            {
                OperationId = operationId,
                Kind = 1,
                RequestFingerprint = new string('A', 64),
                SessionId = Guid.NewGuid()
            });
            var unitOfWork = scope.ServiceProvider.GetRequiredService<WildBunch.Application.Abstractions.IGameSessionUnitOfWork>();

            await Assert.ThrowsAnyAsync<DbUpdateException>(() => unitOfWork.CommitAsync());
        }

        await using var verificationScope = factory.Services.CreateAsyncScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<WildBunchDbContext>();
        Assert.False(await verificationDb.GameSessions.AnyAsync(entity => entity.Id == sessionId));
        Assert.False(await verificationDb.StoredEvents.AnyAsync(entity => entity.StreamId == sessionId));
        Assert.False(await verificationDb.StartupOperations.AnyAsync(entity => entity.OperationId == operationId));
    }
}
