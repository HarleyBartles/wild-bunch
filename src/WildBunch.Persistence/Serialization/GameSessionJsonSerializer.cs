using System.Reflection;
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

public sealed class GameSessionJsonSerializer
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
        PlayerSnapshot Player,
        WorldSnapshot World,
        CaseFileSnapshot CaseFile,
        PursuitStateSnapshot PursuitState,
        GameClockSnapshot Clock,
        JourneySnapshot? Journey,
        IReadOnlyList<GameLogEntrySnapshot> LogEntries)
    {
        public static GameSessionSnapshot FromDomain(GameSession session)
            => new(
                session.Id.Value,
                session.Status,
                PlayerSnapshot.FromDomain(session.Player),
                WorldSnapshot.FromDomain(session.World),
                CaseFileSnapshot.FromDomain(session.CaseFile),
                PursuitStateSnapshot.FromDomain(session.PursuitState),
                GameClockSnapshot.FromDomain(session.Clock),
                session.Journey is null ? null : JourneySnapshot.FromDomain(session.Journey.ToSnapshot()),
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
                journey);

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
        WaterFeature WaterFeature)
    {
        public static TrailSnapshot FromDomain(DomainTrail trail)
            => new(
                trail.Id.Value,
                trail.FromTownId.Value,
                trail.ToTownId.Value,
                trail.Risk,
                trail.Terrain,
                trail.WaterFeature);

        public static DomainTrail ToDomain(TrailSnapshot snapshot)
            => new(
                new TrailId(snapshot.Id),
                new TownId(snapshot.FromTownId),
                new TownId(snapshot.ToTownId),
                snapshot.Risk,
                snapshot.Terrain,
                snapshot.WaterFeature);
    }

    private sealed record CaseFileSnapshot(
        string? AccusationId,
        string? OpeningLead,
        int KillerReleaseProgress,
        int KillerReleaseThreshold,
        IReadOnlyList<SuspectSnapshot> Suspects,
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

    private sealed record ClueSnapshot(string Id, ClueKind Kind, string Description)
    {
        public static ClueSnapshot FromDomain(Clue clue)
            => new(clue.Id.Value, clue.Kind, clue.Description);

        public static Clue ToDomain(ClueSnapshot snapshot)
            => new(new ClueId(snapshot.Id), snapshot.Kind, snapshot.Description);
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

    private sealed record GameLogEntrySnapshot(GameLogEntryKind Kind, string Message, int Day, int Turn)
    {
        public static GameLogEntrySnapshot FromDomain(GameLogEntry entry)
            => new(entry.Kind, entry.Message, entry.Day, entry.Turn);

        public static GameLogEntry ToDomain(GameLogEntrySnapshot snapshot)
            => new(snapshot.Kind, snapshot.Message, snapshot.Day, snapshot.Turn);
    }

    private sealed class JourneySnapshot
    {
        public string OriginTownId { get; set; } = string.Empty;
        public string DestinationTownId { get; set; } = string.Empty;
        public string OriginTownName { get; set; } = string.Empty;
        public string DestinationTownName { get; set; } = string.Empty;
        public TravelRouteProfileSnapshot RouteProfile { get; set; } = new();
        public TravelMode TravelMode { get; set; }
        public JourneyStatus Status { get; set; }
        public bool MountedTravelAvailable { get; set; }
        public bool WaterSecure { get; set; }
        public int TotalDistance { get; set; }
        public int RemainingDistance { get; set; }
        public int ExpectedDays { get; set; }
        public int RemainingDays { get; set; }
        public int RequiredFood { get; set; }
        public int AvailableFood { get; set; }
        public int RequiredHorseFeed { get; set; }
        public int AvailableHorseFeed { get; set; }
        public DomainHorseTravelState? HorseState { get; set; }
        public int DaysTravelled { get; set; }
        public int DelayDays { get; set; }
        public JourneyEncounterSnapshot? PendingEncounter { get; set; }
        public IReadOnlyList<string> Warnings { get; set; } = Array.Empty<string>();

        public static JourneySnapshot FromDomain(TravelJourneySnapshot snapshot)
            => new()
            {
                OriginTownId = snapshot.OriginTownId.Value,
                DestinationTownId = snapshot.DestinationTownId.Value,
                OriginTownName = snapshot.OriginTownName,
                DestinationTownName = snapshot.DestinationTownName,
                RouteProfile = TravelRouteProfileSnapshot.FromDomain(snapshot.RouteProfile),
                TravelMode = snapshot.TravelMode,
                Status = snapshot.Status,
                MountedTravelAvailable = snapshot.MountedTravelAvailable,
                WaterSecure = snapshot.WaterSecure,
                TotalDistance = snapshot.TotalDistance,
                RemainingDistance = snapshot.RemainingDistance,
                ExpectedDays = snapshot.ExpectedDays,
                RemainingDays = snapshot.RemainingDays,
                RequiredFood = snapshot.RequiredFood,
                AvailableFood = snapshot.AvailableFood,
                RequiredHorseFeed = snapshot.RequiredHorseFeed,
                AvailableHorseFeed = snapshot.AvailableHorseFeed,
                HorseState = snapshot.HorseState,
                DaysTravelled = snapshot.DaysTravelled,
                DelayDays = snapshot.DelayDays,
                PendingEncounter = snapshot.PendingEncounter is null ? null : JourneyEncounterSnapshot.FromDomain(snapshot.PendingEncounter),
                Warnings = snapshot.Warnings.ToArray()
            };

        public TravelJourneySnapshot ToDomain()
            => new(
                new TownId(OriginTownId),
                new TownId(DestinationTownId),
                OriginTownName,
                DestinationTownName,
                RouteProfile.ToDomain(),
                TravelMode,
                Status,
                MountedTravelAvailable,
                WaterSecure,
                TotalDistance,
                RemainingDistance,
                ExpectedDays,
                RemainingDays,
                RequiredFood,
                AvailableFood,
                RequiredHorseFeed,
                AvailableHorseFeed,
                HorseState,
                DaysTravelled,
                DelayDays,
                PendingEncounter?.ToDomain(),
                Warnings.ToArray());
    }

    private sealed class JourneyEncounterSnapshot
    {
        public string Kind { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public IReadOnlyList<JourneyEncounterChoiceSnapshot> Choices { get; set; } = Array.Empty<JourneyEncounterChoiceSnapshot>();

        public static JourneyEncounterSnapshot FromDomain(JourneyEncounterState encounter)
            => new()
            {
                Kind = encounter.Kind,
                Message = encounter.Message,
                Choices = encounter.Choices.Select(JourneyEncounterChoiceSnapshot.FromDomain).ToArray()
            };

        public JourneyEncounterState ToDomain()
            => new(
                Kind,
                Message,
                Choices.Select(choice => choice.ToDomain()).ToArray());
    }

    private sealed class JourneyEncounterChoiceSnapshot
    {
        public string Id { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;

        public static JourneyEncounterChoiceSnapshot FromDomain(JourneyEncounterChoiceState choice)
            => new()
            {
                Id = choice.Id,
                Label = choice.Label
            };

        public JourneyEncounterChoiceState ToDomain()
            => new(Id, Label);
    }

    private sealed class TravelRouteProfileSnapshot
    {
        public string TrailId { get; set; } = string.Empty;
        public TrailRisk Risk { get; set; }
        public TrailTerrain Terrain { get; set; }
        public WaterFeature WaterFeature { get; set; }
        public int TotalDistance { get; set; }
        public int MountedDailyProgress { get; set; }
        public int FootDailyProgress { get; set; }
        public IReadOnlyList<string> Warnings { get; set; } = Array.Empty<string>();

        public static TravelRouteProfileSnapshot FromDomain(TravelRouteProfile routeProfile)
            => new()
            {
                TrailId = routeProfile.TrailId,
                Risk = routeProfile.Risk,
                Terrain = routeProfile.Terrain,
                WaterFeature = routeProfile.WaterFeature,
                TotalDistance = routeProfile.TotalDistance,
                MountedDailyProgress = routeProfile.MountedDailyProgress,
                FootDailyProgress = routeProfile.FootDailyProgress,
                Warnings = routeProfile.Warnings.ToArray()
            };

        public TravelRouteProfile ToDomain()
            => new(
                TrailId,
                Risk,
                Terrain,
                WaterFeature,
                TotalDistance,
                MountedDailyProgress,
                FootDailyProgress,
                Warnings.ToArray());
    }

    private static class GameSessionRehydrator
    {
        private static readonly ConstructorInfo? Constructor = typeof(GameSession).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            new[]
            {
                typeof(GameSessionId),
                typeof(Player),
                typeof(DomainWorld),
                typeof(CaseFile),
                typeof(PursuitState),
                typeof(GameClock),
                typeof(GameStatus),
                typeof(TravelJourney)
            },
            modifiers: null);

        private static readonly FieldInfo? LogEntriesField = typeof(GameSession).GetField("_logEntries", BindingFlags.Instance | BindingFlags.NonPublic);

        public static GameSession Create(
            GameSessionId id,
            Player player,
            DomainWorld world,
            CaseFile caseFile,
            PursuitState pursuitState,
            GameClock clock,
            GameStatus status,
            TravelJourney? journey)
        {
            if (Constructor is null)
            {
                throw new InvalidOperationException("Unable to locate the GameSession persistence constructor.");
            }

            return (GameSession)Constructor.Invoke(new object[] { id, player, world, caseFile, pursuitState, clock, status, journey });
        }

        public static void ReplaceLogEntries(GameSession session, IReadOnlyList<GameLogEntry> logEntries)
        {
            if (LogEntriesField?.GetValue(session) is not List<GameLogEntry> entries)
            {
                throw new InvalidOperationException("Unable to access game log entries for rehydration.");
            }

            entries.Clear();
            entries.AddRange(logEntries);
        }

        public static void SetBackingField<T>(object target, string fieldName, T value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException($"Unable to access field {fieldName} on {target.GetType().Name}.");

            field.SetValue(target, value);
        }
    }
}
