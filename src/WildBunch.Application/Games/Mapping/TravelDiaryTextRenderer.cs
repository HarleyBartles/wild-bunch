using WildBunch.GameContent.Travel;
using DomainJourneyEncounter = WildBunch.Domain.Travel.JourneyEncounterState;
using DomainJourneyTrailEvent = WildBunch.Domain.Travel.JourneyTrailEventState;
using DomainTravelDiaryDayState = WildBunch.Domain.Travel.TravelDiaryDayState;
using DomainTravelDiaryEncounterResolutionState = WildBunch.Domain.Travel.TravelDiaryEncounterResolutionState;
using DomainTravelRulesProfile = WildBunch.Domain.Travel.TravelRulesProfile;
using DomainTrailTerrain = WildBunch.Domain.World.TrailTerrain;
using DomainWaterFeature = WildBunch.Domain.World.WaterFeature;

namespace WildBunch.Application.Games.Mapping;

public sealed record TravelDiaryRenderedDay(
    string? JourneyBeat,
    string? ResourceBeat,
    IReadOnlyList<string> Entries,
    IReadOnlyList<string> SelectedFlavourIds);

public static class TravelDiaryTextRenderer
{
    public static TravelDiaryRenderedDay RenderDay(
        DomainTravelDiaryDayState day,
        DomainTravelRulesProfile travelRulesProfile,
        ISet<string>? selectedFlavourIds = null)
    {
        ArgumentNullException.ThrowIfNull(day);
        ArgumentNullException.ThrowIfNull(travelRulesProfile);

        selectedFlavourIds ??= new HashSet<string>(StringComparer.Ordinal);
        var daySelectedFlavourIds = new List<string>();

        var journeyBeat = RenderJourneyBeat(day, selectedFlavourIds, daySelectedFlavourIds);
        var resourceBeat = RenderResourceBeat(day, selectedFlavourIds, daySelectedFlavourIds);
        var bodyEntries = day.Entries.Count > 0
            ? day.Entries
            : RenderFallbackBodyEntries(day, travelRulesProfile, selectedFlavourIds, daySelectedFlavourIds);
        var statusEntry = RenderStatus(day, selectedFlavourIds, daySelectedFlavourIds);

        var entries = new List<string>();
        if (!string.IsNullOrWhiteSpace(journeyBeat))
        {
            entries.Add(journeyBeat);
        }

        if (!string.IsNullOrWhiteSpace(resourceBeat))
        {
            entries.Add(resourceBeat);
        }

        if (bodyEntries.Count > 0)
        {
            entries.AddRange(bodyEntries);
        }

        if (!string.IsNullOrWhiteSpace(statusEntry))
        {
            entries.Add(statusEntry);
        }

        return new TravelDiaryRenderedDay(
            journeyBeat,
            resourceBeat,
            DeduplicateInOrder(entries),
            daySelectedFlavourIds);
    }

    public static IReadOnlyList<string> RenderEntries(DomainTravelDiaryDayState day, DomainTravelRulesProfile travelRulesProfile)
        => RenderDay(day, travelRulesProfile).Entries;

    public static string RenderJourneyBeat(DomainTravelDiaryDayState day)
        => RenderJourneyBeat(day, selectedFlavourIds: new HashSet<string>(StringComparer.Ordinal), daySelectedFlavourIds: null);

    public static string? RenderResourceBeat(DomainTravelDiaryDayState day)
        => RenderResourceBeat(day, selectedFlavourIds: new HashSet<string>(StringComparer.Ordinal), daySelectedFlavourIds: null);

    public static string RenderStatus(DomainTravelDiaryDayState day)
        => RenderStatus(day, selectedFlavourIds: new HashSet<string>(StringComparer.Ordinal), daySelectedFlavourIds: null);

    private static string RenderJourneyBeat(
        DomainTravelDiaryDayState day,
        ISet<string> selectedFlavourIds,
        ICollection<string>? daySelectedFlavourIds)
    {
        if (day.PendingEncounter is not null && day.EncounterResolution is null)
        {
            return string.Empty;
        }

        if (day.EncounterResolution is not null)
        {
            return SelectText(
                BuildChoiceOutcomeContext(day, day.EncounterResolution.ChoiceId, beatIndex: 0),
                selectedFlavourIds,
                daySelectedFlavourIds);
        }

        if (day.TrailEvent is not null)
        {
            return day.TrailEvent.Id switch
            {
                WildBunch.Domain.Travel.JourneyTrailEventId.LuckyCoinCache => SelectText(BuildLuckyContext(day, "coin", beatIndex: 0), selectedFlavourIds, daySelectedFlavourIds),
                WildBunch.Domain.Travel.JourneyTrailEventId.LuckyFoodCache => SelectText(BuildLuckyContext(day, "food", beatIndex: 0), selectedFlavourIds, daySelectedFlavourIds),
                WildBunch.Domain.Travel.JourneyTrailEventId.LuckyWaterSeep => SelectText(BuildLuckyContext(day, "water", beatIndex: 0), selectedFlavourIds, daySelectedFlavourIds),
                WildBunch.Domain.Travel.JourneyTrailEventId.BadLuckWashout => SelectText(BuildUnluckyContext(day, "weather", beatIndex: 0), selectedFlavourIds, daySelectedFlavourIds),
                WildBunch.Domain.Travel.JourneyTrailEventId.BadLuckFoodLoss => SelectText(BuildUnluckyContext(day, "food", beatIndex: 0), selectedFlavourIds, daySelectedFlavourIds),
                WildBunch.Domain.Travel.JourneyTrailEventId.BadLuckSpookedHorse => SelectText(BuildHorsePressureContext(day, beatIndex: 0), selectedFlavourIds, daySelectedFlavourIds),
                _ => SelectText(BuildQuietContext(day, beatIndex: 0), selectedFlavourIds, daySelectedFlavourIds)
            };
        }

        if (day.DayNumber % 6 == 0)
        {
            return SelectText(BuildQuietContext(day, beatIndex: 0), selectedFlavourIds, daySelectedFlavourIds);
        }

        return SelectText(BuildOpeningContext(day, beatIndex: 0), selectedFlavourIds, daySelectedFlavourIds);
    }

    private static string? RenderResourceBeat(
        DomainTravelDiaryDayState day,
        ISet<string> selectedFlavourIds,
        ICollection<string>? daySelectedFlavourIds)
    {
        if (day.Status == WildBunch.Domain.Travel.JourneyStatus.Completed && day.CanteenChargeDelta > 0)
        {
            return "Back in town, I refill the canteen to the brim.";
        }

        if (!day.RouteWaterSecure)
        {
            if (day.CurrentCanteenCharges == 0)
            {
                return SelectText(BuildWaterScarcityContext(day, beatIndex: 1), selectedFlavourIds, daySelectedFlavourIds);
            }

            if (day.CurrentCanteenCharges <= day.CanteenChargesPerDay)
            {
                return SelectText(BuildWaterScarcityContext(day, beatIndex: 1), selectedFlavourIds, daySelectedFlavourIds);
            }
        }

        if (day.CurrentFood == 0 || day.CurrentFood == 1)
        {
            return SelectText(BuildResourceScarcityContext(day, beatIndex: 1), selectedFlavourIds, daySelectedFlavourIds);
        }

        if (day.CurrentHorseFeed <= 1 && day.HorseStateAfter is not null)
        {
            return SelectText(BuildHorsePressureContext(day, beatIndex: 1), selectedFlavourIds, daySelectedFlavourIds);
        }

        if (day.RouteWaterSecure)
        {
            return SelectText(BuildWaterReliefContext(day, beatIndex: 1), selectedFlavourIds, daySelectedFlavourIds);
        }

        return SelectText(BuildQuietContext(day, beatIndex: 1), selectedFlavourIds, daySelectedFlavourIds);
    }

    private static string RenderStatus(
        DomainTravelDiaryDayState day,
        ISet<string> selectedFlavourIds,
        ICollection<string>? daySelectedFlavourIds)
        => day.Status switch
        {
            WildBunch.Domain.Travel.JourneyStatus.Completed => SelectText(BuildArrivalContext(day, beatIndex: 2), selectedFlavourIds, daySelectedFlavourIds),
            WildBunch.Domain.Travel.JourneyStatus.Active => "I kept moving and let the trail stretch ahead.",
            WildBunch.Domain.Travel.JourneyStatus.Interrupted => "I was stuck until I decided how to answer the rider.",
            WildBunch.Domain.Travel.JourneyStatus.Failed => "I could not finish the trail before it gave out.",
            _ => "I was still on the trail."
        };

    private static IReadOnlyList<string> RenderFallbackBodyEntries(
        DomainTravelDiaryDayState day,
        DomainTravelRulesProfile travelRulesProfile,
        ISet<string> selectedFlavourIds,
        ICollection<string>? daySelectedFlavourIds)
    {
        var entries = new List<string>();

        if (day.TrailEvent is not null)
        {
            entries.Add(RenderTrailEvent(day.TrailEvent));
        }

        if (day.HorseStateBefore is not null || day.HorseStateAfter is not null)
        {
            var horseEntry = RenderHorseChange(day, travelRulesProfile);
            if (horseEntry.Length != 0)
            {
                entries.Add(horseEntry);
            }
        }

        if (day.PendingEncounter is not null && day.EncounterResolution is null)
        {
            entries.Add(RenderPendingEncounter(day.PendingEncounter));
        }

        if (day.EncounterResolution is not null)
        {
            entries.Add(RenderEncounterDecision(day, selectedFlavourIds, daySelectedFlavourIds));
            var outcomeEntry = RenderEncounterOutcome(day);
            if (outcomeEntry.Length != 0)
            {
                entries.Add(outcomeEntry);
            }
        }

        return entries;
    }

    private static string RenderTrailEvent(DomainJourneyTrailEvent trailEvent)
        => trailEvent.Id switch
        {
            WildBunch.Domain.Travel.JourneyTrailEventId.LuckyCoinCache => $"I found a hidden cache of trail coins and pocketed ${trailEvent.WalletDelta:0.00}.",
            WildBunch.Domain.Travel.JourneyTrailEventId.LuckyFoodCache => $"I found a cache of jerky and trail biscuits and picked up {trailEvent.FoodDelta} food.",
            WildBunch.Domain.Travel.JourneyTrailEventId.LuckyWaterSeep => $"I found a seep under the rocks and topped off my canteen by {trailEvent.CanteenChargeDelta} charge(s).",
            WildBunch.Domain.Travel.JourneyTrailEventId.BadLuckWashout => $"I lost {trailEvent.DelayDays} extra day(s) to a washout detour.",
            WildBunch.Domain.Travel.JourneyTrailEventId.BadLuckFoodLoss => $"I lost {Math.Abs(trailEvent.FoodDelta)} food and {Math.Abs(trailEvent.CanteenChargeDelta)} canteen charge(s) to a dust storm.",
            WildBunch.Domain.Travel.JourneyTrailEventId.BadLuckSpookedHorse => "My horse got spooked by a canyon echo and came out more exhausted.",
            _ => trailEvent.Message
        };

    private static string RenderHorseChange(DomainTravelDiaryDayState day, DomainTravelRulesProfile travelRulesProfile)
    {
        if (day.HorseStateBefore is null || day.HorseStateAfter is null)
        {
            return string.Empty;
        }

        var horseBefore = day.HorseStateBefore;
        var horseAfter = day.HorseStateAfter;
        var lostMountedTravel = day.StartingTravelMode == WildBunch.Domain.Travel.TravelMode.Mounted && day.EndingTravelMode == WildBunch.Domain.Travel.TravelMode.Foot;

        if (horseAfter.IsDeadFor(travelRulesProfile))
        {
            return "My horse died. Hunger most likely, horses do not eat rocks.";
        }

        if (horseAfter.IsLameFor(travelRulesProfile))
        {
            return lostMountedTravel
                ? "My horse went lame and I had to finish the trail on foot."
                : "My horse went lame and could no longer carry me.";
        }

        if (lostMountedTravel)
        {
            return "I had to finish the trail on foot.";
        }

        if (horseBefore == horseAfter)
        {
            return string.Empty;
        }

        return "My horse took the strain and came out of the day worse for it.";
    }

    private static string RenderPendingEncounter(DomainJourneyEncounter encounter)
    {
        var choices = encounter.Choices.Select(choice => choice.Label.ToLowerInvariant()).ToArray();
        var choiceText = choices.Length == 0
            ? "I had no way out yet."
            : choices.Length == 1
                ? $"I could {choices[0]}."
                : choices.Length == 2
                    ? $"I could {choices[0]} or {choices[1]}."
                    : $"I could {string.Join(", ", choices.Take(choices.Length - 1))}, or {choices[^1]}.";
        return $"{encounter.Message} {choiceText}";
    }

    private static string RenderEncounterDecision(
        DomainTravelDiaryDayState day,
        ISet<string> selectedFlavourIds,
        ICollection<string>? daySelectedFlavourIds)
        => day.EncounterResolution!.ChoiceId switch
        {
            "run" => "I decided to run for it.",
            "fight" => "I decided to stand and fight.",
            "bribe" => "I decided to bribe the rider.",
            _ => $"I chose to {day.EncounterResolution.ChoiceLabel.ToLowerInvariant()}."
        };

    private static string RenderEncounterOutcome(DomainTravelDiaryDayState day)
    {
        var resolution = day.EncounterResolution!;
        var pieces = new List<string>();

        switch (resolution.ChoiceId)
        {
            case "run":
                if (resolution.HealthDelta < 0)
                {
                    pieces.Add($"I got away on foot, but it cost me {Math.Abs(resolution.HealthDelta)} health.");
                }
                else if (resolution.ContinuedOnFoot)
                {
                    pieces.Add("I got away, but I had to keep going on foot.");
                }
                else
                {
                    pieces.Add("I got away before he could catch me.");
                }
                break;

            case "fight":
                pieces.Add(resolution.AmmoSpent > 0
                    ? $"I spent {resolution.AmmoSpent} round(s) and lost {Math.Abs(resolution.HealthDelta)} health before forcing the rider off the trail."
                    : $"I fought with my knife and lost {Math.Abs(resolution.HealthDelta)} health before forcing the rider off the trail.");
                break;

            case "bribe":
                if (resolution.HealthDelta < 0)
                {
                    pieces.Add("I tried to bribe the rider, but he took it badly and made me pay for it.");
                }
                else
                {
                    pieces.Add($"I paid ${Math.Abs(resolution.WalletDelta):0.00} to make the problem go away.");
                }
                break;
        }

        if (resolution.HorseExhaustionDelta > 0)
        {
            pieces.Add("My horse came out of it more exhausted.");
        }

        return string.Join(" ", pieces);
    }

    private static string SelectText(
        TravelDiaryFlavourContext context,
        ISet<string> selectedFlavourIds,
        ICollection<string>? daySelectedFlavourIds)
    {
        var beforeCount = selectedFlavourIds.Count;
        var entry = TravelDiaryFlavourCatalog.Select(context, selectedFlavourIds);
        if (selectedFlavourIds.Count > beforeCount)
        {
            daySelectedFlavourIds?.Add(entry.Id);
        }

        return entry.TextTemplate;
    }

    private static TravelDiaryFlavourContext BuildOpeningContext(DomainTravelDiaryDayState day, int beatIndex)
        => BuildBaseContext(day, TravelDiaryFlavourCategory.DayOpening, beatIndex, preferredTags: [DescribeTerrainTag(day.Terrain)]);

    private static TravelDiaryFlavourContext BuildQuietContext(DomainTravelDiaryDayState day, int beatIndex)
        => BuildBaseContext(day, TravelDiaryFlavourCategory.QuietTexture, beatIndex, preferredTags: [DescribeTerrainTag(day.Terrain), "quiet"]);

    private static TravelDiaryFlavourContext BuildLuckyContext(DomainTravelDiaryDayState day, string tag, int beatIndex)
        => BuildBaseContext(day, TravelDiaryFlavourCategory.LuckyEvent, beatIndex, preferredTags: [tag, "lucky"]);

    private static TravelDiaryFlavourContext BuildUnluckyContext(DomainTravelDiaryDayState day, string tag, int beatIndex)
        => BuildBaseContext(day, TravelDiaryFlavourCategory.UnluckyEvent, beatIndex, preferredTags: [tag, "unlucky"]);

    private static TravelDiaryFlavourContext BuildHorsePressureContext(DomainTravelDiaryDayState day, int beatIndex)
        => BuildBaseContext(day, TravelDiaryFlavourCategory.HorsePressure, beatIndex, preferredTags: ["horse", "pressure"]);

    private static TravelDiaryFlavourContext BuildWaterScarcityContext(DomainTravelDiaryDayState day, int beatIndex)
        => BuildBaseContext(day, TravelDiaryFlavourCategory.WaterScarcity, beatIndex, preferredTags: ["water", "dry"]);

    private static TravelDiaryFlavourContext BuildWaterReliefContext(DomainTravelDiaryDayState day, int beatIndex)
        => BuildBaseContext(day, TravelDiaryFlavourCategory.WaterRelief, beatIndex, preferredTags: ["water", "relief"]);

    private static TravelDiaryFlavourContext BuildResourceScarcityContext(DomainTravelDiaryDayState day, int beatIndex)
        => BuildBaseContext(day, TravelDiaryFlavourCategory.ResourceScarcity, beatIndex, preferredTags: ["resource", "food"]);

    private static TravelDiaryFlavourContext BuildChoiceOutcomeContext(DomainTravelDiaryDayState day, string choiceId, int beatIndex)
        => BuildBaseContext(day, TravelDiaryFlavourCategory.ChoiceOutcome, beatIndex, choiceId: choiceId, preferredTags: [choiceId]);

    private static TravelDiaryFlavourContext BuildArrivalContext(DomainTravelDiaryDayState day, int beatIndex)
        => BuildBaseContext(day, TravelDiaryFlavourCategory.ArrivalCompletion, beatIndex, preferredTags: ["arrival", "completion"]);

    private static TravelDiaryFlavourContext BuildBaseContext(
        DomainTravelDiaryDayState day,
        TravelDiaryFlavourCategory category,
        int beatIndex,
        string? choiceId = null,
        IReadOnlyCollection<string>? preferredTags = null)
        => new(
            category,
            JourneyKey: $"{day.OriginTownName}->{day.DestinationTownName}|{day.StartingTravelMode}|{day.StartingRideDayDistance:0.##}|{day.StartingDaysRemaining}",
            DayNumber: day.DayNumber,
            BeatIndex: beatIndex,
            Terrain: day.Terrain,
            WaterFeature: day.RouteWaterSecure ? DomainWaterFeature.Creek : DomainWaterFeature.None,
            TravelMode: day.EndingTravelMode,
            HasHorse: day.HorseStateAfter is not null,
            RouteWaterSecure: day.RouteWaterSecure,
            CurrentFood: day.CurrentFood,
            CurrentHorseFeed: day.CurrentHorseFeed,
            CurrentCanteenCharges: day.CurrentCanteenCharges,
            CanteenChargesPerDay: day.CanteenChargesPerDay,
            TrailEventId: day.TrailEvent?.Id.ToString(),
            EncounterKind: day.PendingEncounter?.Kind,
            ChoiceId: choiceId,
            JourneyStatus: day.Status,
            PreferredTags: preferredTags);

    private static string DescribeTerrainTag(DomainTrailTerrain terrain)
        => terrain switch
        {
            DomainTrailTerrain.OpenRange => "open-range",
            DomainTrailTerrain.Hills => "hills",
            DomainTrailTerrain.Badlands => "badlands",
            DomainTrailTerrain.Mountains => "mountains",
            _ => "trail"
        };

    private static IReadOnlyList<string> DeduplicateInOrder(IEnumerable<string> entries)
    {
        var deduplicated = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var entry in entries)
        {
            if (seen.Add(entry))
            {
                deduplicated.Add(entry);
            }
        }

        return deduplicated;
    }
}
