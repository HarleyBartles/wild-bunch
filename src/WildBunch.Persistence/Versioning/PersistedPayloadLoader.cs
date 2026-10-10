using WildBunch.Application.Projections;
using WildBunch.Domain.Events;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Persistence.GameSessions;
using WildBunch.Persistence.Serialization;

namespace WildBunch.Persistence.Versioning;

/// <summary>
/// The single funnel that turns persisted rows into domain objects.
/// Events are upcasted via PayloadUpcasterRegistry. Components and diary
/// days are version-checked against ProjectionVersions — if the stored
/// version doesn't match current, the projection is rebuilt from the
/// event stream. No code outside WildBunch.Persistence should call
/// GameSessionJsonSerializer's deserialize methods directly — this loader
/// is the only sanctioned surface. See the event sourcing integrity policy.
/// </summary>
public sealed class PersistedPayloadLoader
{
    private readonly PayloadUpcasterRegistry _eventUpcasters;
    private readonly GameSessionJsonSerializer _serializer;
    private readonly TravelDiaryDayProjector _diaryDayProjector;
    private readonly Func<IReadOnlyList<IDomainEvent>, GameSession> _rebuildSessionFromEvents;

    public PersistedPayloadLoader(
        PayloadUpcasterRegistry eventUpcasters,
        GameSessionJsonSerializer serializer,
        TravelDiaryDayProjector diaryDayProjector,
        Func<IReadOnlyList<IDomainEvent>, GameSession> rebuildSessionFromEvents)
    {
        _eventUpcasters = eventUpcasters;
        _serializer = serializer;
        _diaryDayProjector = diaryDayProjector;
        _rebuildSessionFromEvents = rebuildSessionFromEvents;
    }

    /// <summary>
    /// Loads a single event: upcast via the registry, then deserialize.
    /// The upcaster registry fails closed on future versions (code older
    /// than data) and on missing upcasters in the chain.
    /// </summary>
    public IDomainEvent LoadEvent(StoredEventEntity stored)
    {
        var json = _eventUpcasters.Upcast(
            stored.EventType, stored.SchemaVersion, stored.PayloadJson);
        return _serializer.DeserializeEvent(stored.EventType, json);
    }

    /// <summary>
    /// Loads a batch of events: upcast + deserialize each.
    /// Convenience method for load paths that fetch the full event stream.
    /// </summary>
    public IReadOnlyList<IDomainEvent> LoadEvents(IReadOnlyList<StoredEventEntity> stored)
    {
        var events = new IDomainEvent[stored.Count];
        for (var i = 0; i < stored.Count; i++)
        {
            events[i] = LoadEvent(stored[i]);
        }
        return events;
    }

    /// <summary>
    /// Loads a component's payload JSON: version-check, rebuild if stale.
    /// Returns null if the component doesn't exist. If the stored version
    /// doesn't match ProjectionVersions.ForComponent(name), the component
    /// is rebuilt from the event stream via the rebuild callback (which
    /// rehydrates the full session and extracts the component).
    /// </summary>
    public string? LoadComponentPayload(
        IReadOnlyDictionary<string, GameSessionComponentEntity> components,
        string componentName,
        IReadOnlyList<IDomainEvent> events)
        => CreateComponentPayloadReadScope(components, events).GetPayload(componentName);

    /// <summary>
    /// Creates request-local component access for one coherent store load. If
    /// more than one component is stale, all recovered payloads come from the
    /// same event-derived aggregate reconstruction.
    /// </summary>
    internal ComponentPayloadReadScope CreateComponentPayloadReadScope(
        IReadOnlyDictionary<string, GameSessionComponentEntity> components,
        IReadOnlyList<IDomainEvent> events)
        => new(this, components, events);

    internal sealed class ComponentPayloadReadScope
    {
        private readonly PersistedPayloadLoader _owner;
        private readonly IReadOnlyDictionary<string, GameSessionComponentEntity> _components;
        private readonly IReadOnlyList<IDomainEvent> _events;
        private readonly Dictionary<string, string> _recoveredPayloads = new(StringComparer.Ordinal);
        private GameSession? _rebuiltSession;

        internal ComponentPayloadReadScope(
            PersistedPayloadLoader owner,
            IReadOnlyDictionary<string, GameSessionComponentEntity> components,
            IReadOnlyList<IDomainEvent> events)
        {
            _owner = owner;
            _components = components;
            _events = events;
        }

        internal string? GetPayload(string componentName)
        {
            if (!_components.TryGetValue(componentName, out var entity))
                return null;

            if (entity.ComponentVersion == ProjectionVersions.ForComponent(componentName))
                return entity.PayloadJson;

            if (_recoveredPayloads.TryGetValue(componentName, out var payload))
                return payload;

            _rebuiltSession ??= _owner._rebuildSessionFromEvents(_events);
            payload = _owner.SerializeComponentByName(_rebuiltSession, componentName);
            _recoveredPayloads.Add(componentName, payload);
            return payload;
        }
    }

    /// <summary>
    /// Loads diary days: version-check, rebuild if stale.
    /// If any row's SchemaVersion doesn't match ProjectionVersions.DiaryDay,
    /// all diary days are discarded and rebuilt via TravelDiaryDayProjector.
    /// If no rows exist, rebuild from events (empty sessions get an empty list).
    /// </summary>
    public IReadOnlyList<TravelDiaryDayState> LoadDiaryDays(
        IReadOnlyList<GameSessionDiaryDayEntity> stored,
        IReadOnlyList<IDomainEvent> events,
        long streamVersion,
        long? projectionStreamVersion,
        int? projectionDayCount)
    {
        var ordered = stored.OrderBy(day => day.Sequence).ToArray();
        var hasCurrentSchema = ordered.All(day => day.SchemaVersion == ProjectionVersions.DiaryDay);
        var hasContiguousSequence = ordered.Select(day => day.Sequence).SequenceEqual(Enumerable.Range(0, ordered.Length));
        if (projectionStreamVersion == streamVersion
            && projectionDayCount == ordered.Length
            && hasCurrentSchema
            && hasContiguousSequence)
        {
            try
            {
                return ordered.Select(d => _serializer.DeserializeTravelDiaryDay(d.PayloadJson)).ToArray();
            }
            catch (InvalidComponentCacheShapeException)
            {
                // Current metadata cannot make an unmaterializable diary row valid.
                // Rebuild the complete projection; authoritative history failures escape.
            }
        }

        // Stale, empty or malformed: rebuild from events via the projector.
        return _diaryDayProjector.Project(events).Days;
    }

    private string SerializeComponentByName(GameSession session, string componentName)
    {
        return componentName switch
        {
            GameSessionComponentNames.Player => _serializer.SerializePlayer(session.Player),
            GameSessionComponentNames.World => _serializer.SerializeWorld(session.World),
            GameSessionComponentNames.CaseFile => _serializer.SerializeCaseFile(session.CaseFile),
            GameSessionComponentNames.Clock => _serializer.SerializeClock(session.Clock),
            GameSessionComponentNames.PursuitState => _serializer.SerializePursuitState(session.PursuitState),
            GameSessionComponentNames.Setup => _serializer.SerializeSetup(session.GameEntropy),
            GameSessionComponentNames.SaltSource => _serializer.SerializeSaltSource(session.SaltSource),
            GameSessionComponentNames.TownVisitState => _serializer.SerializeTownVisitState(session.TownVisitStateOrNull ?? throw new InvalidOperationException("Cannot rebuild null TownVisitState.")),
            GameSessionComponentNames.Journey => _serializer.SerializeJourneySnapshot(session.Journey?.ToSnapshot(session.TravelRules) ?? throw new InvalidOperationException("Cannot rebuild null Journey.")),
            GameSessionComponentNames.CompletedJourneyHistory => _serializer.SerializeCompletedJourneyHistory(session.CompletedJourneyHistory),
            GameSessionComponentNames.WantedSuspectPresenceLedger => _serializer.SerializeWantedSuspectPresenceLedger(session.WantedSuspectPresenceEntries),
            GameSessionComponentNames.CurrentActionContext => _serializer.SerializeCurrentActionContext(session.CurrentActionContext, session.CurrentActionContextTownId),
            GameSessionComponentNames.PendingDevTravelOverride => _serializer.SerializePendingDevTravelOverride(session.PendingDevTravelOverride) ?? throw new InvalidOperationException("Cannot rebuild null PendingDevTravelOverride."),
            GameSessionComponentNames.PendingDevSaloonOverride => _serializer.SerializePendingDevSaloonOverride(session.PendingDevSaloonOverride) ?? throw new InvalidOperationException("Cannot rebuild null PendingDevSaloonOverride."),
            _ => throw new InvalidOperationException($"Unknown component name '{componentName}' for rebuild."),
        };
    }
}
