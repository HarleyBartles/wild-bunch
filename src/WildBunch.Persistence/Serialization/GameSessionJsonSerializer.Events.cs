using System.Text.Json;
using WildBunch.Domain.Events;

namespace WildBunch.Persistence.Serialization;

public sealed partial class GameSessionJsonSerializer
{
    private static readonly JsonSerializerOptions EventOptions = CreateEventOptions();

    /// <summary>
    /// Serializes a typed domain event to JSON for storage in the event stream.
    /// The event type name is the concrete type name (e.g., "GameStarted", "StoreItemPurchased").
    /// </summary>
    public string SerializeEvent(IDomainEvent e)
    {
        ArgumentNullException.ThrowIfNull(e);
        return JsonSerializer.Serialize(e, e.GetType(), EventOptions);
    }

    /// <summary>
    /// Deserializes a stored event payload back to a typed domain event.
    /// Throws if the event type is unknown.
    /// </summary>
    public IDomainEvent DeserializeEvent(string eventType, string payloadJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentException.ThrowIfNullOrWhiteSpace(payloadJson);

        var type = ResolveEventType(eventType);
        return (IDomainEvent)(JsonSerializer.Deserialize(payloadJson, type, EventOptions)
            ?? throw new InvalidOperationException($"Unable to deserialize event payload as {eventType}."));
    }

    private static Type ResolveEventType(string eventType) => eventType switch
    {
        nameof(GameStarted) => typeof(GameStarted),
        nameof(StoreItemPurchased) => typeof(StoreItemPurchased),
        nameof(InvestigationPerformed) => typeof(InvestigationPerformed),
        nameof(TownActionContextEntered) => typeof(TownActionContextEntered),
        nameof(SaloonPersonOfInterestSpotted) => typeof(SaloonPersonOfInterestSpotted),
        nameof(WantedSuspectConfronted) => typeof(WantedSuspectConfronted),
        nameof(SheriffTurnInSettled) => typeof(SheriffTurnInSettled),
        nameof(SaloonPersonOfInterestConfronted) => typeof(SaloonPersonOfInterestConfronted),
        nameof(JourneyStarted) => typeof(JourneyStarted),
        nameof(TravelDayAdvanced) => typeof(TravelDayAdvanced),
        nameof(TrailEventApplied) => typeof(TrailEventApplied),
        nameof(JourneyEncounterResolved) => typeof(JourneyEncounterResolved),
        nameof(JourneyCompleted) => typeof(JourneyCompleted),
        nameof(JourneyArrivalAcknowledged) => typeof(JourneyArrivalAcknowledged),
        _ => throw new InvalidOperationException($"Unknown domain event type: {eventType}")
    };

    private static JsonSerializerOptions CreateEventOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new OutlawGangIdJsonConverter());
        return options;
    }
}
