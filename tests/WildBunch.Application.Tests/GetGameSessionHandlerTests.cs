using System.Text.Json;
using WildBunch.Application.Games.Exceptions;
using WildBunch.Application.Games.Queries;
using WildBunch.Application.Tests.TestDoubles;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Inventory;

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
        Assert.Equal(session.TravelDifficulty, result.TravelDifficulty);
        Assert.Equal(session.Player.Wallet.Cash, result.Inventory.Wallet.Cash);
        Assert.Equal(session.Player.Inventory.Items.Count, result.Inventory.Items.Count);
        Assert.NotNull(result.Inventory.HorseState);
        Assert.Equal(session.Player.Inventory.GetHorseState()!.Hunger, result.Inventory.HorseState!.Hunger);
        Assert.Equal(session.Player.Inventory.GetHorseState()!.Thirst, result.Inventory.HorseState.Thirst);
        Assert.Equal(session.Player.Inventory.GetHorseState()!.Exhaustion, result.Inventory.HorseState.Exhaustion);
        Assert.NotNull(result.Inventory.CanteenState);
        Assert.Equal(session.Player.Inventory.GetCanteenState()!.Charges, result.Inventory.CanteenState!.Charges);
        Assert.Equal(session.Player.Inventory.GetCanteenState()!.Capacity, result.Inventory.CanteenState.Capacity);
        var capabilityResolver = new InventoryCapabilityResolver();
        var expectedCapabilities = capabilityResolver.Resolve(session.Player.Inventory);
        Assert.Equal(expectedCapabilities.MountedTravelAvailable, result.Inventory.Capabilities.MountedTravelAvailable);
        Assert.Equal(expectedCapabilities.GunfightCapable, result.Inventory.Capabilities.GunfightCapable);
        Assert.Equal(session.Clock.Day, result.Clock.Day);
        Assert.Equal(session.Clock.Turn, result.Clock.Turn);
        Assert.Equal(session.PursuitState.Heat, result.PursuitState.Heat);
        Assert.Equal(session.CaseFile.OpeningLead.Description, result.CaseFile.OpeningLead);
        Assert.Equal(session.CaseFile.KillerReleaseState.IsReleased, result.CaseFile.KillerReleaseState.IsReleased);
        Assert.Equal(session.CaseFile.Suspects.Count, result.CaseFile.Suspects.Count);
        Assert.Contains(result.CaseFile.Suspects, suspect => suspect.Name == "Ira Flint");
        Assert.Equal(new SuspectId("suspect-1"), session.CaseFile.TrueCulpritId);

        var payload = JsonSerializer.Serialize(result);
        Assert.DoesNotContain("\"trueCulpritId\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"isTrueCulprit\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"trueculpritid\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"profile\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"traits\"", payload, StringComparison.OrdinalIgnoreCase);
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
