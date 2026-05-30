using WildBunch.Application.Games.Mapping;
using WildBunch.GameContent.Travel;
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

        var rendered = TravelDiaryTextRenderer.RenderDay(day, TravelRulesProfile.Default);

        Assert.NotNull(rendered.JourneyBeat);
        Assert.NotNull(rendered.ResourceBeat);
        Assert.Contains(rendered.JourneyBeat, rendered.Entries);
        Assert.Contains(rendered.ResourceBeat, rendered.Entries);
        Assert.Contains("I found a cache of jerky and trail biscuits and picked up 2 food.", rendered.Entries);
        Assert.True(rendered.SelectedFlavourIds.Count >= 2);
        Assert.All(rendered.SelectedFlavourIds, id => Assert.StartsWith("diary.", id, StringComparison.Ordinal));
        Assert.Equal(rendered.Entries[^1], TravelDiaryTextRenderer.RenderStatus(day));
    }

    [Fact]
    public void RenderEntriesReturnsFirstPersonChoiceTextForPendingEncounter()
    {
        var day = CreateDay(
            pendingEncounter: JourneyEncounterState.CreateChoiceEncounter("foe", "A hard-eyed rider cuts across my path."));

        var rendered = TravelDiaryTextRenderer.RenderDay(day, TravelRulesProfile.Default);

        Assert.Contains(rendered.Entries, entry => entry.Contains("A hard-eyed rider cuts across my path.", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(rendered.Entries, entry => entry.Contains("I can run, fight, or bribe.", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(rendered.Entries, entry => entry.Contains("you ", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void RenderEntriesKeepsResolvedEncounterVoiceInFirstPerson()
    {
        var day = CreateDay(
            status: JourneyStatus.Interrupted,
            encounterResolution: new TravelDiaryEncounterResolutionState("run", "Run", -3, 0m, 0, 0, 1, true));

        var rendered = TravelDiaryTextRenderer.RenderDay(day, TravelRulesProfile.Default);

        Assert.Contains(rendered.Entries, entry => entry.Contains("I decided to run for it.", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(rendered.Entries, entry => entry.Contains("I got away on foot, but it cost me 3 health.", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(rendered.Entries, entry => entry.Contains("My horse came out of it more exhausted.", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(rendered.Entries, entry => entry.Contains("you ", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void RenderEntriesKeepsBribeResolutionVoiceInFirstPerson()
    {
        var day = CreateDay(
            status: JourneyStatus.Interrupted,
            encounterResolution: new TravelDiaryEncounterResolutionState("bribe", "Bribe", 0, -5m, 0, 0, 0, false));

        var rendered = TravelDiaryTextRenderer.RenderDay(day, TravelRulesProfile.Default);

        Assert.NotEmpty(rendered.SelectedFlavourIds);
        Assert.Contains(rendered.Entries, entry => entry.Contains("I decided to bribe the rider.", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(rendered.Entries, entry => entry.Contains("I paid $5.00 to make the problem go away.", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(rendered.Entries, entry => entry.Contains("you ", StringComparison.OrdinalIgnoreCase));
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

        var rendered = TravelDiaryTextRenderer.RenderDay(day, TravelRulesProfile.Default);

        Assert.Contains("A hard mile is a hard mile.", rendered.Entries);
        Assert.Contains("A second distinct note.", rendered.Entries);
        Assert.Equal(rendered.Entries.Count, rendered.Entries.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void RenderDayAvoidsRepeatingFlavourIdsAcrossDaysWhenHistoryIsCarried()
    {
        var seenFlavourIds = new HashSet<string>(StringComparer.Ordinal);
        var firstDay = CreateDay(
            terrain: TrailTerrain.OpenRange,
            routeWaterSecure: true,
            currentCanteenCharges: 2,
            currentFood: 3,
            currentHorseFeed: 2,
            entries: new[] { "I rode a quiet mile." });
        var secondDay = CreateDay(
            terrain: TrailTerrain.OpenRange,
            routeWaterSecure: true,
            currentCanteenCharges: 2,
            currentFood: 3,
            currentHorseFeed: 2,
            entries: new[] { "I rode another quiet mile." });

        var firstRendered = TravelDiaryTextRenderer.RenderDay(firstDay, TravelRulesProfile.Default, seenFlavourIds);
        var secondRendered = TravelDiaryTextRenderer.RenderDay(secondDay, TravelRulesProfile.Default, seenFlavourIds);

        Assert.NotEmpty(firstRendered.SelectedFlavourIds);
        Assert.NotEmpty(secondRendered.SelectedFlavourIds);
        Assert.DoesNotContain(firstRendered.SelectedFlavourIds[0], secondRendered.SelectedFlavourIds);
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
