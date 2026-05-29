using System.Reflection;
using System.Text.Json;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;
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
                session.LogEntries.Select(GameLogEntrySnapshot.FromDomain).ToArray());

        public GameSession ToDomain()
        {
            var world = WorldSnapshot.ToDomain(World);
            var caseFile = CaseFileSnapshot.ToDomain(CaseFile);
            var player = PlayerSnapshot.ToDomain(Player);
            var pursuitState = PursuitStateSnapshot.ToDomain(PursuitState);
            var clock = GameClockSnapshot.ToDomain(Clock);
            var session = GameSessionRehydrator.Create(
                new GameSessionId(Id),
                player,
                world,
                caseFile,
                pursuitState,
                clock,
                Status);

            GameSessionRehydrator.ReplaceLogEntries(session, LogEntries.Select(GameLogEntrySnapshot.ToDomain).ToArray());
            return session;
        }
    }

    private sealed record PlayerSnapshot(string Name, string CurrentTownId, int Health, decimal Money, int Supplies)
    {
        public static PlayerSnapshot FromDomain(Player player)
            => new(player.Name, player.CurrentTownId.Value, player.Health, player.Money, player.Supplies.Units);

        public static Player ToDomain(PlayerSnapshot snapshot)
            => new(
                snapshot.Name,
                new TownId(snapshot.CurrentTownId),
                snapshot.Health,
                snapshot.Money,
                new Supplies(snapshot.Supplies));
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

    private sealed record TrailSnapshot(string Id, string FromTownId, string ToTownId, int SupplyCost, TrailRisk Risk)
    {
        public static TrailSnapshot FromDomain(DomainTrail trail)
            => new(trail.Id.Value, trail.FromTownId.Value, trail.ToTownId.Value, trail.SupplyCost, trail.Risk);

        public static DomainTrail ToDomain(TrailSnapshot snapshot)
            => new(new TrailId(snapshot.Id), new TownId(snapshot.FromTownId), new TownId(snapshot.ToTownId), snapshot.SupplyCost, snapshot.Risk);
    }

    private sealed record CaseFileSnapshot(
        string? AccusationId,
        IReadOnlyList<SuspectSnapshot> Suspects,
        string TrueCulpritId,
        IReadOnlyList<ClueSnapshot> KnownClues,
        IReadOnlyList<ClueSnapshot>? PublicClues)
    {
        public static CaseFileSnapshot FromDomain(CaseFile caseFile)
            => new(
                caseFile.Accusation is null ? null : caseFile.Accusation.Value.Value,
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
                snapshot.KnownClues.Select(ClueSnapshot.ToDomain),
                snapshot.PublicClues?.Select(ClueSnapshot.ToDomain));

            return caseFile;
        }
    }

    private sealed record SuspectSnapshot(string Id, string Name, SuspectTraitsSnapshot Traits, SuspectStatus Status)
    {
        public static SuspectSnapshot FromDomain(Suspect suspect)
            => new(suspect.Id.Value, suspect.Name, SuspectTraitsSnapshot.FromDomain(suspect.Traits), suspect.Status);

        public static Suspect ToDomain(SuspectSnapshot snapshot)
            => new(new SuspectId(snapshot.Id), snapshot.Name, SuspectTraitsSnapshot.ToDomain(snapshot.Traits), snapshot.Status);
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
                typeof(GameStatus)
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
            GameStatus status)
        {
            if (Constructor is null)
            {
                throw new InvalidOperationException("Unable to locate the GameSession persistence constructor.");
            }

            return (GameSession)Constructor.Invoke(new object[] { id, player, world, caseFile, pursuitState, clock, status });
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
