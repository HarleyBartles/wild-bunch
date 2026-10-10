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
        GameDifficulty = GameDifficulty.Standard,
        SaltSource = SaltSource.CreateFixed("test"),
        GameEntropy = GameEntropy.Classic
    };

    private static TravelJourneySnapshot JourneySnapshot(int sequence) => new(
        JourneySequence: sequence,
        OriginTownId: new TownId("pinecross"),
        DestinationTownId: new TownId("dustfork"),
        OriginTownName: "Pinecross",
        DestinationTownName: "Dust Fork",
        RouteProfile: new TravelRouteProfile("pinecross-dustfork", TrailRisk.Moderate, TrailTerrain.OpenRange, WaterFeature.Creek, 6m, 3m, 2m, []),
        TravelMode: TravelMode.Mounted,
        Status: JourneyStatus.Active,
        MountedTravelAvailable: true,
        WaterSecure: true,
        RideDayDistance: 6m,
        RemainingRideDayDistance: 3m,
        ExpectedDays: 3,
        RemainingDays: 2,
        CanteenChargesPerDay: 1,
        RequiredCanteenCharges: 2,
        AvailableCanteenCharges: 4,
        CanteenReserveCharges: 1,
        DelayMarginDays: 0,
        DelayRisk: false,
        RequiredFood: 2,
        AvailableFood: 5,
        RequiredHorseFeed: 1,
        AvailableHorseFeed: 3,
        HorseState: null,
        OpeningNarration: "The long road east waits.",
        DaysTravelled: 1,
        DelayDays: 0,
        CurrentDayPlan: null,
        PendingEncounter: null,
        Warnings: []);

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
    public void SheriffTurnInSettled_AddsPlayerMessageAtRecordedDayAndTurn()
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
                Day = 4,
                Turn = 2
            }
        };
        var log = projector.Project(events);

        Assert.Equal(2, log.Count);
        Assert.Equal(GameLogEntryKind.CaseUpdate, log[1].Kind);
        Assert.Equal("You turn Jesse Roe in for the bounty.", log[1].Message);
        Assert.Equal(4, log[1].Day);
        Assert.Equal(2, log[1].Turn);
    }

    [Fact]
    public void SaloonPersonOfInterestConfronted_CitizenFineAndRelease_IsRecordedAtEnteredActionTime()
    {
        var projector = new JournalLogProjector();
        var events = new IDomainEvent[]
        {
            GameStartedEvent(),
            new TownActionContextEntered
            {
                Context = TownActionContext.Saloon,
                TownId = new TownId("pinecross"),
                Day = 3,
                Turn = 2,
                TimeOfDay = TimeOfDay.Evening,
                PursuitHeat = 0
            },
            new SaloonPersonOfInterestConfronted
            {
                Message = "The butcher comes quietly. The sheriff releases him and fines you $5.00.",
                TargetName = "the butcher",
                PersonOfInterestKind = SaloonPersonOfInterestKind.Citizen,
                Outcome = SaloonPersonOfInterestConfrontationOutcome.WrongWantedDeclaration,
                IsCitizen = true,
                FineAmount = 5m
            }
        };

        var log = projector.Project(events);

        Assert.Equal(2, log.Count);
        Assert.Equal(GameLogEntryKind.CaseUpdate, log[1].Kind);
        Assert.Equal("The butcher comes quietly. The sheriff releases him and fines you $5.00.", log[1].Message);
        Assert.Equal(3, log[1].Day);
        Assert.Equal(2, log[1].Turn);
    }

    [Fact]
    public void SaloonPersonOfInterestConfronted_RejectedAttempt_IsRecordedAtEnteredActionTime()
    {
        var projector = new JournalLogProjector();
        var events = new IDomainEvent[]
        {
            GameStartedEvent(),
            new TownActionContextEntered
            {
                Context = TownActionContext.Saloon,
                TownId = new TownId("pinecross"),
                Day = 3,
                Turn = 2,
                TimeOfDay = TimeOfDay.Evening,
                PursuitHeat = 0
            },
            new SaloonPersonOfInterestConfronted
            {
                Message = "There is no wanted notice for the rancher.",
                TargetName = "the rancher",
                PersonOfInterestKind = SaloonPersonOfInterestKind.WantedSuspect,
                Outcome = SaloonPersonOfInterestConfrontationOutcome.Rejected
            }
        };

        var log = projector.Project(events);

        Assert.Equal(2, log.Count);
        Assert.Equal("There is no wanted notice for the rancher.", log[1].Message);
        Assert.Equal(3, log[1].Day);
        Assert.Equal(2, log[1].Turn);
    }

    [Fact]
    public void SaloonPersonOfInterestConfronted_WhenWantedOutcomeHasDetailedEvents_DoesNotDuplicateSummary()
    {
        var projector = new JournalLogProjector();
        var events = new IDomainEvent[]
        {
            GameStartedEvent(),
            new WantedSuspectConfronted
            {
                TargetSuspectId = new SuspectId("suspect-1"),
                TargetName = "Cole Tanner",
                Disposition = WarrantDisposition.DeadOrAlive,
                Choice = WantedSuspectConfrontationChoice.Surrendered,
                Outcome = WantedSuspectConfrontationOutcome.Surrendered,
                IsAlive = true,
                IsSecured = true,
                Message = "Cole Tanner gives up without a fight."
            },
            new SheriffTurnInSettled
            {
                TargetSuspectId = new SuspectId("suspect-1"),
                TargetName = "Cole Tanner",
                Disposition = WarrantDisposition.DeadOrAlive,
                IsAlive = true,
                BountyAmount = 50m,
                Message = "The sheriff pays you $50.00.",
                Day = 1,
                Turn = 1
            },
            new SaloonPersonOfInterestConfronted
            {
                Message = "Cole Tanner gives up without a fight. The sheriff pays you $50.00.",
                TargetSuspectId = new SuspectId("suspect-1"),
                TargetName = "Cole Tanner",
                PersonOfInterestKind = SaloonPersonOfInterestKind.WantedSuspect,
                Outcome = SaloonPersonOfInterestConfrontationOutcome.Surrendered,
                IsCitizen = false
            }
        };

        var log = projector.Project(events);

        Assert.Equal(3, log.Count);
        Assert.Contains(log, entry => entry.Message == "Cole Tanner gives up without a fight.");
        Assert.Contains(log, entry => entry.Message == "The sheriff pays you $50.00.");
        Assert.DoesNotContain(log, entry => entry.Message.Contains("Cole Tanner gives up without a fight. The sheriff pays"));
    }

    [Fact]
    public void SaloonPersonOfInterestSpotted_WhenRecordLogIsTrue_AddsCaseUpdate()
    {
        var projector = new JournalLogProjector();
        var events = new IDomainEvent[]
        {
            GameStartedEvent(),
            new TownActionContextEntered
            {
                Context = TownActionContext.Saloon,
                TownId = new TownId("pinecross"),
                Day = 2,
                Turn = 1,
                TimeOfDay = TimeOfDay.Afternoon,
                PursuitHeat = 0
            },
            new SaloonPersonOfInterestSpotted
            {
                SourceKind = InvestigationSourceKind.SaloonLookAround,
                TownId = new TownId("pinecross"),
                Message = "You spot a shady figure in the saloon.",
                RecordLog = true
            }
        };

        var log = projector.Project(events);

        Assert.Equal(2, log.Count);
        Assert.Equal(GameLogEntryKind.CaseUpdate, log[1].Kind);
        Assert.Equal("You spot a shady figure in the saloon.", log[1].Message);
        Assert.Equal(2, log[1].Day);
        Assert.Equal(1, log[1].Turn);
    }

    [Fact]
    public void SaloonPersonOfInterestSpotted_WhenRecordLogIsFalse_DoesNotAddJournalEntry()
    {
        var projector = new JournalLogProjector();
        var events = new IDomainEvent[]
        {
            GameStartedEvent(),
            new TownActionContextEntered
            {
                Context = TownActionContext.Saloon,
                TownId = new TownId("pinecross"),
                Day = 2,
                Turn = 1,
                TimeOfDay = TimeOfDay.Afternoon,
                PursuitHeat = 0
            },
            new SaloonPersonOfInterestSpotted
            {
                SourceKind = InvestigationSourceKind.SaloonLookAround,
                TownId = new TownId("pinecross"),
                Message = "You spot a townsfolk in the saloon.",
                RecordLog = false
            }
        };

        var log = projector.Project(events);

        Assert.Single(log);
    }

    [Fact]
    public void WantedSuspectConfronted_AddsRecordedPlayerMessage()
    {
        var projector = new JournalLogProjector();
        var events = new IDomainEvent[]
        {
            GameStartedEvent(),
            new WantedSuspectConfronted
            {
                TargetSuspectId = new SuspectId("suspect-1"),
                TargetName = "Cole Tanner",
                Disposition = WarrantDisposition.DeadOrAlive,
                Choice = WantedSuspectConfrontationChoice.Surrendered,
                Outcome = WantedSuspectConfrontationOutcome.Surrendered,
                IsAlive = true,
                IsSecured = true,
                Message = "Cole Tanner gives up without a fight."
            }
        };

        var log = projector.Project(events);

        Assert.Equal(2, log.Count);
        Assert.Equal(GameLogEntryKind.CaseUpdate, log[1].Kind);
        Assert.Equal("Cole Tanner gives up without a fight.", log[1].Message);
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
            new JourneyStarted { JourneySnapshot = JourneySnapshot(1), DiaryMessage = "You set out.", PursuitHeat = 0 },
            new TravelDayAdvanced
            {
                Day = 2,
                JourneySnapshot = JourneySnapshot(1),
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
    public void TravelEntries_KeepTheirRecordedJourneySequenceAcrossMultipleJourneys()
    {
        var firstJourney = JourneySnapshot(1);
        var secondJourney = JourneySnapshot(2);
        var events = new IDomainEvent[]
        {
            GameStartedEvent(),
            new JourneyStarted { JourneySnapshot = firstJourney, DiaryMessage = "The first road begins.", PursuitHeat = 0 },
            new TravelDayAdvanced
            {
                Day = 2,
                JourneySnapshot = firstJourney,
                HealthDelta = 0,
                PursuitHeat = 0,
                DayOutcome = TravelDayOutcome.Ongoing,
                DiaryMessage = "I cross the first ridge.",
                HorseLostMessage = string.Empty
            },
            new JourneyStarted { JourneySnapshot = secondJourney, DiaryMessage = "The second road begins.", PursuitHeat = 0 },
            new TravelDayAdvanced
            {
                Day = 4,
                JourneySnapshot = secondJourney,
                HealthDelta = 0,
                PursuitHeat = 0,
                DayOutcome = TravelDayOutcome.Ongoing,
                DiaryMessage = "I cross the second ridge.",
                HorseLostMessage = string.Empty
            }
        };

        var entries = new JournalLogProjector().Project(events);

        Assert.Null(entries.Single(entry => entry.Kind == GameLogEntryKind.Opening).JourneySequence);
        Assert.Equal(
            new[] { ("The first road begins.", (int?)1), ("I cross the first ridge.", (int?)1), ("The second road begins.", (int?)2), ("I cross the second ridge.", (int?)2) },
            entries.Where(entry => entry.Kind == GameLogEntryKind.Travel)
                .Select(entry => (entry.Message, entry.JourneySequence)));
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
                JourneySnapshot = JourneySnapshot(1),
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
