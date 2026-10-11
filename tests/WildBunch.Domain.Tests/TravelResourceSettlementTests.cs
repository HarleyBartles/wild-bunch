using WildBunch.Domain.Events;
using WildBunch.Domain.Game;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using Xunit;

namespace WildBunch.Domain.Tests;

public sealed class TravelResourceSettlementTests
{
    [Theory]
    [InlineData(ItemKind.Food)]
    [InlineData(ItemKind.HorseFeed)]
    public void BribeRetaliationRecordsTheResourceActuallyStolen(ItemKind stolenKind)
    {
        var (session, encounter) = StartInterruptedJourney(
            stolenKind,
            startingFood: stolenKind == ItemKind.Food ? 3 : 0);
        var startingQuantity = session.Journey!.FoodRemaining;
        if (stolenKind == ItemKind.HorseFeed)
        {
            startingQuantity = session.Journey.HorseFeedRemaining;
        }

        var setupEvents = TravelTestFactory.RecaptureSetupEventsForReplay(session);
        var forcedRoll = FindRetaliationRoll(session, encounter, stolenKind);

        session.ResolveJourneyEncounter("bribe", bulletSpend: null, bribeAmount: 1m, forcedRoll: forcedRoll);

        var resolution = Assert.Single(session.UncommittedEvents.OfType<JourneyEncounterResolved>());
        var expectedQuantity = startingQuantity - 1;
        Assert.Equal(stolenKind, resolution.StolenItemKind);
        Assert.Equal(1, resolution.StolenItemQuantity);
        Assert.Equal(expectedQuantity, Quantity(resolution.JourneySnapshot, stolenKind));
        Assert.Equal(expectedQuantity, session.Player.Inventory.GetQuantity(stolenKind));
        Assert.Equal(expectedQuantity, Quantity(session.Journey.ToSnapshot(session.TravelRules), stolenKind));

        var history = setupEvents.Concat(session.CommittedEvents).Concat(session.UncommittedEvents).ToArray();
        var replayed = GameSession.RehydrateFromEvents(session.Id, session.World, history);
        Assert.Equal(expectedQuantity, replayed.Player.Inventory.GetQuantity(stolenKind));
        Assert.Equal(expectedQuantity, Quantity(replayed.Journey!.ToSnapshot(replayed.TravelRules), stolenKind));
    }

    [Fact]
    public void TrailLossesRecordOnlyAvailableResourcesAndNarrateTheAppliedAmounts()
    {
        var (session, encounter) = StartInterruptedJourney(ItemKind.Food);
        var startingCanteenCharges = session.Journey!.AvailableCanteenCharges;
        Assert.True(startingCanteenCharges > 0);
        var firstLoss = JourneyTrailEventState.CreateBadLuck(
            JourneyTrailEventId.BadLuckFoodLoss,
            "Dust-choked outfit",
            "A dust storm strips away food and canteen water.",
            foodDelta: -2,
            canteenChargeDelta: -3);
        var secondLoss = JourneyTrailEventState.CreateBadLuck(
            JourneyTrailEventId.BadLuckFoodLoss,
            "Another dust storm",
            "Another dust storm strips away food and canteen water.",
            foodDelta: -1,
            canteenChargeDelta: -1);
        session.Journey!.SetCurrentDayPlan(new TravelDayPlanState(
            DayNumber: 1,
            Encounters:
            [
                FoeDayEncounter(encounter),
                TrailDayEncounter(1, firstLoss),
                TrailDayEncounter(2, secondLoss)
            ],
            CurrentEncounterIndex: 0,
            IsComplete: false));
        session.Journey.MarkInterrupted(encounter);

        var setupEvents = TravelTestFactory.RecaptureSetupEventsForReplay(session);
        var forcedRoll = FindRetaliationRoll(session, encounter, ItemKind.Food);

        session.ResolveJourneyEncounter("bribe", bulletSpend: null, bribeAmount: 1m, forcedRoll: forcedRoll);

        var trailEvents = session.UncommittedEvents.OfType<TrailEventApplied>().ToArray();
        Assert.Equal(2, trailEvents.Length);
        Assert.Equal([-1, 0], trailEvents.Select(e => e.FoodDelta));
        Assert.Equal([-startingCanteenCharges, 0], trailEvents.Select(e => e.CanteenChargeDelta));
        Assert.Equal(0, session.Journey!.FoodRemaining);
        Assert.Equal(0, session.Journey.AvailableCanteenCharges);
        Assert.Equal(0, session.Player.Inventory.GetQuantity(ItemKind.Food));
        Assert.Equal(0, session.Player.GetCanteenState()!.Charges);
        Assert.Contains("1 food", trailEvents[0].Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains($"{startingCanteenCharges} canteen", trailEvents[0].Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("2 food", trailEvents[0].Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("1 food", trailEvents[0].DiaryMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("no remaining food", trailEvents[1].DiaryMessage, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("1 food", trailEvents[1].DiaryMessage, StringComparison.OrdinalIgnoreCase);

        var history = setupEvents.Concat(session.CommittedEvents).Concat(session.UncommittedEvents).ToArray();
        var replayed = GameSession.RehydrateFromEvents(session.Id, session.World, history);
        Assert.Equal(0, replayed.Journey!.FoodRemaining);
        Assert.Equal(0, replayed.Journey.AvailableCanteenCharges);
        Assert.Equal(0, replayed.Player.Inventory.GetQuantity(ItemKind.Food));
        Assert.Equal(0, replayed.Player.GetCanteenState()!.Charges);
    }

    private static (GameSession Session, JourneyEncounterState Encounter) StartInterruptedJourney(
        ItemKind? onlyStolenKind,
        int startingFood = 3)
    {
        var (session, _) = TravelTestFactory.CreateHighRiskJourney();
        var food = session.Player.Inventory.GetQuantity(ItemKind.Food);
        if (food > startingFood)
        {
            session.Player.RemoveQuantity(ItemKind.Food, food - startingFood);
        }

        if (onlyStolenKind == ItemKind.HorseFeed)
        {
            session.Player.RemoveQuantity(ItemKind.Horse, 1);
            session.Player.AddItem(ItemKind.HorseFeed, 1);
        }

        var preview = new TravelResolver().PreviewJourney(
            session.World,
            session.Player.CurrentTownId!.Value,
            new TownId("dryfork"),
            session.Player.Inventory,
            session.TravelRules).Preview!;
        session.StartJourney(preview);
        session.AdvanceJourneyDay();
        var encounter = JourneyEncounterState.CreateFoe(
            "A hard-eyed rider blocks the trail.",
            new JourneyFoeProfile(8, 8, 10m));
        session.Journey!.SetCurrentDayPlan(new TravelDayPlanState(
            DayNumber: 1,
            Encounters: [FoeDayEncounter(encounter)],
            CurrentEncounterIndex: 0,
            IsComplete: false));
        session.Journey.MarkInterrupted(encounter);
        session.MarkEventsCommitted();
        return (session, encounter);
    }

    private static TravelDayEncounterState FoeDayEncounter(JourneyEncounterState encounter)
        => new(0, TravelDayEncounterCategory.Foe, "Rider", encounter.Message, null, encounter, null);

    private static TravelDayEncounterState TrailDayEncounter(int index, JourneyTrailEventState trailEvent)
        => new(index, TravelDayEncounterCategory.Unlucky, trailEvent.Title, trailEvent.Message, trailEvent, null, null);

    private static ulong FindRetaliationRoll(GameSession session, JourneyEncounterState encounter, ItemKind stolenKind)
    {
        for (ulong roll = 0; roll < 10_000; roll++)
        {
            var plan = JourneyEncounterResolutionEngine.ResolveBribe(
                encounter,
                session.Player.Wallet.Cash,
                session.TravelRules,
                requestedBribe: 1m,
                session.Journey!.FoodRemaining,
                session.Journey.HorseFeedRemaining,
                session.Player.Inventory.GetQuantity(ItemKind.RevolverAmmo),
                roll);
            if (!plan.Resolved && plan.StolenItemKind == stolenKind && plan.StolenItemQuantity > 0)
            {
                return roll;
            }
        }

        throw new InvalidOperationException($"Could not find a retaliation roll that steals {stolenKind}.");
    }

    private static int Quantity(TravelJourneySnapshot snapshot, ItemKind itemKind)
        => itemKind switch
        {
            ItemKind.Food => snapshot.AvailableFood,
            ItemKind.HorseFeed => snapshot.AvailableHorseFeed,
            _ => throw new ArgumentOutOfRangeException(nameof(itemKind), itemKind, "Unexpected travel resource.")
        };
}
