using WildBunch.Application.Games.Mapping;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;

namespace WildBunch.Application.Tests;

public sealed class TravelDiaryTextRendererTests
{
    [Fact]
    public void RenderEntriesReturnsFirstPersonChoiceTextForPendingEncounter()
    {
        var day = CreateDay(
            pendingEncounter: JourneyEncounterState.CreateChoiceEncounter("foe", "A hard-eyed rider cuts across my path."));

        var entries = TravelDiaryTextRenderer.RenderEntries(day, TravelRulesProfile.Default);

        Assert.Single(entries);
        Assert.Contains("A hard-eyed rider cuts across my path.", entries[0], StringComparison.OrdinalIgnoreCase);
        Assert.Contains("I can run, fight, or bribe.", entries[0], StringComparison.OrdinalIgnoreCase);
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

    private static TravelDiaryDayState CreateDay(
        JourneyStatus status = JourneyStatus.Active,
        JourneyEncounterState? pendingEncounter = null,
        TravelDiaryEncounterResolutionState? encounterResolution = null,
        JourneyTrailEventState? trailEvent = null)
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
            null,
            null,
            trailEvent,
            pendingEncounter,
            encounterResolution,
            null,
            null,
            null,
            Entries: Array.Empty<string>(),
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
            CurrentFood: 3,
            CurrentHorseFeed: 0,
            CurrentCanteenCharges: 2,
            CurrentAmmo: 0,
            CurrentHeat: 0,
            Warnings: Array.Empty<string>());
}
