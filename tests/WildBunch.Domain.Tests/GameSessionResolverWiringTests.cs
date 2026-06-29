using WildBunch.Domain.Cases;
using WildBunch.Domain.Economy;
using WildBunch.Domain.Game;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using DomainWorld = WildBunch.Domain.World.World;
using DomainInventory = WildBunch.Domain.Inventory.Inventory;
using Town = WildBunch.Domain.World.Town;
using TownServices = WildBunch.Domain.World.TownServices;
using Trail = WildBunch.Domain.World.Trail;
using TrailId = WildBunch.Domain.World.TrailId;

namespace WildBunch.Domain.Tests;

/// <summary>
/// Tests that GameSession's investigation methods use the WantedPosterResolver and
/// ClueSurfacingResolver for town/visit-aware selection instead of ordered peek.
/// BUNCH-107.
/// </summary>
public sealed class GameSessionResolverWiringTests
{
    [Fact]
    public void ReadWantedPosters_SurfacesDifferentWarrantsInDifferentTowns()
    {
        var session = CreateSessionWithMultipleWarrants();

        // Read posters in the starting town (slot 0, visit 1).
        var firstResult = session.ReadWantedPosters();
        Assert.True(firstResult.Success);
        var firstWarrant = Assert.Single(session.CaseFile.KnownWarrants);

        // Travel to a different town (slot 1) and read posters there.
        var secondTown = session.World.Towns.First(t => !t.Id.Equals(session.CurrentTown.TownId));
        session.CurrentTown.EnterTown(secondTown);
        session.ResetActionContextForTownChange();

        var secondResult = session.ReadWantedPosters();
        Assert.True(secondResult.Success);
        Assert.Equal(2, session.CaseFile.KnownWarrants.Count);

        var secondWarrant = session.CaseFile.KnownWarrants.Last();
        Assert.NotEqual(firstWarrant.Id, secondWarrant.Id);
    }

    [Fact]
    public void ReadWantedPosters_FreshSessionsInDifferentStartingTownsSurfaceDifferentFirstWarrants()
    {
        // Two fresh sessions with the same warrant pool but different starting towns
        // should surface different first warrants. With the old ordered-peek, both
        // would surface the first warrant in the pool. With the resolver, the town
        // slot index varies the selection.
        var (sessionA, sessionB) = CreateTwoSessionsInDifferentTowns();

        sessionA.ReadWantedPosters();
        sessionB.ReadWantedPosters();

        var warrantA = Assert.Single(sessionA.CaseFile.KnownWarrants);
        var warrantB = Assert.Single(sessionB.CaseFile.KnownWarrants);
        Assert.NotEqual(warrantA.Id, warrantB.Id);
    }

    [Fact]
    public void ReadWantedPosters_ExcludesTrueCulpritWarrantUntilKillerReleased()
    {
        var session = CreateSessionWithCulpritAndUnrelatedWarrants();

        // The culprit warrant is gated behind the killer release gate.
        // Reading posters should surface the unrelated warrant, not the culprit.
        var result = session.ReadWantedPosters();
        Assert.True(result.Success);
        var warrant = Assert.Single(session.CaseFile.KnownWarrants);
        Assert.Equal(InvestigationTargetKind.UnrelatedWantedCriminal, warrant.Terms.TargetKind);
    }

    [Fact]
    public void GatherLocalGossip_SkipsColorOnlyClueAndReturnsNothingNew()
    {
        var session = CreateSessionWithColorOnlyGossipClue();

        var result = session.GatherLocalGossip();

        Assert.True(result.Success);
        Assert.Equal("You ask around for local gossip, but hear nothing new.", result.Message);
        Assert.Empty(session.CaseFile.KnownClues);
    }

    /// <summary>
    /// Creates two fresh sessions with identical warrant pools but different starting
    /// towns (slot 0 vs slot 1). With the resolver, the town slot index varies which
    /// warrant surfaces first. With the old ordered-peek, both would surface the same
    /// first warrant.
    /// </summary>
    private static (GameSession SessionA, GameSession SessionB) CreateTwoSessionsInDifferentTowns()
    {
        var townA = new Town(new TownId("town-a"), "Town A", TownServices.None);
        var townB = new Town(new TownId("town-b"), "Town B", TownServices.None);
        var townC = new Town(new TownId("town-c"), "Town C", TownServices.None);
        var world = new DomainWorld(
            new[] { townA, townB, townC },
            new[]
            {
                new Trail(new TrailId("trail-ab"), townA.Id, townB.Id, TrailRisk.Low),
                new Trail(new TrailId("trail-bc"), townB.Id, townC.Id, TrailRisk.Low)
            });

        var suspects = new[]
        {
            new Suspect(new SuspectId("suspect-1"), "Ira Flint", SuspectTraits.Empty, SuspectStatus.AtLarge),
            new Suspect(new SuspectId("suspect-2"), "Mira Cline", SuspectTraits.Empty, SuspectStatus.AtLarge)
        };

        var publicWarrants = new[]
        {
            CreateUnrelatedWarrant("warrant-unrelated-1", "Reno Pike"),
            CreateUnrelatedWarrant("warrant-unrelated-2", "Cole Harmon"),
            CreateUnrelatedWarrant("warrant-unrelated-3", "Dusty McCabe")
        };

        GameSession CreateInTown(TownId startingTownId)
        {
            var caseFile = new CaseFile(
                accusation: null,
                suspects,
                trueCulpritId: new SuspectId("suspect-2"),
                openingLead: CaseOpeningLead.Create("A pale scar cuts across the left cheek."),
                knownClues: Array.Empty<Clue>(),
                publicClues: Array.Empty<Clue>(),
                publicWarrants: publicWarrants);

            return GameSession.StartNew(
                "Ranger Vale",
                world,
                caseFile,
                startingTownId,
                Wallet.Starting(25m),
                null,
                GameDifficulty.Easy,
                SaltSource.CreateFixed("test-salt"));
        }

        return (CreateInTown(townA.Id), CreateInTown(townB.Id));
    }

    private static GameSession CreateSessionWithMultipleWarrants()
    {
        var townA = new Town(new TownId("town-a"), "Town A", TownServices.None);
        var townB = new Town(new TownId("town-b"), "Town B", TownServices.None);
        var townC = new Town(new TownId("town-c"), "Town C", TownServices.None);
        var world = new DomainWorld(
            new[] { townA, townB, townC },
            new[]
            {
                new Trail(new TrailId("trail-ab"), townA.Id, townB.Id, TrailRisk.Low),
                new Trail(new TrailId("trail-bc"), townB.Id, townC.Id, TrailRisk.Low)
            });

        var suspects = new[]
        {
            new Suspect(new SuspectId("suspect-1"), "Ira Flint", SuspectTraits.Empty, SuspectStatus.AtLarge),
            new Suspect(new SuspectId("suspect-2"), "Mira Cline", SuspectTraits.Empty, SuspectStatus.AtLarge)
        };

        var inventory = new DomainInventory(new[]
        {
            new InventoryItem(ItemKind.Food, 4),
            new InventoryItem(ItemKind.Canteen, 1, canteenState: CanteenState.Full(10)),
            new InventoryItem(ItemKind.Horse, 1, HorseTravelState.Healthy),
            new InventoryItem(ItemKind.Saddle, 1)
        });

        var publicWarrants = new[]
        {
            CreateUnrelatedWarrant("warrant-unrelated-1", "Reno Pike"),
            CreateUnrelatedWarrant("warrant-unrelated-2", "Cole Harmon"),
            CreateUnrelatedWarrant("warrant-unrelated-3", "Dusty McCabe")
        };

        var caseFile = new CaseFile(
            accusation: null,
            suspects,
            trueCulpritId: new SuspectId("suspect-2"),
            openingLead: CaseOpeningLead.Create("A pale scar cuts across the left cheek."),
            knownClues: Array.Empty<Clue>(),
            publicClues: Array.Empty<Clue>(),
            publicWarrants: publicWarrants);

        return GameSession.StartNew(
            "Ranger Vale",
            world,
            caseFile,
            townA.Id,
            Wallet.Starting(25m),
            inventory,
            GameDifficulty.Easy,
            SaltSource.CreateFixed("test-salt"));
    }

    private static GameSession CreateSessionWithCulpritAndUnrelatedWarrants()
    {
        var townA = new Town(new TownId("town-a"), "Town A", TownServices.None);
        var townB = new Town(new TownId("town-b"), "Town B", TownServices.None);
        var world = new DomainWorld(
            new[] { townA, townB },
            new[]
            {
                new Trail(new TrailId("trail-ab"), townA.Id, townB.Id, TrailRisk.Low)
            });

        var suspects = new[]
        {
            new Suspect(new SuspectId("suspect-1"), "Ira Flint", SuspectTraits.Empty, SuspectStatus.AtLarge),
            new Suspect(new SuspectId("suspect-2"), "Mira Cline", SuspectTraits.Empty, SuspectStatus.AtLarge)
        };

        var publicWarrants = new[]
        {
            new Warrant(
                new WarrantId("warrant-culprit"),
                "Mira Cline",
                new WarrantTerms(
                    WarrantDisposition.DeadOrAlive,
                    2500m,
                    new[] { "Red Wren" },
                    new[] { "Pale scar across the left cheek" },
                    "Dodge City Marshal",
                    InvestigationTargetKind.TrueCulprit,
                    [OutlawGangIds.WildBunch],
                    OutlawGangIds.WildBunch,
                    InvestigationSourceKind.SheriffWarrants),
                "Wanted for a Wild Bunch robbery."),
            CreateUnrelatedWarrant("warrant-unrelated-1", "Reno Pike")
        };

        var caseFile = new CaseFile(
            accusation: null,
            suspects,
            trueCulpritId: new SuspectId("suspect-2"),
            openingLead: CaseOpeningLead.Create("A pale scar cuts across the left cheek."),
            knownClues: Array.Empty<Clue>(),
            publicClues: Array.Empty<Clue>(),
            publicWarrants: publicWarrants);

        return GameSession.StartNew(
            "Ranger Vale",
            world,
            caseFile,
            townA.Id,
            Wallet.Starting(25m),
            null,
            GameDifficulty.Easy,
            SaltSource.CreateFixed("test-salt"));
    }

    private static GameSession CreateSessionWithColorOnlyGossipClue()
    {
        var townA = new Town(new TownId("town-a"), "Town A", TownServices.None);
        var townB = new Town(new TownId("town-b"), "Town B", TownServices.None);
        var world = new DomainWorld(
            new[] { townA, townB },
            new[]
            {
                new Trail(new TrailId("trail-ab"), townA.Id, townB.Id, TrailRisk.Low)
            });

        var suspects = new[]
        {
            new Suspect(new SuspectId("suspect-1"), "Ira Flint", SuspectTraits.Empty, SuspectStatus.AtLarge),
            new Suspect(new SuspectId("suspect-2"), "Mira Cline", SuspectTraits.Empty, SuspectStatus.AtLarge)
        };

        var caseFile = new CaseFile(
            accusation: null,
            suspects,
            trueCulpritId: new SuspectId("suspect-2"),
            openingLead: CaseOpeningLead.Create("A pale scar cuts across the left cheek."),
            knownClues: Array.Empty<Clue>(),
            publicClues: new[]
            {
                new Clue(
                    new ClueId("clue-color-only"),
                    ClueKind.Whereabouts,
                    "A rider turned north at dusk.",
                    Array.Empty<SuspectId>(),
                    InvestigationTargetKind.GangMember,
                    InvestigationSourceKind.LocalGossip,
                    source: "saloon talk",
                    context: "Town gossip",
                    anchors: new ClueAnchors(
                        locations: new[]
                        {
                            new ClueLocationAnchor("North road", Place: "North road", Route: "North road")
                        },
                        times: new[]
                        {
                            new ClueTimeAnchor(ClueRecency.Yesterday, Day: 2)
                        },
                        directions: new[]
                        {
                            new ClueDirectionAnchor("north", Movement: "turned north", Route: "North road")
                        }))
            });

        return GameSession.StartNew(
            "Ranger Vale",
            world,
            caseFile,
            townA.Id,
            Wallet.Starting(25m),
            null,
            GameDifficulty.Easy,
            SaltSource.CreateFixed("test-salt"));
    }

    private static Warrant CreateUnrelatedWarrant(string id, string targetName)
        => new(
            new WarrantId(id),
            targetName,
            new WarrantTerms(
                WarrantDisposition.AliveOnly,
                300m,
                new[] { "The Magpie" },
                new[] { "Mismatched spurs" },
                "Silver Creek Sheriff",
                InvestigationTargetKind.UnrelatedWantedCriminal,
                Array.Empty<OutlawGangId>(),
                null,
                InvestigationSourceKind.SheriffWarrants),
            "Wanted for cattle theft.");
}
