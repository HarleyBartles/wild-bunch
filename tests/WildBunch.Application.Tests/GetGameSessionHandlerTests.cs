using System.Text.Json;
using WildBunch.Application.Games.Exceptions;
using WildBunch.Application.Games.Queries;
using WildBunch.Application.Tests.TestDoubles;
using WildBunch.Domain.Cases;

namespace WildBunch.Application.Tests;

public sealed class GetGameSessionHandlerTests
{
    [Fact]
    public async Task GetGameSessionReturnsSavedSessionDto()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = new StubNewGameFactory().CreatedSession;
        repository.Seed(session);
        var handler = new GetGameSessionHandler(repository);

        var result = await handler.HandleAsync(new GetGameSessionQuery(session.Id.Value));

        Assert.Equal(session.Id.Value, result.Id);
        Assert.Equal(session.Player.Name, result.Player.Name);
        Assert.Equal(session.Player.CurrentTownId.Value, result.Player.CurrentTownId);
        Assert.Equal(session.Clock.Day, result.Clock.Day);
        Assert.Equal(session.Clock.Turn, result.Clock.Turn);
        Assert.Equal(session.PursuitState.Heat, result.PursuitState.Heat);
        Assert.Equal(new SuspectId("suspect-1"), session.CaseFile.TrueCulpritId);

        var payload = JsonSerializer.Serialize(result);
        Assert.DoesNotContain("\"trueCulpritId\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"isTrueCulprit\"", payload, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetGameSessionThrowsWhenMissing()
    {
        var handler = new GetGameSessionHandler(new InMemoryGameSessionRepository());

        var exception = await Assert.ThrowsAsync<GameSessionNotFoundException>(
            () => handler.HandleAsync(new GetGameSessionQuery(Guid.NewGuid())));

        Assert.Contains("was not found", exception.Message);
    }
}
