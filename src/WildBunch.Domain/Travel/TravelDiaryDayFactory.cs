using WildBunch.Domain.Game;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.World;

namespace WildBunch.Domain.Travel;

internal sealed record TravelResourceSnapshot(
    HorseTravelState? HorseState,
    decimal Wallet,
    int Food,
    int HorseFeed,
    int CanteenCharges,
    int Ammo,
    int Health,
    int Heat);

internal sealed record TravelDiaryBaselineState(
    TravelMode StartingTravelMode,
    decimal StartingRideDayDistance,
    int StartingDaysRemaining,
    int StartingDelayDays,
    TravelResourceSnapshot StartingResources);

internal static class TravelResourceSnapshotFactory
{
    public static TravelResourceSnapshot Capture(Player player, PursuitState pursuitState)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(pursuitState);

        return new TravelResourceSnapshot(
            player.Inventory.GetHorseState(),
            player.Wallet.Cash,
            player.Inventory.GetQuantity(ItemKind.Food),
            player.Inventory.GetQuantity(ItemKind.HorseFeed),
            player.Inventory.GetCanteenState()?.Charges ?? 0,
            player.Inventory.GetQuantity(ItemKind.RevolverAmmo) + player.Inventory.GetQuantity(ItemKind.RifleAmmo),
            player.Health,
            pursuitState.Heat);
    }
}

internal static class TravelDiaryDayFactory
{
    public static TravelDiaryDayState Create(
        TravelJourneySnapshot journeySnapshot,
        TravelDiaryBaselineState startingState,
        TravelResourceSnapshot currentResources,
        JourneyTrailEventState? trailEvent = null,
        JourneyEncounterState? pendingEncounter = null,
        TravelDiaryEncounterResolutionState? encounterResolution = null,
        IReadOnlyList<string>? entries = null)
    {
        ArgumentNullException.ThrowIfNull(journeySnapshot);
        ArgumentNullException.ThrowIfNull(startingState);
        ArgumentNullException.ThrowIfNull(currentResources);

        var openingNarration = startingState.StartingDaysRemaining == journeySnapshot.ExpectedDays ? journeySnapshot.OpeningNarration : null;
        var journeyBeat = BuildJourneyBeat(journeySnapshot, trailEvent, pendingEncounter, encounterResolution);
        var resourceBeat = BuildResourceBeat(journeySnapshot, startingState.StartingResources, currentResources);
        var diaryEntries = BuildDiaryEntries(
            journeySnapshot,
            startingState.StartingTravelMode,
            trailEvent,
            pendingEncounter,
            encounterResolution,
            journeyBeat,
            resourceBeat,
            entries);

        return new TravelDiaryDayState(
            journeySnapshot.DaysTravelled,
            journeySnapshot.OriginTownName,
            journeySnapshot.DestinationTownName,
            startingState.StartingTravelMode,
            journeySnapshot.TravelMode,
            journeySnapshot.Status,
            startingState.StartingRideDayDistance,
            journeySnapshot.RemainingRideDayDistance,
            startingState.StartingDaysRemaining,
            journeySnapshot.RemainingDays,
            startingState.StartingResources.HorseState,
            journeySnapshot.HorseState,
            trailEvent,
            pendingEncounter ?? journeySnapshot.PendingEncounter,
            encounterResolution,
            openingNarration,
            journeyBeat,
            resourceBeat,
            Entries: diaryEntries,
            HealthDelta: currentResources.Health - startingState.StartingResources.Health,
            WalletDelta: currentResources.Wallet - startingState.StartingResources.Wallet,
            FoodDelta: currentResources.Food - startingState.StartingResources.Food,
            HorseFeedDelta: currentResources.HorseFeed - startingState.StartingResources.HorseFeed,
            CanteenChargeDelta: currentResources.CanteenCharges - startingState.StartingResources.CanteenCharges,
            AmmoSpent: startingState.StartingResources.Ammo - currentResources.Ammo,
            HorseHungerDelta: (currentResources.HorseState?.Hunger ?? 0) - (startingState.StartingResources.HorseState?.Hunger ?? 0),
            HorseThirstDelta: (currentResources.HorseState?.Thirst ?? 0) - (startingState.StartingResources.HorseState?.Thirst ?? 0),
            HorseExhaustionDelta: (currentResources.HorseState?.Exhaustion ?? 0) - (startingState.StartingResources.HorseState?.Exhaustion ?? 0),
            DelayDays: journeySnapshot.DelayDays - startingState.StartingDelayDays,
            HeatIncrease: currentResources.Heat - startingState.StartingResources.Heat,
            CurrentHealth: currentResources.Health,
            CurrentWallet: currentResources.Wallet,
            CurrentFood: currentResources.Food,
            CurrentHorseFeed: currentResources.HorseFeed,
            CurrentCanteenCharges: currentResources.CanteenCharges,
            CurrentAmmo: currentResources.Ammo,
            CurrentHeat: currentResources.Heat,
            Warnings: journeySnapshot.Warnings);
    }

    private static IReadOnlyList<string> BuildDiaryEntries(
        TravelJourneySnapshot journeySnapshot,
        TravelMode startingTravelMode,
        JourneyTrailEventState? trailEvent,
        JourneyEncounterState? pendingEncounter,
        TravelDiaryEncounterResolutionState? encounterResolution,
        string? journeyBeat,
        string? resourceBeat,
        IReadOnlyList<string>? entries)
    {
        var diaryEntries = new List<string>();

        if (!string.IsNullOrWhiteSpace(journeyBeat))
        {
            diaryEntries.Add(journeyBeat!);
        }

        if (!string.IsNullOrWhiteSpace(resourceBeat))
        {
            diaryEntries.Add(resourceBeat!);
        }

        var effectivePendingEncounter = pendingEncounter ?? journeySnapshot.PendingEncounter;

        if (entries is not null && entries.Count > 0)
        {
            diaryEntries.AddRange(entries);
        }
        else
        {
            if (trailEvent is not null)
            {
                diaryEntries.Add(trailEvent.Message);
            }

            if (startingTravelMode == TravelMode.Mounted && journeySnapshot.TravelMode == TravelMode.Foot)
            {
                diaryEntries.Add("I had to finish the trail on foot after the horse went lame.");
            }

            if (effectivePendingEncounter is not null && encounterResolution is null)
            {
                diaryEntries.Add(effectivePendingEncounter.Message);
            }

            if (encounterResolution is not null)
            {
                diaryEntries.Add(encounterResolution.ChoiceId switch
                {
                    "run" => "I decided to run for it.",
                    "fight" => "I decided to stand and fight.",
                    "bribe" => "I decided to bribe my way through.",
                    _ => $"I chose to {encounterResolution.ChoiceLabel.ToLowerInvariant()}."
                });
            }
        }

        diaryEntries.Add(RenderStatus(journeySnapshot));
        return diaryEntries;
    }

    private static string BuildJourneyBeat(
        TravelJourneySnapshot journeySnapshot,
        JourneyTrailEventState? trailEvent,
        JourneyEncounterState? pendingEncounter,
        TravelDiaryEncounterResolutionState? encounterResolution)
    {
        if (pendingEncounter is not null && encounterResolution is null)
        {
            return string.Empty;
        }

        if (encounterResolution is not null)
        {
            return encounterResolution.ChoiceId switch
            {
                "run" => "I put the bad moment behind me and keep moving.",
                "fight" => "I answer hard and keep the trail under my boot.",
                "bribe" => "I pay my way through and keep the dust moving.",
                _ => $"I answer by choosing to {encounterResolution.ChoiceLabel.ToLowerInvariant()}."
            };
        }

        if (trailEvent is not null)
        {
            return trailEvent.Id switch
            {
                JourneyTrailEventId.LuckyCoinCache => "I find a little luck when I need it most.",
                JourneyTrailEventId.LuckyFoodCache => "I catch the smell of good luck and fresh grub on the wind.",
                JourneyTrailEventId.LuckyWaterSeep => "I follow a faint trace of damp earth and find a hidden seep.",
                JourneyTrailEventId.BadLuckWashout => "I have to earn every mile when the trail caves in.",
                JourneyTrailEventId.BadLuckFoodLoss => "I keep my temper in check while the dust turns mean.",
                JourneyTrailEventId.BadLuckSpookedHorse => "My horse flinches at the wrong sound, and I pay for it the rest of the day.",
                _ => trailEvent.Message
            };
        }

        if (journeySnapshot.DaysTravelled % 6 == 0)
        {
            return "I ride through enough quiet that I can hear leather creak and wind move through the brush.";
        }

        return journeySnapshot.RouteProfile.Terrain switch
        {
            TrailTerrain.OpenRange => journeySnapshot.TravelMode == TravelMode.Mounted
                ? "I cross open range with the horse moving steady under me."
                : "I walk the open range and let the horizon keep me honest.",
            TrailTerrain.Hills => journeySnapshot.TravelMode == TravelMode.Mounted
                ? "I make the horse work for every rise, but the miles still move."
                : "The hills keep asking for another climb, and I keep answering.",
            TrailTerrain.Badlands => "I keep following the road through hard, dry badlands.",
            TrailTerrain.Mountains => "I keep picking my way upward as the trail climbs hard.",
            _ => "I keep moving and let the road tell me what kind of day it is."
        };
    }

    private static string? BuildResourceBeat(TravelJourneySnapshot journeySnapshot, TravelResourceSnapshot startingResources, TravelResourceSnapshot currentResources)
    {
        var pieces = new List<string>();

        if (journeySnapshot.Status == JourneyStatus.Completed && currentResources.CanteenCharges > startingResources.CanteenCharges)
        {
            pieces.Add("Back in town, I refill the canteen to the brim.");
        }
        else if (!JourneyUpkeepRules.HasRouteWater(journeySnapshot.RouteProfile.WaterFeature))
        {
            if (currentResources.CanteenCharges == 0)
            {
                pieces.Add("My canteen is dry, so every mile starts to matter.");
            }
            else if (currentResources.CanteenCharges <= journeySnapshot.CanteenChargesPerDay)
            {
                pieces.Add("I am down to the last stretch of water in the canteen.");
            }
        }

        if (currentResources.Food == 0)
        {
            pieces.Add("My food is gone, and the trail has turned mean.");
        }
        else if (currentResources.Food == 1)
        {
            pieces.Add("My food is down to the last meal.");
        }

        if (currentResources.HorseFeed == 0 && journeySnapshot.HorseState is not null)
        {
            pieces.Add("My horse feed is gone, so I have to watch the horse more closely.");
        }
        else if (currentResources.HorseFeed == 1 && journeySnapshot.HorseState is not null)
        {
            pieces.Add("I am down to the last handful of horse feed.");
        }

        return pieces.Count == 0 ? null : string.Join(" ", pieces);
    }

    private static string RenderStatus(TravelJourneySnapshot journeySnapshot)
        => journeySnapshot.Status switch
        {
            JourneyStatus.Active => "I keep moving and let the trail stretch ahead.",
            JourneyStatus.Interrupted => "I am stuck until I decide how to answer the rider.",
            JourneyStatus.Completed => $"I made it to {journeySnapshot.DestinationTownName}.",
            JourneyStatus.Failed => "I could not finish the trail before it gave out.",
            _ => "I am still on the trail."
        };
}
