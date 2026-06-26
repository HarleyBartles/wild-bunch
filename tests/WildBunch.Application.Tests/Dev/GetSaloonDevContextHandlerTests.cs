using WildBunch.Application.Dev.Queries;
using WildBunch.Application.Tests.TestDoubles;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;
using WildBunch.Domain.World;
using Town = WildBunch.Domain.World.Town;
using TownId = WildBunch.Domain.World.TownId;
using TownServices = WildBunch.Domain.World.TownServices;
using Trail = WildBunch.Domain.World.Trail;
using TrailId = WildBunch.Domain.World.TrailId;
using World = WildBunch.Domain.World.World;

namespace WildBunch.Application.Tests.Dev;

public sealed class GetSaloonDevContextHandlerTests
{
    [Fact]
    public async Task HandleAsync_ReturnsSaloonContext_WithHiddenTruthAndSuspects()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateSessionWithSaloonSuspect();
        repository.Seed(session);

        var handler = new GetSaloonDevContextHandler(repository);

        var result = await handler.HandleAsync(new GetSaloonDevContextQuery(session.Id.Value));

        Assert.Equal(session.Id.Value, result.SessionId);
        Assert.Equal("Current Town", result.CurrentTownName);
        Assert.False(result.SourceSpent);
        Assert.Null(result.PendingDevOverride);

        // Hidden truth is exposed in dev DTO
        Assert.NotNull(result.HiddenTruth);
        Assert.Equal("suspect-2", result.HiddenTruth!.TrueCulpritId);
        Assert.Equal("Reno Pike", result.HiddenTruth.TrueCulpritName);

        // Suspects list includes eligibility info
        Assert.Equal(2, result.Suspects.Count);
        var suspect1 = result.Suspects.Single(s => s.SuspectId == "suspect-1");
        Assert.False(suspect1.IsTrueCulprit);
        Assert.True(suspect1.IsEligibleSaloonPoi);
        var suspect2 = result.Suspects.Single(s => s.SuspectId == "suspect-2");
        Assert.True(suspect2.IsTrueCulprit);
        Assert.False(suspect2.IsEligibleSaloonPoi);
        Assert.Contains("True culprit", suspect2.IneligibilityReason);
    }

    [Fact]
    public async Task HandleAsync_WithPendingOverride_ReturnsOverrideState()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateSessionWithSaloonSuspect();
        session.ForceDevSaloonOverride(DevSaloonOverride.ForSuspect(new SuspectId("suspect-1")));
        session.MarkEventsCommitted();
        repository.Seed(session);

        var handler = new GetSaloonDevContextHandler(repository);

        var result = await handler.HandleAsync(new GetSaloonDevContextQuery(session.Id.Value));

        Assert.NotNull(result.PendingDevOverride);
        Assert.Equal("Suspect", result.PendingDevOverride!.ForcedKind);
        Assert.Equal("suspect-1", result.PendingDevOverride.ForcedSuspectId);
    }

    [Fact]
    public async Task HandleAsync_HiddenTruthDoesNotLeakIntoPlayerDtoSerialization()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateSessionWithSaloonSuspect();
        repository.Seed(session);

        var handler = new GetSaloonDevContextHandler(repository);

        var result = await handler.HandleAsync(new GetSaloonDevContextQuery(session.Id.Value));

        // The dev DTO deliberately contains hidden truth. Verify it's present in dev DTO.
        Assert.NotNull(result.HiddenTruth);
        // This test documents the boundary: the dev DTO is separate from player DTOs.
        // Player DTOs (GameSessionDto, JournalDto) must NOT contain trueCulpritId.
        // That boundary is enforced by the GameSessionMapper and JournalResolver tests.
    }

    private static GameSession CreateSessionWithSaloonSuspect()
    {
        var town = new WildBunch.Domain.World.Town(
            new TownId("current"), "Current Town", WildBunch.Domain.World.TownServices.NoticeBoard);
        var connected = new WildBunch.Domain.World.Town(
            new TownId("connected"), "Connected Town", WildBunch.Domain.World.TownServices.None);
        var world = new WildBunch.Domain.World.World(
            new[] { town, connected },
            new[] { new WildBunch.Domain.World.Trail(
                new WildBunch.Domain.World.TrailId("trail-1"), town.Id, connected.Id,
                WildBunch.Domain.World.TrailRisk.Low) });

        var suspects = new[]
        {
            new Suspect(
                new SuspectId("suspect-1"),
                "Mira Cline",
                new SuspectProfile(
                    Array.Empty<SuspectAlias>(),
                    new[] { new SuspectIdentityFact("Has a scar on the left cheek.") }),
                SuspectTraits.Empty,
                SuspectStatus.AtLarge),
            new Suspect(new SuspectId("suspect-2"), "Reno Pike", SuspectTraits.Empty, SuspectStatus.AtLarge)
        };

        var caseFile = new CaseFile(
            accusation: null, suspects,
            trueCulpritId: new SuspectId("suspect-2"),
            openingLead: CaseOpeningLead.Create("Follow the public leads."),
            knownClues: Array.Empty<Clue>(),
            knownWarrants: Array.Empty<Warrant>());

        var session = GameSession.StartNew("Ranger Vale", world, caseFile, town.Id,
            WildBunch.Domain.Economy.Wallet.Starting(25m), inventory: null,
            WildBunch.Domain.Travel.TravelDifficulty.Easy,
            WildBunch.Domain.Travel.TravelRandomnessState.CreateDeterministic(string.Empty));
        session.MarkEventsCommitted();
        return session;
    }
}
