using WildBunch.Application.Games.Mapping;
using WildBunch.Application.Games.Models;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using WildBunch.Domain.Game;

namespace WildBunch.Application.Tests.Mappers;

public sealed class TravelDiaryMapperTests
{
    [Fact]
    public void ToDtoPreservesRecordedLatestJourneyEntriesWithSharedDayAndMessageValues()
    {
        var history = new[]
        {
            new GameLogEntry(GameLogEntryKind.Travel, "The trail goes quiet.", 4, 0, 1),
            new GameLogEntry(GameLogEntryKind.Travel, "The trail goes quiet.", 4, 0, 2),
            new GameLogEntry(GameLogEntryKind.Travel, "I reach Dust Fork.", 5, 0, 2)
        };

        var dto = TravelDiaryMapper.ToDto(Array.Empty<TravelDiaryDayState>(), history);

        Assert.NotNull(dto);
        Assert.Equal(new[] { "The trail goes quiet.", "I reach Dust Fork." }, dto!.JourneyEntries.Select(entry => entry.Message));
        Assert.All(dto.JourneyEntries, entry => Assert.Equal(GameLogEntryKind.Travel, entry.Kind));
        Assert.DoesNotContain(dto.JourneyEntries, entry => entry.Day == 3);
    }

    [Fact]
    public void ToDtoPreservesStructuredDayFactsWithoutCreatingNarration()
    {
        var day = new TravelDiaryDayState(
            1,
            "Pinecross",
            "Dry Fork",
            TravelMode.Mounted,
            TravelMode.Mounted,
            JourneyStatus.Active,
            3m,
            3m,
            4,
            4,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            Entries: new[] { "I found a cache of jerky and trail biscuits and picked up 2 food." },
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
            Warnings: Array.Empty<string>())
        {
            Terrain = TrailTerrain.OpenRange,
            RouteWaterSecure = true,
            CanteenChargesPerDay = 0
        };

        var dto = TravelDiaryMapper.ToDto(new[] { day });

        Assert.NotNull(dto);
        var mappedDay = Assert.Single(dto!.Days);
        Assert.Equal(3, mappedDay.CurrentFood);
        Assert.Equal(1000, mappedDay.CurrentHealth);
    }
}
