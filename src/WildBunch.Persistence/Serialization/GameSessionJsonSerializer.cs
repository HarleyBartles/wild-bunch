using System.Text.Json;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Economy;
using WildBunch.Domain.Game;
using DomainCanteenState = WildBunch.Domain.Inventory.CanteenState;
using DomainHorseTravelState = WildBunch.Domain.Inventory.HorseTravelState;
using WildBunch.Domain.Travel;
using DomainInventory = WildBunch.Domain.Inventory.Inventory;
using DomainInventoryItem = WildBunch.Domain.Inventory.InventoryItem;
using DomainInventoryItemKind = WildBunch.Domain.Inventory.ItemKind;
using WildBunch.Domain.World;
using DomainWorld = WildBunch.Domain.World.World;
using TownId = WildBunch.Domain.World.TownId;
using TrailId = WildBunch.Domain.World.TrailId;
using DomainTown = WildBunch.Domain.World.Town;
using DomainTrail = WildBunch.Domain.World.Trail;

namespace WildBunch.Persistence.Serialization;

public sealed partial class GameSessionJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public string Serialize(GameSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        return JsonSerializer.Serialize(GameSessionSnapshot.FromDomain(session), Options);
    }

    public GameSession Deserialize(string stateJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stateJson);

        var snapshot = JsonSerializer.Deserialize<GameSessionSnapshot>(stateJson, Options)
            ?? throw new InvalidOperationException("Unable to deserialize game session state.");

        return snapshot.ToDomain();
    }

    private sealed record GameSessionSnapshot(
        Guid Id,
        GameStatus Status,
        TravelDifficulty TravelDifficulty,
        TravelRandomnessSnapshot? TravelRandomness,
        PlayerSnapshot Player,
        WorldSnapshot World,
        CaseFileSnapshot CaseFile,
        PursuitStateSnapshot PursuitState,
        GameClockSnapshot Clock,
        JourneySnapshot? Journey,
        IReadOnlyList<TravelDiaryDayState> TravelDiaryDays,
        IReadOnlyList<GameLogEntrySnapshot> LogEntries)
    {
        public static GameSessionSnapshot FromDomain(GameSession session)
            => new(
                session.Id.Value,
                session.Status,
                session.TravelDifficulty,
                TravelRandomnessSnapshot.FromDomain(session.TravelRandomness),
                PlayerSnapshot.FromDomain(session.Player),
                WorldSnapshot.FromDomain(session.World),
                CaseFileSnapshot.FromDomain(session.CaseFile),
                PursuitStateSnapshot.FromDomain(session.PursuitState),
                GameClockSnapshot.FromDomain(session.Clock),
                session.Journey is null ? null : JourneySnapshot.FromDomain(session.Journey.ToSnapshot(session.TravelRules)),
                session.TravelDiaryDays.ToArray(),
                session.LogEntries.Select(GameLogEntrySnapshot.FromDomain).ToArray());

        public GameSession ToDomain()
        {
            var world = WorldSnapshot.ToDomain(World);
            var caseFile = CaseFileSnapshot.ToDomain(CaseFile);
            var player = PlayerSnapshot.ToDomain(Player);
            var pursuitState = PursuitStateSnapshot.ToDomain(PursuitState);
            var clock = GameClockSnapshot.ToDomain(Clock);
            var journey = Journey is null ? null : TravelJourney.FromSnapshot(Journey.ToDomain());
            var session = GameSessionRehydrator.Create(
                new GameSessionId(Id),
                player,
                world,
                caseFile,
                pursuitState,
                clock,
                Status,
                journey,
                TravelDifficulty,
                TravelRandomness?.ToDomain() ?? TravelRandomnessState.CreateRuntimeSalted());

            GameSessionRehydrator.ReplaceTravelDiaryDays(session, TravelDiaryDays);
            GameSessionRehydrator.ReplaceLogEntries(session, LogEntries.Select(GameLogEntrySnapshot.ToDomain).ToArray());
            return session;
        }
    }

    private sealed record PlayerSnapshot(
        string Name,
        string CurrentTownId,
        int Health,
        WalletSnapshot? Wallet,
        InventorySnapshot? Inventory)
    {
        public static PlayerSnapshot FromDomain(Player player)
            => new(
                player.Name,
                player.CurrentTownId.Value,
                player.Health,
                WalletSnapshot.FromDomain(player.Wallet),
                InventorySnapshot.FromDomain(player.Inventory));

        public static Player ToDomain(PlayerSnapshot snapshot)
            => new(
                snapshot.Name,
                new TownId(snapshot.CurrentTownId),
                snapshot.Health,
                WalletSnapshot.ToDomain(snapshot.Wallet),
                InventorySnapshot.ToDomain(snapshot.Inventory));
    }

    private sealed record WalletSnapshot(decimal Cash)
    {
        public static WalletSnapshot FromDomain(Wallet wallet)
            => new(wallet.Cash);

        public static Wallet ToDomain(WalletSnapshot? snapshot)
            => snapshot is null
                ? throw new InvalidOperationException("Unable to deserialize player wallet.")
                : new Wallet(snapshot.Cash);
    }

    private sealed record InventorySnapshot(IReadOnlyList<InventoryItemSnapshot> Items)
    {
        public static InventorySnapshot FromDomain(DomainInventory inventory)
            => new(inventory.Items.Select(InventoryItemSnapshot.FromDomain).ToArray());

        public static DomainInventory ToDomain(InventorySnapshot? snapshot)
            => snapshot is null
                ? DomainInventory.Empty()
                : new DomainInventory(snapshot.Items.Select(InventoryItemSnapshot.ToDomain));
    }

    private sealed record InventoryItemSnapshot(
        DomainInventoryItemKind Kind,
        int Quantity,
        DomainHorseTravelState? HorseState,
        DomainCanteenState? CanteenState)
    {
        public static InventoryItemSnapshot FromDomain(DomainInventoryItem item)
            => new(item.Kind, item.Quantity, item.HorseState, item.CanteenState);

        public static DomainInventoryItem ToDomain(InventoryItemSnapshot snapshot)
            => new(snapshot.Kind, snapshot.Quantity, snapshot.HorseState, snapshot.CanteenState);
    }

    private sealed record WorldSnapshot(IReadOnlyList<TownSnapshot> Towns, IReadOnlyList<TrailSnapshot> Trails)
    {
        public static WorldSnapshot FromDomain(DomainWorld world)
            => new(
                world.Towns.Select(TownSnapshot.FromDomain).ToArray(),
                world.Trails.Select(TrailSnapshot.FromDomain).ToArray());

        public static DomainWorld ToDomain(WorldSnapshot snapshot)
            => new(
                snapshot.Towns.Select(TownSnapshot.ToDomain),
                snapshot.Trails.Select(TrailSnapshot.ToDomain));
    }

    private sealed record TownSnapshot(string Id, string Name, TownServices Services)
    {
        public static TownSnapshot FromDomain(DomainTown town)
            => new(town.Id.Value, town.Name, town.Services);

        public static DomainTown ToDomain(TownSnapshot snapshot)
            => new(new TownId(snapshot.Id), snapshot.Name, snapshot.Services);
    }

    private sealed record TrailSnapshot(
        string Id,
        string FromTownId,
        string ToTownId,
        TrailRisk Risk,
        TrailTerrain Terrain,
        WaterFeature WaterFeature,
        decimal RideDayDistance)
    {
        public static TrailSnapshot FromDomain(DomainTrail trail)
            => new(
                trail.Id.Value,
                trail.FromTownId.Value,
                trail.ToTownId.Value,
                trail.Risk,
                trail.Terrain,
                trail.WaterFeature,
                trail.RideDayDistance);

        public static DomainTrail ToDomain(TrailSnapshot snapshot)
            => new(
                new TrailId(snapshot.Id),
                new TownId(snapshot.FromTownId),
                new TownId(snapshot.ToTownId),
                snapshot.Risk,
                snapshot.Terrain,
                snapshot.WaterFeature,
                snapshot.RideDayDistance);
    }

    private sealed record CaseFileSnapshot(
        string? AccusationId,
        string? OpeningLead,
        int KillerReleaseProgress,
        int KillerReleaseThreshold,
        IReadOnlyList<SuspectSnapshot> Suspects,
        IReadOnlyList<string>? DiscoveredSuspectIds,
        string TrueCulpritId,
        IReadOnlyList<ClueSnapshot> KnownClues,
        IReadOnlyList<ClueSnapshot>? PublicClues)
    {
        public static CaseFileSnapshot FromDomain(CaseFile caseFile)
            => new(
                caseFile.Accusation is null ? null : caseFile.Accusation.Value.Value,
                caseFile.OpeningLead.Description,
                caseFile.KillerReleaseProgress,
                caseFile.KillerReleaseThreshold,
                caseFile.Suspects.Select(SuspectSnapshot.FromDomain).ToArray(),
                caseFile.DiscoveredSuspectIds.Select(suspectId => suspectId.Value).ToArray(),
                caseFile.TrueCulpritId.Value,
                caseFile.KnownClues.Select(ClueSnapshot.FromDomain).ToArray(),
                caseFile.PublicClues.Select(ClueSnapshot.FromDomain).ToArray());

        public static CaseFile ToDomain(CaseFileSnapshot snapshot)
        {
            var caseFile = new CaseFile(
                snapshot.AccusationId is null ? null : new SuspectId(snapshot.AccusationId),
                snapshot.Suspects.Select(SuspectSnapshot.ToDomain),
                new SuspectId(snapshot.TrueCulpritId),
                CaseOpeningLead.Create(snapshot.OpeningLead ?? "Follow the public leads and look for a signature mark."),
                snapshot.KnownClues.Select(ClueSnapshot.ToDomain),
                (snapshot.DiscoveredSuspectIds ?? Array.Empty<string>()).Select(suspectId => new SuspectId(suspectId)),
                snapshot.PublicClues?.Select(ClueSnapshot.ToDomain),
                snapshot.KillerReleaseThreshold,
                snapshot.KillerReleaseProgress);

            return caseFile;
        }
    }

    private sealed record SuspectSnapshot(string Id, string Name, SuspectProfileSnapshot Profile, SuspectTraitsSnapshot Traits, SuspectStatus Status)
    {
        public static SuspectSnapshot FromDomain(Suspect suspect)
            => new(suspect.Id.Value, suspect.Name, SuspectProfileSnapshot.FromDomain(suspect.Profile), SuspectTraitsSnapshot.FromDomain(suspect.Traits), suspect.Status);

        public static Suspect ToDomain(SuspectSnapshot snapshot)
            => new(new SuspectId(snapshot.Id), snapshot.Name, SuspectProfileSnapshot.ToDomain(snapshot.Profile), SuspectTraitsSnapshot.ToDomain(snapshot.Traits), snapshot.Status);
    }

    private sealed record SuspectProfileSnapshot(IReadOnlyList<SuspectAliasSnapshot> Aliases, IReadOnlyList<SuspectIdentityFactSnapshot> IdentifyingFacts)
    {
        public static SuspectProfileSnapshot FromDomain(SuspectProfile profile)
            => new(
                profile.Aliases.Select(SuspectAliasSnapshot.FromDomain).ToArray(),
                profile.IdentifyingFacts.Select(SuspectIdentityFactSnapshot.FromDomain).ToArray());

        public static SuspectProfile ToDomain(SuspectProfileSnapshot snapshot)
            => new(
                (snapshot.Aliases ?? Array.Empty<SuspectAliasSnapshot>()).Select(SuspectAliasSnapshot.ToDomain),
                (snapshot.IdentifyingFacts ?? Array.Empty<SuspectIdentityFactSnapshot>()).Select(SuspectIdentityFactSnapshot.ToDomain));
    }

    private sealed record SuspectAliasSnapshot(string Name, AliasKind Kind)
    {
        public static SuspectAliasSnapshot FromDomain(SuspectAlias alias)
            => new(alias.Name, alias.Kind);

        public static SuspectAlias ToDomain(SuspectAliasSnapshot snapshot)
            => new(snapshot.Name, snapshot.Kind);
    }

    private sealed record SuspectIdentityFactSnapshot(string Description)
    {
        public static SuspectIdentityFactSnapshot FromDomain(SuspectIdentityFact fact)
            => new(fact.Description);

        public static SuspectIdentityFact ToDomain(SuspectIdentityFactSnapshot snapshot)
            => new(snapshot.Description);
    }

    private sealed record SuspectTraitsSnapshot(bool IsLocal, bool IsArmed, bool IsDesperate)
    {
        public static SuspectTraitsSnapshot FromDomain(SuspectTraits traits)
            => new(traits.IsLocal, traits.IsArmed, traits.IsDesperate);

        public static SuspectTraits ToDomain(SuspectTraitsSnapshot snapshot)
            => new(snapshot.IsLocal, snapshot.IsArmed, snapshot.IsDesperate);
    }

    private sealed record ClueSnapshot
    {
        public ClueSnapshot(string id, ClueKind kind, string description, IReadOnlyList<string>? linkedSuspectIds = null)
        {
            Id = id;
            Kind = kind;
            Description = description;
            LinkedSuspectIds = linkedSuspectIds;
        }

        public string Id { get; init; }
        public ClueKind Kind { get; init; }
        public string Description { get; init; }
        public IReadOnlyList<string>? LinkedSuspectIds { get; init; }

        public static ClueSnapshot FromDomain(Clue clue)
            => new(
                clue.Id.Value,
                clue.Kind,
                clue.Description,
                clue.LinkedSuspectIds.Select(suspectId => suspectId.Value).ToArray());

        public static Clue ToDomain(ClueSnapshot snapshot)
            => new(
                new ClueId(snapshot.Id),
                snapshot.Kind,
                snapshot.Description,
                (snapshot.LinkedSuspectIds ?? Array.Empty<string>()).Select(suspectId => new SuspectId(suspectId)));
    }

    private sealed record PursuitStateSnapshot(int Heat)
    {
        public static PursuitStateSnapshot FromDomain(PursuitState pursuitState)
            => new(pursuitState.Heat);

        public static PursuitState ToDomain(PursuitStateSnapshot snapshot)
        {
            var pursuitState = new PursuitState();
            GameSessionRehydrator.SetBackingField(pursuitState, "<Heat>k__BackingField", snapshot.Heat);
            return pursuitState;
        }
    }

    private sealed record GameClockSnapshot(int Day, int Turn)
    {
        public static GameClockSnapshot FromDomain(GameClock clock)
            => new(clock.Day, clock.Turn);

        public static GameClock ToDomain(GameClockSnapshot snapshot)
        {
            var clock = new GameClock();
            GameSessionRehydrator.SetBackingField(clock, "<Day>k__BackingField", snapshot.Day);
            GameSessionRehydrator.SetBackingField(clock, "<Turn>k__BackingField", snapshot.Turn);
            return clock;
        }
    }

    private sealed record TravelRandomnessSnapshot(TravelRandomnessMode Mode, string Salt)
    {
        public static TravelRandomnessSnapshot FromDomain(TravelRandomnessState randomnessState)
            => new(randomnessState.Mode, randomnessState.Salt);

        public TravelRandomnessState ToDomain()
            => new(Mode, Salt);
    }

    private sealed record GameLogEntrySnapshot(GameLogEntryKind Kind, string Message, int Day, int Turn)
    {
        public static GameLogEntrySnapshot FromDomain(GameLogEntry entry)
            => new(entry.Kind, entry.Message, entry.Day, entry.Turn);

        public static GameLogEntry ToDomain(GameLogEntrySnapshot snapshot)
            => new(snapshot.Kind, snapshot.Message, snapshot.Day, snapshot.Turn);
    }
}
