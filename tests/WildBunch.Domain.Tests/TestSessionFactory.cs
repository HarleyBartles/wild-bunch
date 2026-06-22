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
/// Factory for creating <see cref="GameSession"/> instances with specific investigation
/// scenarios. Used by <see cref="InvestigationEventSourcingTests"/> to set up sessions with
/// public clues/warrants for testing the event-sourced investigation pattern.
/// </summary>
public static class TestSessionFactory
{
    /// <summary>
    /// Creates a session with a single public clue matching the given source kind.
    /// The clue is in <see cref="CaseFile.PublicClues"/> and NOT in <see cref="CaseFile.KnownClues"/>.
    /// The town supports NoticeBoard, Telegraph, and Lodging services.
    /// </summary>
    public static GameSession CreateWithPublicClue(InvestigationSourceKind sourceKind, string description)
    {
        var town = new Town(new TownId("current"), "Current Town",
            TownServices.NoticeBoard | TownServices.Telegraph | TownServices.Lodging);
        var connected = new Town(new TownId("connected"), "Connected Town", TownServices.None);
        var world = new DomainWorld(
            new[] { town, connected },
            new[] { new Trail(new TrailId("trail-1"), town.Id, connected.Id, TrailRisk.Low) });

        var suspects = new[]
        {
            new Suspect(new SuspectId("suspect-1"), "Ira Flint",
                SuspectTraits.FromTags(SuspectTraitTags.Local, SuspectTraitTags.Desperate), SuspectStatus.AtLarge),
            new Suspect(new SuspectId("suspect-2"), "Mira Cline",
                SuspectTraits.Empty, SuspectStatus.AtLarge)
        };

        var clue = new Clue(
            new ClueId("clue-public-1"),
            ClueKind.Alias,
            description,
            new[] { new SuspectId("suspect-1") },
            InvestigationTargetKind.Suspected,
            sourceKind,
            source: "test source",
            context: "test context");

        var caseFile = new CaseFile(
            accusation: null,
            suspects,
            trueCulpritId: new SuspectId("suspect-2"),
            openingLead: CaseOpeningLead.Create("A pale scar cuts across the left cheek."),
            knownClues: Array.Empty<Clue>(),
            publicClues: new[] { clue });

        var inventory = new DomainInventory(new[]
        {
            new InventoryItem(ItemKind.Food, 4),
            new InventoryItem(ItemKind.Canteen, 1, canteenState: CanteenState.Full(10)),
            new InventoryItem(ItemKind.Horse, 1, HorseTravelState.Healthy),
            new InventoryItem(ItemKind.Saddle, 1)
        });

        var session = GameSession.StartNew("Ranger Vale", world, caseFile, town.Id,
            Wallet.Starting(25m), inventory, TravelDifficulty.Easy,
            TravelRandomnessState.CreateDeterministic(string.Empty));
        session.MarkEventsCommitted();
        return session;
    }

    /// <summary>
    /// Creates a session where the given source kind is already spent for this visit
    /// but no clue has been revealed. The source is marked spent via the town aggregate's
    /// mutating <see cref="TownAggregate.CheckSource"/> (or <see cref="TownAggregate.CheckWantedPosters"/>
    /// for <see cref="InvestigationSourceKind.SheriffWarrants"/>) without going through an
    /// investigation method, so no event is produced and no clue is revealed.
    /// </summary>
    public static GameSession CreateWithSpentSource(InvestigationSourceKind sourceKind)
    {
        var session = CreateWithPublicClue(sourceKind, "A dusty boot print.");

        if (sourceKind == InvestigationSourceKind.SheriffWarrants)
        {
            session.CurrentTown.CheckWantedPosters();
        }
        else
        {
            session.CurrentTown.CheckSource(sourceKind);
        }

        return session;
    }

    /// <summary>
    /// Creates a session with a public warrant AND a public clue both tagged with
    /// <see cref="InvestigationSourceKind.SheriffWarrants"/>. Used for <see cref="GameSession.ReadWantedPosters"/>
    /// testing where both a warrant and a clue should be revealed in a single event.
    /// </summary>
    public static GameSession CreateWithPublicWarrantAndClue(InvestigationSourceKind sourceKind)
    {
        var town = new Town(new TownId("current"), "Current Town",
            TownServices.NoticeBoard | TownServices.Telegraph | TownServices.Lodging);
        var connected = new Town(new TownId("connected"), "Connected Town", TownServices.None);
        var world = new DomainWorld(
            new[] { town, connected },
            new[] { new Trail(new TrailId("trail-1"), town.Id, connected.Id, TrailRisk.Low) });

        var suspects = new[]
        {
            new Suspect(new SuspectId("suspect-1"), "Ira Flint",
                SuspectTraits.FromTags(SuspectTraitTags.Local, SuspectTraitTags.Desperate), SuspectStatus.AtLarge),
            new Suspect(new SuspectId("suspect-2"), "Mira Cline",
                SuspectTraits.Empty, SuspectStatus.AtLarge)
        };

        var clue = new Clue(
            new ClueId("clue-public-warrant-1"),
            ClueKind.Alias,
            "A poster links Grey Jay to a rider with a pale scar.",
            new[] { new SuspectId("suspect-1") },
            InvestigationTargetKind.Suspected,
            sourceKind,
            source: "wanted poster",
            context: "Public wanted poster");

        var warrant = new Warrant(
            new WarrantId("warrant-public-1"),
            "Mira Cline",
            new WarrantTerms(
                WarrantDisposition.DeadOrAlive,
                2500m,
                new[] { "Red Wren", "Aunt Tess" },
                new[] { "Pale scar across the left cheek" },
                "Dodge City Marshal",
                InvestigationTargetKind.TrueCulprit,
                [OutlawGangIds.WildBunch],
                OutlawGangIds.WildBunch,
                InvestigationSourceKind.SheriffWarrants),
            "Wanted for a Wild Bunch robbery.");

        var caseFile = new CaseFile(
            accusation: null,
            suspects,
            trueCulpritId: new SuspectId("suspect-2"),
            openingLead: CaseOpeningLead.Create("A pale scar cuts across the left cheek."),
            knownClues: Array.Empty<Clue>(),
            publicClues: new[] { clue },
            publicWarrants: new[] { warrant });

        var inventory = new DomainInventory(new[]
        {
            new InventoryItem(ItemKind.Food, 4),
            new InventoryItem(ItemKind.Canteen, 1, canteenState: CanteenState.Full(10)),
            new InventoryItem(ItemKind.Horse, 1, HorseTravelState.Healthy),
            new InventoryItem(ItemKind.Saddle, 1)
        });

        var session = GameSession.StartNew("Ranger Vale", world, caseFile, town.Id,
            Wallet.Starting(25m), inventory, TravelDifficulty.Easy,
            TravelRandomnessState.CreateDeterministic(string.Empty));
        session.MarkEventsCommitted();
        return session;
    }

    /// <summary>
    /// Creates a session with an active (in-progress) journey so that
    /// <see cref="GameSession.IsJourneyModal"/> returns true and investigation
    /// actions are blocked.
    /// </summary>
    public static GameSession CreateWithActiveJourney()
    {
        var session = CreateWithPublicClue(InvestigationSourceKind.LocalGossip, "A dusty boot print.");
        StartJourney(session);
        return session;
    }

    private static void StartJourney(GameSession session)
    {
        var travelResolver = new TravelResolver();
        var preview = travelResolver.PreviewJourney(
                session.World,
                session.Player.CurrentTownId,
                new TownId("connected"),
                session.Player.Inventory)
            .Preview!;

        session.StartJourney(preview);
    }
}
