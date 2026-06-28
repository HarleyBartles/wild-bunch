using System.Text.RegularExpressions;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Events;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;

namespace WildBunch.Application.Projections;

/// <summary>
/// Reference projector for the full audit projection.
/// Derives the complete event log from typed domain events.
/// This is a pure function over the event stream - no aggregate mutation.
/// See ADR-0028.
/// </summary>
public sealed class FullAuditProjector : IDomainEventProjector<FullAuditProjection>
{
    public FullAuditProjection Project(IReadOnlyList<IDomainEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);

        var entries = new List<AuditEntry>();
        var sequence = 0;
        foreach (var e in events)
        {
            sequence++;
            entries.Add(new AuditEntry(
                sequence,
                e.GetType().Name,
                Summarize(e),
                DateTime.UtcNow));
        }

        return new FullAuditProjection(Guid.Empty, entries);
    }

    private static string Summarize(IDomainEvent e) => e switch
    {
        GameStarted gs => $"Game started: {gs.PlayerName} in {gs.StartingTownName} ({gs.GameDifficulty}).",
        StoreItemPurchased purchase => $"Purchased {purchase.Quantity}x {purchase.DisplayName} for {purchase.TotalPrice:C} (wallet: {purchase.WalletAfter:C}).",
        TownActionContextEntered contextEntered => $"Entered {FormatTownContext(contextEntered.Context)} in {contextEntered.TownId.Value} on day {contextEntered.Day}, turn {contextEntered.Turn} ({contextEntered.TimeOfDay}); heat {contextEntered.PursuitHeat}.",
        InvestigationPerformed investigation => $"Investigated {FormatInvestigationSource(investigation.SourceKind)} in {investigation.TownId.Value}: {investigation.Message}",
        SaloonPersonOfInterestSpotted spotted => $"Saloon look-around in {spotted.TownId.Value} found {FormatSaloonPoiKind(spotted.PersonOfInterestKind)}: {spotted.Message}",
        SaloonPersonOfInterestConfronted saloonConfrontation => $"Saloon confrontation with {saloonConfrontation.TargetName} ({FormatSaloonPoiKind(saloonConfrontation.PersonOfInterestKind)}) resolved as {FormatEnumName(saloonConfrontation.Outcome)}: {saloonConfrontation.Message}",
        WantedSuspectConfronted wantedConfrontation => $"Wanted suspect confrontation with {wantedConfrontation.TargetName} ({FormatWarrantDisposition(wantedConfrontation.Disposition)}) resolved as {FormatEnumName(wantedConfrontation.Outcome)}: {wantedConfrontation.Message}",
        SheriffTurnInSettled turnIn => $"Sheriff turn-in settled for {turnIn.TargetName} on day {turnIn.Day}, turn {turnIn.Turn}: {turnIn.BountyAmount:C} bounty.",
        JourneyStarted journeyStarted => $"Journey started from {journeyStarted.JourneySnapshot.OriginTownName} to {journeyStarted.JourneySnapshot.DestinationTownName} by {FormatTravelMode(journeyStarted.JourneySnapshot.TravelMode)} travel ({DescribeJourneyProgress(journeyStarted.JourneySnapshot)}): {journeyStarted.DiaryMessage}",
        TravelDayAdvanced dayAdvanced => $"Travel day {dayAdvanced.Day} advanced for {dayAdvanced.JourneySnapshot.OriginTownName} to {dayAdvanced.JourneySnapshot.DestinationTownName} by {FormatTravelMode(dayAdvanced.JourneySnapshot.TravelMode)} travel ({DescribeJourneyProgress(dayAdvanced.JourneySnapshot)}): {dayAdvanced.DiaryMessage}",
        TrailEventApplied trailEvent => $"Trail event {FormatTrailEventId(trailEvent.TrailEventId)} ({FormatEnumName(trailEvent.TrailEventKind)}): {trailEvent.DiaryMessage}",
        JourneyEncounterResolved encounterResolved => $"Journey encounter {FormatResolutionState(encounterResolved.Resolved)} via {encounterResolved.ChoiceLabel}: {encounterResolved.DiaryMessage}",
        JourneyCompleted journeyCompleted => $"Journey completed to {journeyCompleted.DestinationTownName} from {journeyCompleted.JourneySnapshot.OriginTownName}: {journeyCompleted.DiaryMessage}",
        JourneyArrivalAcknowledged arrivalAcknowledged => $"Journey arrival acknowledged for sequence {arrivalAcknowledged.JourneySequence}: {arrivalAcknowledged.DiaryMessage}",
        DevTravelOverrideForced forced => forced.FoeProfile is null
            ? $"Forced travel override: {FormatEnumName(forced.ForcedCategory)}."
            : $"Forced travel override: {FormatEnumName(forced.ForcedCategory)} with foe profile speed {forced.FoeProfile.Speed}, fight {forced.FoeProfile.FightStrength}, bribe {forced.FoeProfile.MinimumBribe:C}.",
        DevTravelOverrideCleared => "Cleared pending travel override.",
        DevTravelOverrideConsumed => "Consumed pending travel override during travel day advance.",
        DevSaloonOverrideForced forced => forced.ForcedSuspectId is null
            ? $"Forced saloon override: {FormatEnumName(forced.ForcedKind)}."
            : $"Forced saloon override: {FormatEnumName(forced.ForcedKind)} for suspect {forced.ForcedSuspectId.Value}.",
        DevSaloonOverrideCleared => "Cleared pending saloon override.",
        DevSaloonOverrideConsumed => "Consumed pending saloon override during saloon look-around.",
        _ => e.GetType().Name
    };

    private static string DescribeJourneyProgress(TravelJourneySnapshot snapshot)
        => $"{snapshot.RemainingDays} of {snapshot.ExpectedDays} day(s) remaining, status {FormatEnumName(snapshot.Status)}";

    private static string FormatTravelMode(TravelMode mode)
        => mode == TravelMode.Mounted ? "mounted" : "on foot";

    private static string FormatTownContext(TownActionContext context) => context switch
    {
        TownActionContext.None => "no context",
        TownActionContext.SheriffOffice => "the sheriff office",
        TownActionContext.Saloon => "the saloon",
        TownActionContext.Store => "the store",
        TownActionContext.Stable => "the stable",
        TownActionContext.Jail => "the jail",
        TownActionContext.TelegraphOffice => "the telegraph office",
        TownActionContext.TownSquare => "the town square",
        _ => FormatEnumName(context)
    };

    private static string FormatInvestigationSource(InvestigationSourceKind sourceKind) => sourceKind switch
    {
        InvestigationSourceKind.NoticeBoard => "the notice board",
        InvestigationSourceKind.LocalRecords => "local records",
        InvestigationSourceKind.TelegraphLead => "a telegraph lead",
        InvestigationSourceKind.LocalGossip => "local gossip",
        InvestigationSourceKind.StableLedger => "the stable ledger",
        InvestigationSourceKind.SheriffWarrants => "sheriff warrants",
        InvestigationSourceKind.SaloonLookAround => "a saloon look-around",
        _ => FormatEnumName(sourceKind)
    };

    private static string FormatSaloonPoiKind(SaloonPersonOfInterestKind? kind)
        => kind switch
        {
            SaloonPersonOfInterestKind.Citizen => "a citizen",
            SaloonPersonOfInterestKind.WantedSuspect => "a wanted suspect",
            null => "a person of interest",
            _ => FormatEnumName(kind.Value)
        };

    private static string FormatWarrantDisposition(WarrantDisposition disposition) => disposition switch
    {
        WarrantDisposition.AliveOnly => "alive only",
        WarrantDisposition.DeadOrAlive => "dead or alive",
        _ => FormatEnumName(disposition)
    };

    private static string FormatTrailEventId(JourneyTrailEventId trailEventId) => trailEventId switch
    {
        JourneyTrailEventId.LuckyCoinCache => "lucky coin cache",
        JourneyTrailEventId.LuckyFoodCache => "lucky food cache",
        JourneyTrailEventId.LuckyWaterSeep => "lucky water seep",
        JourneyTrailEventId.BadLuckWashout => "bad luck washout",
        JourneyTrailEventId.BadLuckFoodLoss => "bad luck food loss",
        JourneyTrailEventId.BadLuckDustStorm => "bad luck dust storm",
        JourneyTrailEventId.BadLuckSpookedHorse => "bad luck spooked horse",
        _ => FormatEnumName(trailEventId)
    };

    private static string FormatResolutionState(bool resolved)
        => resolved ? "resolved" : "unresolved";

    private static string FormatEnumName<TEnum>(TEnum value)
        where TEnum : struct, Enum
        => Regex.Replace(value.ToString(), "(\\B[A-Z])", " $1");
}
