using DomainJourneyEncounter = WildBunch.Domain.Travel.JourneyEncounterState;
using DomainJourneyTrailEvent = WildBunch.Domain.Travel.JourneyTrailEventState;
using DomainTravelDiaryDayState = WildBunch.Domain.Travel.TravelDiaryDayState;
using DomainTravelDiaryEncounterResolutionState = WildBunch.Domain.Travel.TravelDiaryEncounterResolutionState;
using DomainTravelRulesProfile = WildBunch.Domain.Travel.TravelRulesProfile;
using DomainTrailTerrain = WildBunch.Domain.World.TrailTerrain;

namespace WildBunch.Application.Games.Mapping;

public static class TravelDiaryTextRenderer
{
    public static IReadOnlyList<string> RenderEntries(DomainTravelDiaryDayState day, DomainTravelRulesProfile travelRulesProfile)
    {
        var entries = new List<string>();

        var journeyBeat = RenderJourneyBeat(day);
        if (!string.IsNullOrWhiteSpace(journeyBeat))
        {
            entries.Add(journeyBeat);
        }

        var resourceBeat = RenderResourceBeat(day);
        if (!string.IsNullOrWhiteSpace(resourceBeat))
        {
            entries.Add(resourceBeat);
        }

        if (day.Entries.Count > 0)
        {
            entries.AddRange(day.Entries);
        }
        else
        {
            entries.AddRange(RenderFallbackBodyEntries(day, travelRulesProfile));
        }

        entries.Add(RenderStatus(day));

        return DeduplicateInOrder(entries);
    }

    public static string RenderJourneyBeat(DomainTravelDiaryDayState day)
    {
        if (day.PendingEncounter is not null && day.EncounterResolution is null)
        {
            return string.Empty;
        }

        if (day.EncounterResolution is not null)
        {
            return day.EncounterResolution.ChoiceId switch
            {
                "run" => "I put the bad moment behind me and keep moving.",
                "fight" => "I answer hard and keep the trail under my boot.",
                "bribe" => "I pay my way through and keep the dust moving.",
                _ => $"I answer by choosing to {day.EncounterResolution.ChoiceLabel.ToLowerInvariant()}."
            };
        }

        if (day.TrailEvent is not null)
        {
            return day.TrailEvent.Id switch
            {
                WildBunch.Domain.Travel.JourneyTrailEventId.LuckyCoinCache => "I find a little luck when I need it most.",
                WildBunch.Domain.Travel.JourneyTrailEventId.LuckyFoodCache => "I catch the smell of good luck and fresh grub on the wind.",
                WildBunch.Domain.Travel.JourneyTrailEventId.LuckyWaterSeep => "I follow a faint trace of damp earth and find a hidden seep.",
                WildBunch.Domain.Travel.JourneyTrailEventId.BadLuckWashout => "I have to earn every mile when the trail caves in.",
                WildBunch.Domain.Travel.JourneyTrailEventId.BadLuckFoodLoss => "I keep my temper in check while the dust turns mean.",
                WildBunch.Domain.Travel.JourneyTrailEventId.BadLuckSpookedHorse => "My horse flinches at the wrong sound, and I pay for it the rest of the day.",
                _ => day.TrailEvent.Message
            };
        }

        if (day.DayNumber % 6 == 0)
        {
            return "I ride through enough quiet that I can hear leather creak and wind move through the brush.";
        }

        return day.Terrain switch
        {
            DomainTrailTerrain.OpenRange => day.EndingTravelMode == WildBunch.Domain.Travel.TravelMode.Mounted
                ? "I cross open range with the horse moving steady under me."
                : "I walk the open range and let the horizon keep me honest.",
            DomainTrailTerrain.Hills => day.EndingTravelMode == WildBunch.Domain.Travel.TravelMode.Mounted
                ? "I make the horse work for every rise, but the miles still move."
                : "The hills keep asking for another climb, and I keep answering.",
            DomainTrailTerrain.Badlands => "I keep following the road through hard, dry badlands.",
            DomainTrailTerrain.Mountains => "I keep picking my way upward as the trail climbs hard.",
            _ => "I keep moving and let the road tell me what kind of day it is."
        };
    }

    public static string? RenderResourceBeat(DomainTravelDiaryDayState day)
    {
        var pieces = new List<string>();

        if (day.Status == WildBunch.Domain.Travel.JourneyStatus.Completed && day.CanteenChargeDelta > 0)
        {
            pieces.Add("Back in town, I refill the canteen to the brim.");
        }
        else if (!day.RouteWaterSecure)
        {
            if (day.CurrentCanteenCharges == 0)
            {
                pieces.Add("My canteen is dry, so every mile starts to matter.");
            }
            else if (day.CurrentCanteenCharges <= day.CanteenChargesPerDay)
            {
                pieces.Add("I am down to the last stretch of water in the canteen.");
            }
        }

        if (day.CurrentFood == 0)
        {
            pieces.Add("My food is gone, and the trail has turned mean.");
        }
        else if (day.CurrentFood == 1)
        {
            pieces.Add("My food is down to the last meal.");
        }

        if (day.CurrentHorseFeed == 0 && day.HorseStateAfter is not null)
        {
            pieces.Add("My horse feed is gone, so I have to watch the horse more closely.");
        }
        else if (day.CurrentHorseFeed == 1 && day.HorseStateAfter is not null)
        {
            pieces.Add("I am down to the last handful of horse feed.");
        }

        return pieces.Count == 0 ? null : string.Join(" ", pieces);
    }

    public static string RenderStatus(DomainTravelDiaryDayState day)
        => day.Status switch
        {
            WildBunch.Domain.Travel.JourneyStatus.Active => "I keep moving and let the trail stretch ahead.",
            WildBunch.Domain.Travel.JourneyStatus.Interrupted => "I am stuck until I decide how to answer the rider.",
            WildBunch.Domain.Travel.JourneyStatus.Completed => $"I made it to {day.DestinationTownName}.",
            WildBunch.Domain.Travel.JourneyStatus.Failed => "I could not finish the trail before it gave out.",
            _ => "I am still on the trail."
        };

    private static IReadOnlyList<string> RenderFallbackBodyEntries(DomainTravelDiaryDayState day, DomainTravelRulesProfile travelRulesProfile)
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
            entries.Add(RenderEncounterDecision(day.EncounterResolution));
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
        return $"{encounter.Message} {choiceText}";
    }

    private static string RenderEncounterDecision(DomainTravelDiaryEncounterResolutionState resolution)
        => resolution.ChoiceId switch
        {
            "run" => "I decided to run for it.",
            "fight" => "I decided to stand and fight.",
            "bribe" => "I decided to bribe the rider.",
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
                    ? $"I spent {resolution.AmmoSpent} round(s) and lost {Math.Abs(resolution.HealthDelta)} health before forcing the rider off the trail."
                    : $"I fought with my knife and lost {Math.Abs(resolution.HealthDelta)} health before forcing the rider off the trail.");
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
