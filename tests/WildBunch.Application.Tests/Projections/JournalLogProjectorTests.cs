using WildBunch.Application.Projections;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Events;
using WildBunch.Domain.Game;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;

namespace WildBunch.Application.Tests.Projections;

public sealed class JournalLogProjectorTests
{
    private static GameStarted GameStartedEvent() => new()
    {
        PlayerName = "Ranger Vale",
        StartingTownId = new TownId("pinecross"),
        StartingTownName = "Pinecross",
        StartingHealth = 100,
        StartingWallet = 25m,
        StartingInventoryItems = Array.Empty<InventoryItem>(),
        Difficulty = TravelDifficulty.Normal,
        TravelRandomness = TravelRandomnessState.CreateDeterministic("test"),
        Entropy = AdventureRandomnessPolicy.Standard
    };

    [Fact]
    public void GameStarted_ProducesSingleOpeningEntryWithLegacyText()
    {
        var projector = new JournalLogProjector();
        var log = projector.Project(new IDomainEvent[] { GameStartedEvent() });

        Assert.Single(log);
        Assert.Equal(GameLogEntryKind.Opening, log[0].Kind);
        Assert.Equal("The hunt begins in Pinecross.", log[0].Message);
        Assert.Equal(1, log[0].Day);
        Assert.Equal(0, log[0].Turn);
    }

    [Fact]
    public void StoreItemPurchased_ProducesPurchaseEntry_MatchingLegacyCommandPath()
    {
        var projector = new JournalLogProjector();
        var events = new IDomainEvent[]
        {
            GameStartedEvent(),
            new StoreItemPurchased
            {
                TownId = new TownId("pinecross"),
                ItemKind = ItemKind.Food,
                DisplayName = "Trail Biscuits",
                Quantity = 2,
                UnitPrice = 2m,
                TotalPrice = 4m,
                WalletAfter = 21m
            }
        };
        var log = projector.Project(events);

        // Opening + purchase entry
        Assert.Equal(2, log.Count);
        Assert.Equal(GameLogEntryKind.Purchase, log[1].Kind);
        Assert.Equal("Purchased 2 Trail Biscuits for $4.00.", log[1].Message);
        Assert.Equal(1, log[1].Day);
        Assert.Equal(0, log[1].Turn);
    }

    [Fact]
    public void StoreItemPurchased_SingleQuantity_UsesDisplayNameWithoutQuantityPrefix()
    {
        var projector = new JournalLogProjector();
        var events = new IDomainEvent[]
        {
            GameStartedEvent(),
            new StoreItemPurchased
            {
                TownId = new TownId("pinecross"),
                ItemKind = ItemKind.Canteen,
                DisplayName = "Canteen",
                Quantity = 1,
                UnitPrice = 3m,
                TotalPrice = 3m,
                WalletAfter = 22m
            }
        };
        var log = projector.Project(events);

        Assert.Equal(2, log.Count);
        Assert.Equal(GameLogEntryKind.Purchase, log[1].Kind);
        Assert.Equal("Purchased Canteen for $3.00.", log[1].Message);
    }

    [Fact]
    public void SheriffTurnInSettled_ProducesNoLogEntry_MatchingLegacyApply()
    {
        var projector = new JournalLogProjector();
        var events = new IDomainEvent[]
        {
            GameStartedEvent(),
            new SheriffTurnInSettled
            {
                TargetSuspectId = new SuspectId("suspect-1"),
                TargetName = "Jesse Roe",
                Disposition = WarrantDisposition.DeadOrAlive,
                IsAlive = true,
                BountyAmount = 50m,
                Message = "You turn Jesse Roe in for the bounty.",
                Day = 1,
                Turn = 0
            }
        };
        var log = projector.Project(events);

        Assert.Single(log); // opening only; sheriff turn-in adds no legacy log entry
    }

    [Fact]
    public void InvestigationPerformed_ProducesCaseUpdateEntryWithTrackedDayTurn()
    {
        var projector = new JournalLogProjector();
        var events = new IDomainEvent[]
        {
            GameStartedEvent(),
            new TownActionContextEntered { Day = 1, Turn = 1, Context = TownActionContext.SheriffOffice, TownId = new TownId("pinecross"), TimeOfDay = TimeOfDay.Afternoon, PursuitHeat = 0 },
            new InvestigationPerformed
            {
                SourceKind = InvestigationSourceKind.SheriffWarrants,
                TownId = new TownId("pinecross"),
                Message = "You check the wanted posters."
            }
        };
        var log = projector.Project(events);

        Assert.Equal(2, log.Count);
        Assert.Equal(GameLogEntryKind.CaseUpdate, log[1].Kind);
        Assert.Equal("You check the wanted posters.", log[1].Message);
        Assert.Equal(1, log[1].Day);
        Assert.Equal(1, log[1].Turn);
    }

    [Fact]
    public void TravelDayAdvanced_ProducesTravelEntriesWithAbsoluteDayAndTurnZero()
    {
        var projector = new JournalLogProjector();
        var events = new IDomainEvent[]
        {
            GameStartedEvent(),
            new JourneyStarted { JourneySnapshot = null!, DiaryMessage = "You set out.", PursuitHeat = 0 },
            new TravelDayAdvanced
            {
                Day = 2,
                JourneySnapshot = null!,
                HealthDelta = 0,
                PursuitHeat = 0,
                DayOutcome = TravelDayOutcome.Ongoing,
                AdditionalDiaryMessages = new[] { "A quiet morning." },
                DiaryMessage = "You reach the next leg.",
                HorseLostMessage = string.Empty
            }
        };
        var log = projector.Project(events);

        // GameStarted opening (day 1, turn 0) + JourneyStarted travel entry (day 1, turn 0)
        // + TravelDayAdvanced additional narration (day 2, turn 0) + diary message (day 2, turn 0).
        // The event list includes GameStarted, so the opening entry is log[0]; the travel
        // entries follow. Count is 4, not 3.
        Assert.Equal(4, log.Count);
        Assert.Equal(GameLogEntryKind.Opening, log[0].Kind);
        Assert.Equal("The hunt begins in Pinecross.", log[0].Message);
        Assert.Equal(1, log[0].Day);
        Assert.Equal(0, log[0].Turn);
        Assert.Equal(GameLogEntryKind.Travel, log[1].Kind);
        Assert.Equal("You set out.", log[1].Message);
        Assert.Equal(1, log[1].Day);
        Assert.Equal(0, log[1].Turn);
        Assert.Equal(GameLogEntryKind.Travel, log[2].Kind);
        Assert.Equal("A quiet morning.", log[2].Message);
        Assert.Equal(2, log[2].Day);
        Assert.Equal(0, log[2].Turn);
        Assert.Equal(GameLogEntryKind.Travel, log[3].Kind);
        Assert.Equal("You reach the next leg.", log[3].Message);
        Assert.Equal(2, log[3].Day);
        Assert.Equal(0, log[3].Turn);
    }

    [Fact]
    public void EmptyMessagesAndHorseLostMessage_AreSkippedOrEmittedExactlyAsLegacy()
    {
        var projector = new JournalLogProjector();
        var events = new IDomainEvent[]
        {
            GameStartedEvent(),
            new TravelDayAdvanced
            {
                Day = 2,
                JourneySnapshot = null!,
                HealthDelta = 0,
                PursuitHeat = 0,
                DayOutcome = TravelDayOutcome.Ongoing,
                AdditionalDiaryMessages = Array.Empty<string>(),
                DiaryMessage = "",
                HorseLostMessage = "Your horse went lame."
            }
        };
        var log = projector.Project(events);

        // opening + horse-lost only; empty DiaryMessage is skipped
        Assert.Equal(2, log.Count);
        Assert.Equal(GameLogEntryKind.Travel, log[1].Kind);
        Assert.Equal("Your horse went lame.", log[1].Message);
        Assert.Equal(2, log[1].Day);
    }
}
