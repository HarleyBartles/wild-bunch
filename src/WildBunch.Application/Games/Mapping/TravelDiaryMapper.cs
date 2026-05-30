using WildBunch.Application.Games.Models;
using DomainHorseTravelState = WildBunch.Domain.Inventory.HorseTravelState;
using DomainJourneyEncounter = WildBunch.Domain.Travel.JourneyEncounterState;
using DomainJourneyTrailEvent = WildBunch.Domain.Travel.JourneyTrailEventState;
using DomainTravelDiaryDayState = WildBunch.Domain.Travel.TravelDiaryDayState;
using DomainTravelDiaryEncounterResolutionState = WildBunch.Domain.Travel.TravelDiaryEncounterResolutionState;
using DomainTravelRulesProfile = WildBunch.Domain.Travel.TravelRulesProfile;

namespace WildBunch.Application.Games.Mapping;

public static class TravelDiaryMapper
{
    public static TravelDiaryDto? ToDto(IReadOnlyList<DomainTravelDiaryDayState> days, DomainTravelRulesProfile? travelRulesProfile = null)
    {
        travelRulesProfile ??= DomainTravelRulesProfile.Default;

        if (days.Count == 0)
        {
            return null;
        }

        return new TravelDiaryDto(days.Select(day => ToDto(day, travelRulesProfile)).ToArray());
    }

    private static TravelDiaryDayDto ToDto(DomainTravelDiaryDayState day, DomainTravelRulesProfile travelRulesProfile)
        => new(
            day.DayNumber,
            day.OriginTownName,
            day.DestinationTownName,
            day.StartingTravelMode,
            day.EndingTravelMode,
            day.Status,
            day.StartingRideDayDistance,
            day.RemainingRideDayDistance,
            day.StartingDaysRemaining,
            day.RemainingDays,
            TravelMapper.ToHorseDto(day.HorseStateBefore, travelRulesProfile),
            TravelMapper.ToHorseDto(day.HorseStateAfter, travelRulesProfile),
            day.TrailEvent is null ? null : TravelMapper.ToDto(day.TrailEvent),
            day.PendingEncounter is null ? null : TravelMapper.ToDto(day.PendingEncounter),
            day.EncounterResolution is null ? null : ToDto(day.EncounterResolution),
            day.OpeningNarration,
            day.JourneyBeat,
            day.ResourceBeat,
            day.HealthDelta,
            day.WalletDelta,
            day.FoodDelta,
            day.HorseFeedDelta,
            day.CanteenChargeDelta,
            day.AmmoSpent,
            day.HorseHungerDelta,
            day.HorseThirstDelta,
            day.HorseExhaustionDelta,
            day.DelayDays,
            day.HeatIncrease,
            day.Entries.Count == 0 ? RenderEntries(day, travelRulesProfile) : day.Entries,
            day.Warnings);

    private static TravelDiaryEncounterResolutionDto ToDto(DomainTravelDiaryEncounterResolutionState resolution)
        => new(
            resolution.ChoiceId,
            resolution.ChoiceLabel,
            resolution.HealthDelta,
            resolution.WalletDelta,
            resolution.AmmoSpent,
            resolution.HeatIncrease,
            resolution.HorseExhaustionDelta,
            resolution.ContinuedOnFoot);

    private static IReadOnlyList<string> RenderEntries(DomainTravelDiaryDayState day, DomainTravelRulesProfile travelRulesProfile)
    {
        var entries = new List<string>
        {
            RenderJourneyBeat(day)
        };

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

        if (!string.IsNullOrWhiteSpace(day.ResourceBeat))
        {
            entries.Add(day.ResourceBeat!);
        }

        if (day.PendingEncounter is not null && day.EncounterResolution is null)
        {
            entries.Add(RenderPendingEncounter(day.PendingEncounter));
            return entries;
        }

        if (day.EncounterResolution is not null)
        {
            entries.Add(RenderEncounterDecision(day.EncounterResolution));
            var outcomeEntry = RenderEncounterOutcome(day);
            if (outcomeEntry.Length != 0)
            {
                entries.Add(outcomeEntry);
            }
        }

        entries.Add(RenderStatus(day));
        return entries;
    }

    private static string RenderJourneyBeat(DomainTravelDiaryDayState day)
        => string.IsNullOrWhiteSpace(day.JourneyBeat)
            ? "I keep moving and let the road tell me what kind of day it is."
            : day.JourneyBeat;

    private static string RenderTrailEvent(DomainJourneyTrailEvent trailEvent)
        => trailEvent.Id switch
        {
            WildBunch.Domain.Travel.JourneyTrailEventId.LuckyCoinCache => $"I found a hidden cache of trail coins and pocketed ${trailEvent.WalletDelta:0.00}.",
            WildBunch.Domain.Travel.JourneyTrailEventId.LuckyFoodCache => $"I found a cache of jerky and trail biscuits and picked up {trailEvent.FoodDelta} food.",
            WildBunch.Domain.Travel.JourneyTrailEventId.LuckyWaterSeep => $"I found a seep under the rocks and topped off my canteen by {trailEvent.CanteenChargeDelta} charge(s).",
            WildBunch.Domain.Travel.JourneyTrailEventId.BadLuckWashout => $"A washout forced a detour and cost me {trailEvent.DelayDays} extra day(s).",
            WildBunch.Domain.Travel.JourneyTrailEventId.BadLuckFoodLoss => $"A dust storm stripped away {Math.Abs(trailEvent.FoodDelta)} food and {Math.Abs(trailEvent.CanteenChargeDelta)} canteen charge(s).",
            WildBunch.Domain.Travel.JourneyTrailEventId.BadLuckSpookedHorse => "A canyon echo spooked my horse and left him more exhausted.",
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
            return "My horse died. Hunger most likely, horses don't eat rocks.";
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
            ? "I have no way out yet."
            : choices.Length == 1
                ? $"I can {choices[0]}."
                : choices.Length == 2
                    ? $"I can {choices[0]} or {choices[1]}."
                    : $"I can {string.Join(", ", choices.Take(choices.Length - 1))}, or {choices[^1]}.";
        return $"A hard-eyed trail rider stepped out from the brush and blocked my way. {choiceText}";
    }

    private static string RenderEncounterDecision(DomainTravelDiaryEncounterResolutionState resolution)
        => resolution.ChoiceId switch
        {
            "run" => "I decided to run for it.",
            "fight" => "I decided to stand and fight.",
            "bribe" => "I decided to bribe him.",
            _ => $"I chose to {resolution.ChoiceLabel.ToLowerInvariant()}."
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
                    ? $"I spent {resolution.AmmoSpent} round(s) and forced the rider off the trail."
                    : "I fought with my knife and forced the rider off the trail.");
                if (resolution.HealthDelta < 0)
                {
                    pieces.Add($"It cost me {Math.Abs(resolution.HealthDelta)} health.");
                }
                break;

            case "bribe":
                pieces.Add($"I paid ${Math.Abs(resolution.WalletDelta):0.00} to make the problem go away.");
                break;
        }

        if (resolution.HorseExhaustionDelta > 0)
        {
            pieces.Add("My horse came out of it more exhausted.");
        }

        return string.Join(" ", pieces);
    }

    private static string RenderStatus(DomainTravelDiaryDayState day)
        => day.Status switch
        {
            WildBunch.Domain.Travel.JourneyStatus.Active => "The trail keeps stretching ahead.",
            WildBunch.Domain.Travel.JourneyStatus.Interrupted => "I am stuck until I decide how to answer the rider.",
            WildBunch.Domain.Travel.JourneyStatus.Completed => $"I made it to {day.DestinationTownName}.",
            WildBunch.Domain.Travel.JourneyStatus.Failed => "The trail gave out before I could finish it.",
            _ => "I am still on the trail."
        };
}
