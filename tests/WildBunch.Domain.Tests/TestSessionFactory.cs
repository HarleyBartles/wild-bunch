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
    /// Creates a default session in the starting town with no journey active and no
    /// investigation sources spent. Used by clock/turn correction and event-sourcing
    /// tests that need a clean baseline session with CurrentActionContext = None.
    /// </summary>
    public static GameSession CreateDefault()
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

        var caseFile = new CaseFile(
            accusation: null,
            suspects,
            trueCulpritId: new SuspectId("suspect-2"),
            openingLead: CaseOpeningLead.Create("A pale scar cuts across the left cheek."),
            knownClues: Array.Empty<Clue>(),
            publicClues: Array.Empty<Clue>());

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
    /// Creates a session with a confrontable saloon suspect (a suspect with an identifying
    /// fact but no known warrants). LookAroundSaloon will spot this suspect as a wanted-suspect
    /// person of interest. Used by BUNCH-80 bounty/saloon event-sourcing tests.
    /// </summary>
    public static GameSession CreateWithConfrontableSaloonSuspect()
    {
        var town = new Town(new TownId("current"), "Current Town", TownServices.NoticeBoard);
        var connected = new Town(new TownId("connected"), "Connected Town", TownServices.None);
        var world = new DomainWorld(
            new[] { town, connected },
            new[] { new Trail(new TrailId("trail-1"), town.Id, connected.Id, TrailRisk.Low) });

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
            accusation: null,
            suspects,
            trueCulpritId: new SuspectId("suspect-2"),
            openingLead: CaseOpeningLead.Create("Follow the public leads and look for a signature mark."),
            knownClues: Array.Empty<Clue>(),
            knownWarrants: Array.Empty<Warrant>());

        var session = GameSession.StartNew("Ranger Vale", world, caseFile, town.Id,
            Wallet.Starting(25m), inventory: null, TravelDifficulty.Easy,
            TravelRandomnessState.CreateDeterministic(string.Empty));
        session.MarkEventsCommitted();
        return session;
    }

    /// <summary>
    /// Creates a session with no confrontable saloon suspects (empty suspects list).
    /// LookAroundSaloon will spot a citizen person of interest instead.
    /// Used by BUNCH-80 bounty/saloon event-sourcing tests.
    /// </summary>
    public static GameSession CreateWithNoConfrontableSaloonSuspect()
    {
        var town = new Town(new TownId("current"), "Current Town", TownServices.NoticeBoard);
        var connected = new Town(new TownId("connected"), "Connected Town", TownServices.None);
        var world = new DomainWorld(
            new[] { town, connected },
            new[] { new Trail(new TrailId("trail-1"), town.Id, connected.Id, TrailRisk.Low) });

        var caseFile = new CaseFile(
            accusation: null,
            Array.Empty<Suspect>(),
            trueCulpritId: new SuspectId("suspect-2"),
            openingLead: CaseOpeningLead.Create("Follow the public leads and look for a signature mark."),
            knownClues: Array.Empty<Clue>(),
            knownWarrants: Array.Empty<Warrant>());

        var session = GameSession.StartNew("Ranger Vale", world, caseFile, town.Id,
            Wallet.Starting(25m), inventory: null, TravelDifficulty.Easy,
            TravelRandomnessState.CreateDeterministic(string.Empty));
        session.MarkEventsCommitted();
        return session;
    }

    /// <summary>
    /// Creates a session where the current town has no saloon source available.
    /// LookAroundSaloon will fail with "There is no saloon here."
    /// Used by BUNCH-80 bounty/saloon event-sourcing tests.
    /// </summary>
    public static GameSession CreateWithNoSaloon()
    {
        // SaloonLookAround is Baseline availability (always available). To make it unavailable,
        // we replace it with a Conditional definition requiring Telegraph service, then use a
        // town without Telegraph. This makes IsAvailable return false without removing the
        // source from the catalog (which would cause GetRequiredDefinition to throw).
        var noSaloonCatalog = new TownSourceCatalog(
            TownSourceCatalog.Default.Definitions
                .Select(d => d.Kind == InvestigationSourceKind.SaloonLookAround
                    ? d with { Availability = TownSourceAvailability.Conditional, RequiredServices = TownServices.Telegraph }
                    : d)
                .ToArray());
        var town = new Town(new TownId("current"), "Current Town", TownServices.None, noSaloonCatalog);
        var connected = new Town(new TownId("connected"), "Connected Town", TownServices.None);
        var world = new DomainWorld(
            new[] { town, connected },
            new[] { new Trail(new TrailId("trail-1"), town.Id, connected.Id, TrailRisk.Low) });

        var suspects = new[]
        {
            new Suspect(new SuspectId("suspect-1"), "Ira Flint",
                SuspectTraits.Empty, SuspectStatus.AtLarge)
        };

        var caseFile = new CaseFile(
            accusation: null,
            suspects,
            trueCulpritId: new SuspectId("suspect-1"),
            openingLead: CaseOpeningLead.Create("Follow the public leads."),
            knownClues: Array.Empty<Clue>(),
            knownWarrants: Array.Empty<Warrant>());

        var session = GameSession.StartNew("Ranger Vale", world, caseFile, town.Id,
            Wallet.Starting(25m), inventory: null, TravelDifficulty.Easy,
            TravelRandomnessState.CreateDeterministic(string.Empty));
        session.MarkEventsCommitted();
        return session;
    }

    /// <summary>
    /// Creates a session with a suspect that has a known warrant (DeadOrAlive, $2500 bounty).
    /// The suspect is at large and not yet confronted. Used by BUNCH-80 confrontation
    /// event-sourcing tests.
    /// </summary>
    public static GameSession CreateWithWarrantedSuspect()
    {
        var town = new Town(new TownId("pinecross"), "Pinecross", TownServices.NoticeBoard);
        var connected = new Town(new TownId("connected"), "Connected", TownServices.None);
        var world = new DomainWorld(
            new[] { town, connected },
            new[] { new Trail(new TrailId("trail-1"), town.Id, connected.Id, TrailRisk.Low) });

        var suspects = new[]
        {
            new Suspect(new SuspectId("suspect-1"), "Mira Cline", SuspectTraits.Empty, SuspectStatus.AtLarge),
            new Suspect(new SuspectId("suspect-2"), "Reno Pike", SuspectTraits.Empty, SuspectStatus.AtLarge)
        };

        var caseFile = new CaseFile(
            accusation: null,
            suspects,
            trueCulpritId: new SuspectId("suspect-1"),
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

        var session = GameSession.StartNew("Ranger Vale", world, caseFile, town.Id,
            Wallet.Starting(25m), inventory: null, TravelDifficulty.Easy,
            TravelRandomnessState.CreateDeterministic(string.Empty));
        session.MarkEventsCommitted();
        return session;
    }

    /// <summary>
    /// Creates a session with a warranted suspect that has been confronted and secured alive.
    /// The suspect is in Surrendered confrontation state, ready for sheriff turn-in.
    /// Used by BUNCH-80 sheriff turn-in event-sourcing tests.
    /// </summary>
    public static GameSession CreateWithSecuredSuspect()
    {
        var session = CreateWithWarrantedSuspect();
        session.EnterActionContext(TownActionContext.Saloon);
        // BUNCH-80: confrontation requires an active saloon POI matching the target
        session.CurrentTownVisit.CurrentTownState.SetActiveSaloonWantedSuspect(new SuspectId("suspect-1"));
        session.ResolveWantedSuspectConfrontation(
            new SuspectId("suspect-1"), WantedSuspectConfrontationChoice.Surrendered);
        session.MarkEventsCommitted();
        return session;
    }

    /// <summary>
    /// Creates a session with an active citizen saloon person of interest (no suspects,
    /// LookAroundSaloon called to set a citizen POI). Used by BUNCH-80 saloon confrontation
    /// event-sourcing tests.
    /// </summary>
    public static GameSession CreateWithActiveCitizenSaloonPerson()
    {
        var session = CreateWithNoConfrontableSaloonSuspect();
        session.LookAroundSaloon();
        session.MarkEventsCommitted();
        return session;
    }

    /// <summary>
    /// Creates a session with an active wanted-suspect saloon person of interest, a known
    /// warrant with identity handle "warrant-public-1", and a firearm threat available
    /// (Revolver + RevolverAmmo in inventory). The suspect is set as AvailableInTown and
    /// LookAroundSaloon is called to set the active saloon person. Declaring "warrant-public-1"
    /// triggers the armed+correct confrontation path. Used by BUNCH-80 saloon confrontation
    /// event-sourcing tests.
    /// </summary>
    public static GameSession CreateWithArmedCorrectDeclarationSetup()
    {
        var town = new Town(new TownId("current"), "Current Town", TownServices.NoticeBoard);
        var connected = new Town(new TownId("connected"), "Connected Town", TownServices.None);
        var world = new DomainWorld(
            new[] { town, connected },
            new[] { new Trail(new TrailId("trail-1"), town.Id, connected.Id, TrailRisk.Low) });

        var suspects = new[]
        {
            new Suspect(new SuspectId("suspect-1"), "Mira Cline", SuspectTraits.Empty, SuspectStatus.AtLarge),
            new Suspect(
                new SuspectId("suspect-2"),
                "Reno Pike",
                new SuspectProfile(
                    Array.Empty<SuspectAlias>(),
                    new[] { new SuspectIdentityFact("a black duster") }),
                SuspectTraits.Empty,
                SuspectStatus.AtLarge)
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
                    new WarrantId("warrant-public-1"),
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

        var inventory = new DomainInventory(
            new[]
            {
                new InventoryItem(ItemKind.Revolver, 1),
                new InventoryItem(ItemKind.RevolverAmmo, 2)
            });

        var session = GameSession.StartNew("Ranger Vale", world, caseFile, town.Id,
            Wallet.Starting(25m), inventory: inventory, TravelDifficulty.Easy,
            TravelRandomnessState.CreateDeterministic(string.Empty));
        session.SetWantedSuspectPresenceState(new SuspectId("suspect-1"), WantedSuspectPresenceState.AvailableInTown);
        session.LookAroundSaloon();
        session.MarkEventsCommitted();
        return session;
    }

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

    /// <summary>
    /// Creates a FRESH baseline <see cref="CaseFile"/> from the same template used to create
    /// the session, before any investigation mutations. The public clues that were in the
    /// session's original CaseFile are in <see cref="CaseFile.PublicClues"/> and NOT in
    /// <see cref="CaseFile.KnownClues"/>. Used for replay tests that must prove the event
    /// replay path discovers clues from events (not from the already-mutated session state).
    /// </summary>
    public static CaseFile CreateBaselineCaseFileFor(GameSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        // Reconstruct the baseline CaseFile with the same suspects, true culprit, opening lead,
        // and public clues/warrants — but with empty known clues/warrants (pre-investigation state).
        var originalCase = session.CaseFile;
        return new CaseFile(
            accusation: null,
            originalCase.Suspects,
            originalCase.TrueCulpritId,
            originalCase.OpeningLead,
            knownClues: Array.Empty<Clue>(),
            discoveredSuspectIds: originalCase.DiscoveredSuspectIds,
            publicClues: ReconstructPublicClues(originalCase),
            publicWarrants: ReconstructPublicWarrants(originalCase));
    }

    private static IEnumerable<Clue> ReconstructPublicClues(CaseFile originalCase)
    {
        // The public clues that remain in the session are the un-revealed ones.
        // For a baseline, we need ALL public clues (including ones already revealed).
        // Since we can't recover revealed clues from the mutated session, we use
        // the KnownClues as the clues that were originally public and are now known.
        // This reconstructs the original public pool: known clues (that came from public) + remaining public.
        var knownFromPublic = originalCase.KnownClues
            .Where(c => c.SourceKind.HasValue)
            .ToList();
        return knownFromPublic.Concat(originalCase.PublicClues).ToList();
    }

    private static IEnumerable<Warrant> ReconstructPublicWarrants(CaseFile originalCase)
    {
        // Same logic as clues: known warrants (from public) + remaining public warrants.
        return originalCase.KnownWarrants.Concat(originalCase.PublicWarrants).ToList();
    }
}
