using WildBunch.Application.Games.Mapping;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;

namespace WildBunch.Application.Tests;

public sealed class TravelDiaryTextRendererTests
{
    [Fact]
    public void RenderEntriesWrapsRawEntriesWithStructuredDiaryProse()
    {
        var day = CreateDay(
            terrain: TrailTerrain.OpenRange,
            routeWaterSecure: false,
            canteenChargesPerDay: 2,
            currentCanteenCharges: 1,
            currentFood: 2,
            currentHorseFeed: 0,
            horseStateAfter: HorseTravelState.Healthy,
            entries: new[] { "I found a cache of jerky and trail biscuits and picked up 2 food." });

        var entries = TravelDiaryTextRenderer.RenderEntries(day, TravelRulesProfile.Default);

        Assert.Equal("I cross open range with the horse moving steady under me.", entries[0]);
        Assert.Equal("I am down to the last stretch of water in the canteen. My horse feed is gone, so I have to watch the horse more closely.", entries[1]);
        Assert.Contains("I found a cache of jerky and trail biscuits and picked up 2 food.", entries);
        Assert.Equal("I keep moving and let the trail stretch ahead.", entries[^1]);
    }

    [Fact]
    public void RenderEntriesReturnsFirstPersonChoiceTextForPendingEncounter()
    {
        var day = CreateDay(
            pendingEncounter: JourneyEncounterState.CreateChoiceEncounter("foe", "A hard-eyed rider cuts across my path."));

        var entries = TravelDiaryTextRenderer.RenderEntries(day, TravelRulesProfile.Default);

        Assert.Equal(2, entries.Count);
        Assert.Contains("A hard-eyed rider cuts across my path.", entries[0], StringComparison.OrdinalIgnoreCase);
        Assert.Contains("I can run, fight, or bribe.", entries[0], StringComparison.OrdinalIgnoreCase);
        Assert.Equal("I keep moving and let the trail stretch ahead.", entries[^1]);
        Assert.DoesNotContain("you ", entries[0], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RenderEntriesKeepsResolvedEncounterVoiceInFirstPerson()
    {
        var day = CreateDay(
            status: JourneyStatus.Interrupted,
            encounterResolution: new TravelDiaryEncounterResolutionState("run", "Run", -3, 0m, 0, 0, 1, true));

        var entries = TravelDiaryTextRenderer.RenderEntries(day, TravelRulesProfile.Default);

        Assert.Contains(entries, entry => entry.Contains("I decided to run for it.", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(entries, entry => entry.Contains("I got away on foot, but it cost me 3 health.", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(entries, entry => entry.Contains("My horse came out of it more exhausted.", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(entries, entry => entry.Contains("you ", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void RenderEntriesKeepsBribeResolutionVoiceInFirstPerson()
    {
        var day = CreateDay(
            status: JourneyStatus.Interrupted,
            encounterResolution: new TravelDiaryEncounterResolutionState("bribe", "Bribe", 0, -5m, 0, 0, 0, false));

        var entries = TravelDiaryTextRenderer.RenderEntries(day, TravelRulesProfile.Default);

        Assert.Contains(entries, entry => entry.Contains("I decided to bribe the rider.", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(entries, entry => entry.Contains("I pay my way through and keep the dust moving.", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(entries, entry => entry.Contains("I paid $5.00 to make the problem go away.", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(entries, entry => entry.Contains("you ", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void RenderEntriesDeduplicatesExactStringsWithinADayAndPreservesOrder()
    {
        var day = CreateDay(
            entries: new[]
            {
                "A hard mile is a hard mile.",
                "A hard mile is a hard mile.",
                "A second distinct note."
            });

        var entries = TravelDiaryTextRenderer.RenderEntries(day, TravelRulesProfile.Default);

        Assert.Equal(
            new[]
            {
                "I cross open range with the horse moving steady under me.",
                "A hard mile is a hard mile.",
                "A second distinct note.",
                "I keep moving and let the trail stretch ahead."
            },
            entries);
    }

    private static TravelDiaryDayState CreateDay(
        JourneyStatus status = JourneyStatus.Active,
        JourneyEncounterState? pendingEncounter = null,
        TravelDiaryEncounterResolutionState? encounterResolution = null,
        JourneyTrailEventState? trailEvent = null,
        HorseTravelState? horseStateBefore = null,
        HorseTravelState? horseStateAfter = null,
        TrailTerrain terrain = TrailTerrain.OpenRange,
        bool routeWaterSecure = true,
        int canteenChargesPerDay = 0,
        int currentCanteenCharges = 2,
        int currentFood = 3,
        int currentHorseFeed = 0,
        IReadOnlyList<string>? entries = null)
        => new(
            1,
            "Pinecross",
            "Dry Fork",
            TravelMode.Mounted,
            TravelMode.Mounted,
            status,
            3m,
            3m,
            4,
            4,
            horseStateBefore,
            horseStateAfter,
            trailEvent,
            pendingEncounter,
            encounterResolution,
            null,
            null,
            null,
            Entries: entries ?? Array.Empty<string>(),
            HealthDelta: 0,
            WalletDelta: 0m,
            FoodDelta: 0,
            HorseFeedDelta: 0,
            CanteenChargeDelta: 0,
            AmmoSpent: 0,
            HorseHungerDelta: 0,
            HorseThirstDelta: 0,
            HorseExhaustionDelta: 0,
            DelayDays: 0,
            HeatIncrease: 0,
            CurrentHealth: 1000,
            CurrentWallet: 25m,
            CurrentFood: currentFood,
            CurrentHorseFeed: currentHorseFeed,
            CurrentCanteenCharges: currentCanteenCharges,
            CurrentAmmo: 0,
            CurrentHeat: 0,
            Warnings: Array.Empty<string>())
        {
            Terrain = terrain,
            RouteWaterSecure = routeWaterSecure,
            CanteenChargesPerDay = canteenChargesPerDay
        };
}
