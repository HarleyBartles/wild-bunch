using System.Text.Json;
using WildBunch.Application.Games.Commands;
using WildBunch.Application.Tests.TestDoubles;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using DomainWorld = WildBunch.Domain.World.World;
using Town = WildBunch.Domain.World.Town;
using Trail = WildBunch.Domain.World.Trail;
using TrailId = WildBunch.Domain.World.TrailId;

namespace WildBunch.Application.Tests;

public sealed class ConfrontSaloonWantedSuspectHandlerTests
{
    [Fact]
    public async Task HandleAsyncFleesTheSurfacedSaloonSuspectAndPersistsTheState()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateSession();
        var suspectId = new SuspectId("suspect-1");
        session.SetWantedSuspectPresenceState(suspectId, WantedSuspectPresenceState.AvailableInTown);
        session.ForceDevSaloonOverride(DevSaloonOverride.ForSuspect(suspectId));
        session.MarkEventsCommitted();
        session.LookAroundSaloon();
        repository.Seed(session);
        var handler = new ConfrontSaloonWantedSuspectHandler(repository, repository);

        var result = await handler.HandleAsync(new ConfrontSaloonWantedSuspectCommand(session.Id.Value));

        Assert.True(result.Success);
        Assert.Equal(WantedSuspectConfrontationOutcome.Fled, result.Outcome);
        Assert.Equal("Mira Cline", result.TargetName);
        Assert.True(result.IsAlive);
        Assert.False(result.IsSecured);
        Assert.True(result.SessionChanged);
        Assert.Equal(1, repository.StoreCalls);
        Assert.Equal(1, repository.CommitCalls);
        Assert.Equal(WantedSuspectPresenceState.GoneToGround, repository.Sessions.Single().GetWantedSuspectPresenceState(suspectId));
        Assert.Null(repository.Sessions.Single().CurrentTownVisit.CurrentTownState.ActiveSaloonWantedSuspectId);
        Assert.True(repository.Sessions.Single().CaseFile.TryGetWantedSuspectConfrontationState(suspectId, out var state));
        Assert.Equal(WantedSuspectConfrontationOutcome.Fled, state.Outcome);

        var payload = JsonSerializer.Serialize(result);
        Assert.DoesNotContain("\"trueCulpritId\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"killerReleaseState\"", payload, StringComparison.OrdinalIgnoreCase);
    }

    private static GameSession CreateSession()
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.None);
        var connected = new Town(new TownId("connected"), "Connected", TownServices.None);
        var world = new DomainWorld(
            new[] { pinecross, connected },
            new[] { new Trail(new TrailId("trail-1"), pinecross.Id, connected.Id, TrailRisk.Low) });

        var suspects = new[]
        {
            new Suspect(new SuspectId("suspect-1"), "Mira Cline", SuspectTraits.Empty, SuspectStatus.AtLarge),
            new Suspect(new SuspectId("suspect-2"), "Reno Pike", SuspectTraits.Empty, SuspectStatus.AtLarge)
        };

        var caseFile = new CaseFile(
            accusation: null,
            suspects,
            trueCulpritId: new SuspectId("suspect-2"),
            openingLead: CaseOpeningLead.Create("Follow the public leads and look for a signature mark."),
            knownClues: Array.Empty<Clue>(),
            knownWarrants: new[]
            {
                new Warrant(
                    new WarrantId("warrant-1"),
                    "Mira Cline",
                    new WarrantTerms(
                        WarrantDisposition.DeadOrAlive,
                        2500m,
                        new[] { "Red Wren" },
                        new[] { "Raven-feather pin" },
                        "Dodge City Marshal",
                        InvestigationTargetKind.TrueCulprit,
                        Array.Empty<OutlawGangId>(),
                        null),
                    "Wanted for a stage robbery.")
            });

        var session = GameSession.StartSetup("Ranger Vale", world, caseFile, GameDifficulty.Standard, GameEntropy.Classic, "test-seed", SaltSource.CreateFixed("test"));
        session.ViewPrologue("test-prologue-descriptor");
        session.SelectStartingTown(pinecross.Id);
        session.CompleteGameStart();
        return session;
    }
}
