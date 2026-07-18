using WildBunch.Application.Projections;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Events;
using WildBunch.Domain.Game;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using DomainInventoryItem = WildBunch.Domain.Inventory.InventoryItem;
using DomainItemKind = WildBunch.Domain.Inventory.ItemKind;

namespace WildBunch.Application.Tests.Projections;

public sealed class FullAuditProjectorTests
{
    [Fact]
    public void FullAuditProjector_TownAndSaloonEvents_ProduceReadableSummaries()
    {
        var projector = new FullAuditProjector();
        var events = new IDomainEvent[]
        {
            new GameStarted
            {
                PlayerName = "Ranger Vale",
                StartingTownId = new TownId("pinecross"),
                StartingTownName = "Pinecross",
                StartingHealth = 100,
                StartingWallet = 25m,
                StartingInventoryItems = new[]
                {
                    new DomainInventoryItem(DomainItemKind.Food, 3),
                    new DomainInventoryItem(DomainItemKind.Canteen, 1)
                },
                GameDifficulty = GameDifficulty.Standard,
                SaltSource = SaltSource.CreateFixed(string.Empty),
                GameEntropy = GameEntropy.Classic
            },
            new StoreItemPurchased
            {
                TownId = new TownId("pinecross"),
                ItemKind = DomainItemKind.Food,
                DisplayName = "Trail Biscuits",
                Quantity = 2,
                UnitPrice = 2m,
                TotalPrice = 4m,
                WalletAfter = 21m
            },
            new TownActionContextEntered
            {
                Context = TownActionContext.Saloon,
                TownId = new TownId("pinecross"),
                Day = 1,
                Turn = 1,
                TimeOfDay = TimeOfDay.Morning,
                PursuitHeat = 0
            },
            new InvestigationPerformed
            {
                SourceKind = InvestigationSourceKind.LocalGossip,
                TownId = new TownId("pinecross"),
                Message = "You ask around for gossip and uncover a public lead."
            },
            new SaloonPersonOfInterestSpotted
            {
                SourceKind = InvestigationSourceKind.SaloonLookAround,
                TownId = new TownId("pinecross"),
                Message = "You spot a shady figure in the saloon.",
                SuspectId = new SuspectId("suspect-1"),
                Descriptor = "a shady figure in a raven-feather pin",
                PersonOfInterestKind = SaloonPersonOfInterestKind.WantedSuspect,
                RecordLog = true
            },
            new SaloonPersonOfInterestConfronted
            {
                Message = "Wrong declaration.",
                TargetSuspectId = null,
                TargetName = "the stranger",
                PersonOfInterestKind = SaloonPersonOfInterestKind.Citizen,
                Outcome = SaloonPersonOfInterestConfrontationOutcome.WrongWantedDeclaration,
                FineAmount = 25m,
                WalletBefore = 100m,
                WalletAfter = 75m,
                IsCitizen = true,
                DeclaredWantedIdentityHandle = "Mira Cline"
            },
            new WantedSuspectConfronted
            {
                TargetSuspectId = new SuspectId("suspect-1"),
                TargetName = "Cole Tanner",
                Disposition = WarrantDisposition.DeadOrAlive,
                Choice = WantedSuspectConfrontationChoice.Surrendered,
                Outcome = WantedSuspectConfrontationOutcome.Surrendered,
                IsAlive = true,
                IsSecured = true,
                Message = "Cole Tanner surrenders."
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
                Turn = 2
            }
        };

        var audit = projector.Project(events);

        Assert.Equal(events.Length, audit.Entries.Count);
        AssertReadableEntries(audit);
        Assert.Contains(audit.Entries, entry => entry.Summary.Contains("saloon", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(audit.Entries, entry => entry.Summary.Contains("bounty", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(audit.Entries, entry => entry.Summary.Contains("lead", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void FullAuditProjector_TravelAndDevEvents_ProduceReadableSummaries()
    {
        var projector = new FullAuditProjector();
        var snapshot = CreateJourneySnapshot();
        var events = new IDomainEvent[]
        {
            new JourneyStarted
            {
                JourneySnapshot = snapshot,
                DiaryMessage = "You set out from Pinecross.",
                PursuitHeat = 0
            },
            new TravelDayAdvanced
            {
                Day = 2,
                JourneySnapshot = snapshot with { Status = JourneyStatus.Active, DaysTravelled = 2, RemainingDays = 1 },
                HealthDelta = -1,
                PursuitHeat = 0,
                DayOutcome = TravelDayOutcome.Ongoing,
                DiaryMessage = "The trail stays quiet.",
                HorseLostMessage = string.Empty,
                AdditionalDiaryMessages = ["The sun rises hot."]
            },
            new TrailEventApplied
            {
                JourneySnapshot = snapshot,
                TrailEventKind = JourneyTrailEventKind.BadLuck,
                TrailEventId = JourneyTrailEventId.BadLuckDustStorm,
                Title = "Dust storm",
                Message = "A dust storm slows the ride.",
                WalletDelta = 0m,
                WalletCash = 25m,
                FoodDelta = -1,
                CanteenChargeDelta = 0,
                HorseHungerDelta = 0,
                HorseThirstDelta = 1,
                HorseExhaustionDelta = 1,
                DelayDays = 1,
                HeatIncrease = 0,
                PursuitHeat = 0,
                TravelModeChangedTo = TravelMode.Foot,
                DiaryMessage = "A dust storm slows the ride.",
                HorseLostMessage = string.Empty
            },
            new JourneyEncounterResolved
            {
                ChoiceId = "run",
                ChoiceLabel = "Run",
                Resolved = true,
                PlayerHealth = 9,
                WalletCash = 18m,
                AmmoSpent = 0,
                StolenItemKind = null,
                StolenItemQuantity = 0,
                PursuitHeat = 0,
                HorseExhaustionDelta = 1,
                ContinuedOnFoot = false,
                JourneySnapshot = snapshot,
                DiaryMessage = "You shake off the rider.",
                DayCompleted = true,
                JourneyCompleted = false,
                AdditionalDiaryMessages = ["The rider falls behind."]
            },
            new JourneyCompleted
            {
                DestinationTownId = new TownId("dustfork"),
                DestinationTownName = "Dust Fork",
                JourneySnapshot = snapshot with { Status = JourneyStatus.Completed, RemainingDays = 0, DaysTravelled = 3 },
                DiaryMessage = "You reach Dust Fork."
            },
            new JourneyArrivalAcknowledged
            {
                JourneySequence = 3,
                JourneySnapshot = snapshot with { Status = JourneyStatus.Completed, RemainingDays = 0, DaysTravelled = 3 },
                DiaryMessage = "Arrival is recorded."
            },
            new DevTravelOverrideForced
            {
                ForcedCategory = TravelDayEncounterCategory.Foe,
                FoeProfile = new JourneyFoeProfile(5, 4, 6m),
                EncounterMessage = "Force a foe encounter."
            },
            new DevTravelOverrideCleared(),
            new DevTravelOverrideConsumed(),
            new DevSaloonOverrideForced
            {
                ForcedKind = DevSaloonPoiKind.Suspect,
                ForcedSuspectId = new SuspectId("suspect-1")
            },
            new DevSaloonOverrideCleared(),
            new DevSaloonOverrideConsumed()
        };

        var audit = projector.Project(events);

        Assert.Equal(events.Length, audit.Entries.Count);
        AssertReadableEntries(audit);
        Assert.Contains(audit.Entries, entry => entry.Summary.Contains("journey", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(audit.Entries, entry => entry.Summary.Contains("travel", StringComparison.OrdinalIgnoreCase));
    }

    private static void AssertReadableEntries(FullAuditProjection audit)
    {
        Assert.All(audit.Entries, entry =>
        {
            Assert.False(string.Equals(entry.EventType, entry.Summary, StringComparison.Ordinal));
            Assert.DoesNotContain(entry.EventType, entry.Summary, StringComparison.Ordinal);
        });
    }

    private static TravelJourneySnapshot CreateJourneySnapshot()
    {
        var routeProfile = new TravelRouteProfile(
            "pinecross-dustfork",
            TrailRisk.Moderate,
            TrailTerrain.OpenRange,
            WaterFeature.Creek,
            6m,
            3m,
            2m,
            Array.Empty<string>());

        return new TravelJourneySnapshot(
            JourneySequence: 3,
            OriginTownId: new TownId("pinecross"),
            DestinationTownId: new TownId("dustfork"),
            OriginTownName: "Pinecross",
            DestinationTownName: "Dust Fork",
            RouteProfile: routeProfile,
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
            Warnings: Array.Empty<string>());
    }
}
