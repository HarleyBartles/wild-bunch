using WildBunch.Application.Projections;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using WildBunch.Integration.Tests.TestInfrastructure;
using WildBunch.Persistence.GameSessions;
using WildBunch.Persistence.Serialization;
using WildBunch.Persistence.Versioning;
using DomainWorld = WildBunch.Domain.World.World;
using Town = WildBunch.Domain.World.Town;
using Trail = WildBunch.Domain.World.Trail;
using TrailId = WildBunch.Domain.World.TrailId;

namespace WildBunch.Integration.Tests;

/// <summary>
/// Tests that the UnrelatedCriminalLedger survives JSON snapshot serialization
/// and PostgreSQL round-trips. The ledger must be persisted as a component so
/// that active/taken-in/collected/retired sets, gang parity, and next spawn
/// index survive reload — not reconstructed from a shrinking PublicWarrants
/// pool. See BUNCH-107.
/// </summary>
public sealed class UnrelatedCriminalLedgerPersistenceTests
{
    [Fact]
    public void JsonSnapshotRoundTrip_PreservesLedgerState()
    {
        var session = CreateSessionWithFullLedger(gangSuspectCount: 2, unrelatedWarrantCount: 6);

        // Mutate the ledger: record a gang take-in to despawn, mark a warrant collected.
        session.EnterActionContext(TownActionContext.Saloon);
        session.CurrentTownVisit.CurrentTownState.SetActiveSaloonWantedSuspect(new SuspectId("suspect-1"));
        session.ResolveWantedSuspectConfrontation(new SuspectId("suspect-1"), WantedSuspectConfrontationChoice.Surrendered);
        session.SettleSheriffTurnIn(new SuspectId("suspect-1"), isAlive: true);
        session.UnrelatedCriminalLedger.MarkWarrantCollected(new WarrantId("warrant-unrelated-2"));

        var gangMembersAvailableBefore = session.UnrelatedCriminalLedger.GangMembersAvailable;
        var activeCountBefore = session.UnrelatedCriminalLedger.ActiveCriminalCount;
        var retiredIdsBefore = session.UnrelatedCriminalLedger.RetiredWarrantIds.ToArray();
        var collectedIdsBefore = session.UnrelatedCriminalLedger.WarrantCollectedIds.ToArray();

        // Serialize and deserialize through the JSON snapshot path.
        var serializer = new GameSessionJsonSerializer();
        var json = serializer.Serialize(session);
        var restored = serializer.Deserialize(json);

        // The ledger state must be fully preserved.
        Assert.Equal(gangMembersAvailableBefore, restored.UnrelatedCriminalLedger.GangMembersAvailable);
        Assert.Equal(activeCountBefore, restored.UnrelatedCriminalLedger.ActiveCriminalCount);
        Assert.Equal(retiredIdsBefore, restored.UnrelatedCriminalLedger.RetiredWarrantIds);
        Assert.Equal(collectedIdsBefore, restored.UnrelatedCriminalLedger.WarrantCollectedIds);
    }

    [Fact]
    public async Task PostgreSqlRoundTrip_PreservesLedgerState()
    {
        using var fixture = new PostgreSqlPersistenceFixture();
        var context = fixture.CreateContext();
        var unitOfWork = new EfGameSessionUnitOfWork(context);
        var serializer = new GameSessionJsonSerializer();
        var payloadLoader = new PersistedPayloadLoader(
            new PayloadUpcasterRegistry([]),
            serializer,
            new TravelDiaryDayProjector(),
            rebuildSessionFromEvents: events => SessionRebuilder.RebuildFromEvents(events, serializer));
        var repository = new EfGameSessionRepository(context, serializer, new TravelDiaryDayProjector(), new PayloadUpcasterRegistry([]), payloadLoader);

        var session = CreateSessionWithFullLedger(gangSuspectCount: 2, unrelatedWarrantCount: 6);

        // Mutate the ledger: record a gang take-in to despawn, mark a warrant collected.
        session.EnterActionContext(TownActionContext.Saloon);
        session.CurrentTownVisit.CurrentTownState.SetActiveSaloonWantedSuspect(new SuspectId("suspect-1"));
        session.ResolveWantedSuspectConfrontation(new SuspectId("suspect-1"), WantedSuspectConfrontationChoice.Surrendered);
        session.SettleSheriffTurnIn(new SuspectId("suspect-1"), isAlive: true);
        session.UnrelatedCriminalLedger.MarkWarrantCollected(new WarrantId("warrant-unrelated-2"));
        session.MarkEventsCommitted();

        var gangMembersAvailableBefore = session.UnrelatedCriminalLedger.GangMembersAvailable;
        var activeCountBefore = session.UnrelatedCriminalLedger.ActiveCriminalCount;
        var retiredIdsBefore = session.UnrelatedCriminalLedger.RetiredWarrantIds.ToArray();
        var collectedIdsBefore = session.UnrelatedCriminalLedger.WarrantCollectedIds.ToArray();

        await repository.StoreAsync(session);
        await unitOfWork.CommitAsync();

        var reloaded = await repository.GetByIdAsync(session.Id);
        Assert.NotNull(reloaded);

        Assert.Equal(gangMembersAvailableBefore, reloaded!.UnrelatedCriminalLedger.GangMembersAvailable);
        Assert.Equal(activeCountBefore, reloaded.UnrelatedCriminalLedger.ActiveCriminalCount);
        Assert.Equal(retiredIdsBefore, reloaded.UnrelatedCriminalLedger.RetiredWarrantIds);
        Assert.Equal(collectedIdsBefore, reloaded.UnrelatedCriminalLedger.WarrantCollectedIds);
    }

    private static GameSession CreateSessionWithFullLedger(int gangSuspectCount, int unrelatedWarrantCount)
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.None);
        var world = new DomainWorld(
            new[] { pinecross },
            new[] { new Trail(new TrailId("trail-1"), pinecross.Id, pinecross.Id, TrailRisk.Low) });

        var suspects = Enumerable.Range(1, gangSuspectCount)
            .Select(i => new Suspect(
                new SuspectId($"suspect-{i}"),
                $"Gang Member {i}",
                SuspectTraits.Empty,
                SuspectStatus.AtLarge))
            .ToArray();

        var knownWarrants = Enumerable.Range(1, gangSuspectCount)
            .Select(i => new Warrant(
                new WarrantId($"warrant-gang-{i}"),
                $"Gang Member {i}",
                new WarrantTerms(
                    WarrantDisposition.DeadOrAlive,
                    500m,
                    new[] { $"Alias {i}" },
                    new[] { $"Feature {i}" },
                    "Dodge City Marshal",
                    i == 1 ? InvestigationTargetKind.TrueCulprit : InvestigationTargetKind.GangMember,
                    Array.Empty<OutlawGangId>(),
                    null),
                "Wanted for gang crimes."))
            .ToArray();

        var publicWarrants = Enumerable.Range(1, unrelatedWarrantCount)
            .Select(i => new Warrant(
                new WarrantId($"warrant-unrelated-{i}"),
                $"Outlaw {i}",
                new WarrantTerms(
                    WarrantDisposition.AliveOnly,
                    100m,
                    new[] { $"Outlaw Alias {i}" },
                    new[] { $"Outlaw Feature {i}" },
                    $"Sheriff {i}",
                    InvestigationTargetKind.UnrelatedWantedCriminal,
                    Array.Empty<OutlawGangId>(),
                    null),
                "Wanted for unrelated territorial offenses."))
            .ToArray();

        var caseFile = new CaseFile(
            accusation: null,
            suspects,
            trueCulpritId: new SuspectId("suspect-1"),
            openingLead: CaseOpeningLead.Create("Follow the public leads and look for a signature mark."),
            knownClues: Array.Empty<Clue>(),
            knownWarrants: knownWarrants,
            publicWarrants: publicWarrants);

        var session = GameSession.StartSetup(
            "Ranger Vale", world, caseFile,
            GameDifficulty.Standard, GameEntropy.Classic, "test-seed", SaltSource.CreateFixed("test"));
        session.ViewPrologue("test-prologue-descriptor");
        session.SelectStartingTown(pinecross.Id);
        session.CompleteGameStart();
        return session;
    }
}
