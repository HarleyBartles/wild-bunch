namespace WildBunch.Domain.Travel;

public sealed record TravelJourneyStepResult(
    bool Success,
    JourneyStatus Status,
    string Message,
    string LogMessage,
    int HeatIncrease,
    TravelJourneySnapshot? Journey = null,
    JourneyTrailEventState? TrailEvent = null)
{
    public static TravelJourneyStepResult Failed(string message)
        => new(false, JourneyStatus.Failed, message, message, 0);
}

public sealed record JourneyEncounterResolutionResult(
    bool Success,
    bool SessionChanged,
    JourneyStatus Status,
    string Message,
    TravelJourneySnapshot? Journey = null,
    IReadOnlyList<string>? AdditionalDiaryMessages = null)
{
    public static JourneyEncounterResolutionResult Failed(string message, JourneyStatus status, TravelJourneySnapshot? journey = null)
        => new(false, false, status, message, journey);
}

public sealed record JourneyArrivalAcknowledgementResult(
    bool Success,
    string Message,
    TravelJourneySnapshot? Journey = null)
{
    public static JourneyArrivalAcknowledgementResult Failed(string message, TravelJourneySnapshot? journey = null)
        => new(false, message, journey);
}

public sealed record TravelPreviewResult(bool Success, string Message, TravelPreview? Preview)
{
    public static TravelPreviewResult Failed(string message) => new(false, message, null);
}
