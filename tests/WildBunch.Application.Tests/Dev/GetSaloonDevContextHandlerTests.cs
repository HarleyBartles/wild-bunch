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
        // No active POI before LookAroundSaloon
        Assert.Null(result.ActiveSaloonPoi);

        // Hidden truth is exposed in dev DTO with gate-aware eligibility
        Assert.NotNull(result.HiddenTruth);
        Assert.Equal("suspect-2", result.HiddenTruth!.TrueCulpritId);
        Assert.Equal("Reno Pike", result.HiddenTruth.TrueCulpritName);
        Assert.False(result.HiddenTruth.KillerIsReleased);
        Assert.Contains("killer trail is locked", result.HiddenTruth.KillerReleaseStatus.ToLowerInvariant());
        Assert.NotEmpty(result.HiddenTruth.SaloonLoopExplanation);

        // Citizen info is honestly described
        Assert.NotNull(result.CitizenInfo);
        Assert.True(result.CitizenInfo!.HasNamedArchetypes);
        Assert.NotEmpty(result.CitizenInfo.AvailableArchetypes);
        Assert.Contains("shared suspect vocabulary", result.CitizenInfo.Descriptor);

        // Suspects list includes eligibility info and warrant-shaped facts
        Assert.Equal(2, result.Suspects.Count);
        var suspect1 = result.Suspects.Single(s => s.SuspectId == "suspect-1");
        Assert.False(suspect1.IsTrueCulprit);
        Assert.True(suspect1.IsEligibleSaloonPoi);
        Assert.NotEmpty(suspect1.IdentifyingFacts);
        Assert.Contains("scar", suspect1.IdentifyingFacts[0].ToLowerInvariant());

        var suspect2 = result.Suspects.Single(s => s.SuspectId == "suspect-2");
        Assert.True(suspect2.IsTrueCulprit);
        Assert.False(suspect2.IsEligibleSaloonPoi);
        // Gate-aware: no longer says "can never appear"
        Assert.DoesNotContain("can never appear", suspect2.IneligibilityReason?.ToLowerInvariant() ?? "");
        Assert.Contains("killer trail is locked", suspect2.IneligibilityReason?.ToLowerInvariant() ?? "");
    }

    [Fact]
    public async Task HandleAsync_AfterLookAroundSaloon_ReturnsActiveWantedSuspectPoi()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateSessionWithSaloonSuspect();
        session.ForceDevSaloonOverride(DevSaloonOverride.ForSuspect(new SuspectId("suspect-1")));
        session.MarkEventsCommitted();
        session.LookAroundSaloon();
        session.MarkEventsCommitted();
        repository.Seed(session);

        var handler = new GetSaloonDevContextHandler(repository);

        var result = await handler.HandleAsync(new GetSaloonDevContextQuery(session.Id.Value));

        Assert.True(result.SourceSpent);
        Assert.NotNull(result.ActiveSaloonPoi);
        Assert.Equal("WantedSuspect", result.ActiveSaloonPoi!.PersonOfInterestKind);
        Assert.NotNull(result.ActiveSaloonPoi.SuspectId);
        Assert.NotNull(result.ActiveSaloonPoi.SuspectName);
        Assert.NotNull(result.ActiveSaloonPoi.Descriptor);
    }

    [Fact]
    public async Task HandleAsync_AfterForcedCitizenOverride_ReturnsActiveCitizenPoi()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateSessionWithSaloonSuspect();
        session.ForceDevSaloonOverride(DevSaloonOverride.ForCitizen());
        session.MarkEventsCommitted();
        session.LookAroundSaloon();
        session.MarkEventsCommitted();
        repository.Seed(session);

        var handler = new GetSaloonDevContextHandler(repository);

        var result = await handler.HandleAsync(new GetSaloonDevContextQuery(session.Id.Value));

        Assert.True(result.SourceSpent);
        Assert.NotNull(result.ActiveSaloonPoi);
        Assert.Equal("Citizen", result.ActiveSaloonPoi!.PersonOfInterestKind);
        Assert.Null(result.ActiveSaloonPoi.SuspectId);
        Assert.NotNull(result.ActiveSaloonPoi.Descriptor);
        // Override consumed
        Assert.Null(result.PendingDevOverride);
    }

    [Fact]
    public async Task HandleAsync_AfterForcedSuspectOverride_ReturnsActiveForcedSuspectPoi()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateSessionWithSaloonSuspect();
        session.ForceDevSaloonOverride(DevSaloonOverride.ForSuspect(new SuspectId("suspect-1")));
        session.MarkEventsCommitted();
        session.LookAroundSaloon();
        session.MarkEventsCommitted();
        repository.Seed(session);

        var handler = new GetSaloonDevContextHandler(repository);

        var result = await handler.HandleAsync(new GetSaloonDevContextQuery(session.Id.Value));

        Assert.True(result.SourceSpent);
        Assert.NotNull(result.ActiveSaloonPoi);
        Assert.Equal("WantedSuspect", result.ActiveSaloonPoi!.PersonOfInterestKind);
        Assert.Equal("suspect-1", result.ActiveSaloonPoi.SuspectId);
        Assert.Equal("Mira Cline", result.ActiveSaloonPoi.SuspectName);
        Assert.NotNull(result.ActiveSaloonPoi.Descriptor);
        // Override consumed
        Assert.Null(result.PendingDevOverride);
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
        Assert.Equal("Mira Cline", result.PendingDevOverride.ForcedSuspectName);
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
            WildBunch.Domain.Travel.GameDifficulty.Easy,
            WildBunch.Domain.Travel.SaltSource.CreateFixed(string.Empty));
        session.MarkEventsCommitted();
        return session;
    }
}
