using WildBunch.Application.Projections;
using WildBunch.Domain.Events;
using WildBunch.Domain.Game;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;

namespace WildBunch.Application.Tests.Projections;

/// <summary>
/// Tests that prove the event stream is the authoritative source of game history
/// and that the legacy GameLogEntry table is a projection-legacy per ADR-0028.
/// The FullAuditProjector derives the same information from events that the
/// legacy log entries table stored directly.
/// </summary>
public sealed class GameLogEntryLegacyProjectionTests
{
    [Fact]
    public void FullAuditProjection_SupersedesLegacyLogEntries_ForEventSourcedFlows()
    {
        // The event stream carries the same information that legacy log entries stored.
        // The FullAuditProjector derives it from events, proving the legacy table
        // is a projection-legacy, not the source of truth.
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
                    new InventoryItem(ItemKind.Food, 1)
                },
                Difficulty = TravelDifficulty.Normal,
                TravelRandomness = TravelRandomnessState.CreateDeterministic("test"),
                Entropy = AdventureRandomnessPolicy.Standard
            },
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

        var audit = projector.Project(events);

        // The audit projection has the same number of entries as events
        Assert.Equal(2, audit.Entries.Count);

        // Each entry has a summary that carries the same information as a legacy log entry
        Assert.Contains("Ranger Vale", audit.Entries[0].Summary);
        Assert.Contains("Pinecross", audit.Entries[0].Summary);
        Assert.Contains("Trail Biscuits", audit.Entries[1].Summary);
        Assert.Contains("2", audit.Entries[1].Summary);
    }

    [Fact]
    public void DiaryProjection_SupersedesLegacyLogEntries_ForPlayerFacingDiary()
    {
        // The diary projection derives the player-facing diary from events,
        // superseding the legacy log entries for diary display.
        var projector = new DiaryProjector();
        var events = new IDomainEvent[]
        {
            new GameStarted
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
            },
            new StoreItemPurchased
            {
                TownId = new TownId("pinecross"),
                ItemKind = ItemKind.Food,
                DisplayName = "Trail Biscuits",
                Quantity = 1,
                UnitPrice = 2m,
                TotalPrice = 2m,
                WalletAfter = 23m
            }
        };

        var diary = projector.Project(events);

        // The diary projection has entries for each event
        Assert.Equal(2, diary.Entries.Count);
        Assert.Contains("Pinecross", diary.Entries[0].Summary);
        Assert.Contains("store", diary.Entries[1].Summary, StringComparison.OrdinalIgnoreCase);
    }
}
