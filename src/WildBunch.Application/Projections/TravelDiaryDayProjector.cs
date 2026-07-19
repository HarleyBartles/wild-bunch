using WildBunch.Domain.Events;
using WildBunch.Domain.Game;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;

namespace WildBunch.Application.Projections;

/// <summary>
/// Reconstructs TravelDiaryDayState records from the domain event stream.
/// This is a pure function over events — no aggregate mutation, no runtime context.
/// See ADR-0028 and the event sourcing integrity policy.
///
/// The projector tracks running resource state (health, wallet, ammo, heat) across
/// all events, captures day-starting state at day boundaries, and calls
/// TravelDiaryDayFactory.Create to build each diary day with correct deltas.
/// Entries come from the DayEntries field on TravelDayAdvanced and
/// JourneyEncounterResolved — the command path populates this with the full
/// accumulated entries list for the day.
/// </summary>
public sealed class TravelDiaryDayProjector : IDomainEventProjector<TravelDiaryDayProjection>
{
    public TravelDiaryDayProjection Project(IReadOnlyList<IDomainEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);

        // Running resource state (tracked across all events)
        int health = 0;
        decimal wallet = 0m;
        int ammo = 0;
        int heat = 0;

        // Current journey snapshot (from latest journey event)
        TravelJourneySnapshot? currentSnapshot = null;

        // Day tracking state
        TravelDiaryBaselineState? dayStartingState = null;
        JourneyTrailEventState? pendingTrailEvent = null;
        TravelDiaryEncounterResolutionState? encounterResolution = null;
        var diaryDays = new List<TravelDiaryDayState>();

        foreach (var e in events)
        {
            switch (e)
            {
                case GameStarted gs:
                    health = gs.StartingHealth;
                    wallet = gs.StartingWallet;
                    ammo = CountAmmo(gs.StartingInventoryItems);
                    break;

                case StoreItemPurchased sp:
                    wallet = sp.WalletAfter;
                    if (sp.ItemKind is ItemKind.RevolverAmmo or ItemKind.RifleAmmo)
                        ammo += sp.Quantity;
                    break;

                case SheriffTurnInSettled sts:
                    wallet += sts.BountyAmount;
                    break;

                case UnrelatedCriminalTurnInSettled ucts:
                    wallet += ucts.BountyAmount;
                    break;

                case SaloonPersonOfInterestConfronted spoc:
                    if (spoc.WalletAfter is { } walletAfter)
                        wallet = walletAfter;
                    break;

                case JourneyStarted js:
                    currentSnapshot = js.JourneySnapshot;
                    heat = js.PursuitHeat;
                    dayStartingState = CaptureBaseline(currentSnapshot, health, wallet, ammo, heat);
                    pendingTrailEvent = null;
                    encounterResolution = null;
                    // JourneyLoop.Apply(JourneyStarted) clears _travelDiaryDays — the
                    // projector must match so rebuilds from events stay consistent with
                    // the aggregate. Without this, starting a second journey leaves
                    // stale diary days from the first journey in the projection.
                    diaryDays.Clear();
                    break;

                case TrailEventApplied tea:
                    currentSnapshot = tea.JourneySnapshot;
                    wallet = tea.WalletCash;
                    heat = tea.PursuitHeat;
                    pendingTrailEvent = new JourneyTrailEventState(
                        tea.TrailEventId,
                        tea.TrailEventKind,
                        tea.Title,
                        tea.Message,
                        tea.WalletDelta,
                        tea.FoodDelta,
                        tea.CanteenChargeDelta,
                        tea.HorseHungerDelta,
                        tea.HorseThirstDelta,
                        tea.HorseExhaustionDelta,
                        tea.DelayDays,
                        tea.HeatIncrease);
                    break;

                case TravelDayAdvanced tda:
                    health += tda.HealthDelta;
                    heat = tda.PursuitHeat;
                    currentSnapshot = tda.JourneySnapshot;
                    var trailEventForDay = tda.DayOutcome == TravelDayOutcome.Interrupted ? null : pendingTrailEvent;
                    CreateAndStoreDiaryDay(
                        currentSnapshot, dayStartingState, health, wallet, ammo, heat,
                        trailEventForDay, encounterResolution, tda.DayEntries, diaryDays);
                    dayStartingState = CaptureBaseline(currentSnapshot, health, wallet, ammo, heat);
                    pendingTrailEvent = null;
                    encounterResolution = null;
                    break;

                case JourneyEncounterResolved jer:
                    var healthBefore = health;
                    var walletBefore = wallet;
                    var heatBefore = heat;
                    health = jer.PlayerHealth;
                    wallet = jer.WalletCash;
                    ammo -= jer.AmmoSpent;
                    if (jer.StolenItemKind is ItemKind.RevolverAmmo or ItemKind.RifleAmmo && jer.StolenItemQuantity > 0)
                        ammo -= jer.StolenItemQuantity;
                    heat = jer.PursuitHeat;
                    currentSnapshot = jer.JourneySnapshot;
                    encounterResolution = new TravelDiaryEncounterResolutionState(
                        jer.ChoiceId,
                        jer.ChoiceLabel,
                        health - healthBefore,
                        wallet - walletBefore,
                        jer.AmmoSpent,
                        heat - heatBefore,
                        jer.HorseExhaustionDelta,
                        jer.ContinuedOnFoot);

                    if (jer.DayCompleted)
                    {
                        // Day completed: finalize the diary day with DayEntries from the event
                        CreateAndStoreDiaryDay(
                            currentSnapshot, dayStartingState, health, wallet, ammo, heat,
                            pendingTrailEvent, encounterResolution, jer.DayEntries, diaryDays);
                        dayStartingState = CaptureBaseline(currentSnapshot, health, wallet, ammo, heat);
                        pendingTrailEvent = null;
                        encounterResolution = null;
                    }
                    else
                    {
                        // Day not completed: update the last diary day's entries
                        // The command path calls PersistLatestTravelDiaryDay which updates
                        // the last day in-place. We do the same here.
                        if (diaryDays.Count > 0)
                        {
                            var lastIndex = diaryDays.Count - 1;
                            var updatedDay = TravelDiaryDayFactory.Create(
                                currentSnapshot,
                                dayStartingState!,
                                CaptureResources(currentSnapshot, health, wallet, ammo, heat),
                                trailEvent: null,
                                pendingEncounter: currentSnapshot.PendingEncounter,
                                encounterResolution: encounterResolution,
                                entries: jer.DayEntries);
                            diaryDays[lastIndex] = updatedDay;
                        }
                    }
                    break;

                // JourneyCompleted and JourneyArrivalAcknowledged do not create diary days.
                // The last diary day is created by TravelDayAdvanced or JourneyEncounterResolved
                // with DayCompleted=true. JourneyCompleted carries an empty DiaryMessage.
            }
        }

        return new TravelDiaryDayProjection(diaryDays);
    }

    private static int CountAmmo(IReadOnlyList<InventoryItem> items)
    {
        var total = 0;
        foreach (var item in items)
        {
            if (item.Kind is ItemKind.RevolverAmmo or ItemKind.RifleAmmo)
                total += item.Quantity;
        }
        return total;
    }

    private static TravelResourceSnapshot CaptureResources(
        TravelJourneySnapshot snapshot, int health, decimal wallet, int ammo, int heat)
        => new(
            snapshot.HorseState,
            wallet,
            snapshot.AvailableFood,
            snapshot.AvailableHorseFeed,
            snapshot.AvailableCanteenCharges,
            ammo,
            health,
            heat);

    private static TravelDiaryBaselineState CaptureBaseline(
        TravelJourneySnapshot snapshot, int health, decimal wallet, int ammo, int heat)
        => new(
            snapshot.TravelMode,
            snapshot.RemainingRideDayDistance,
            snapshot.RemainingDays,
            snapshot.DelayDays,
            CaptureResources(snapshot, health, wallet, ammo, heat));

    private static void CreateAndStoreDiaryDay(
        TravelJourneySnapshot snapshot,
        TravelDiaryBaselineState? startingState,
        int health, decimal wallet, int ammo, int heat,
        JourneyTrailEventState? trailEvent,
        TravelDiaryEncounterResolutionState? encounterResolution,
        IReadOnlyList<string> entries,
        List<TravelDiaryDayState> diaryDays)
    {
        if (startingState is null)
            return;

        var currentResources = CaptureResources(snapshot, health, wallet, ammo, heat);
        var pendingEncounter = snapshot.PendingEncounter;

        diaryDays.Add(TravelDiaryDayFactory.Create(
            snapshot,
            startingState,
            currentResources,
            trailEvent: trailEvent,
            pendingEncounter: pendingEncounter,
            encounterResolution: encounterResolution,
            entries: entries));
    }
}
