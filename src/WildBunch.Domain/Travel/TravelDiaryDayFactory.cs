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
        var diaryEntries = entries ?? Array.Empty<string>();

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
            null,
            null,
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
            Warnings: journeySnapshot.Warnings)
        {
            Terrain = journeySnapshot.RouteProfile.Terrain,
            RouteWaterSecure = journeySnapshot.RouteProfile.WaterFeature is WaterFeature.Creek or WaterFeature.River or WaterFeature.Spring,
            CanteenChargesPerDay = journeySnapshot.CanteenChargesPerDay
        };
    }
}
