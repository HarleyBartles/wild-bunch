using WildBunch.Application.Projections;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Events;
using WildBunch.Domain.Game;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;

namespace WildBunch.Application.Tests.Projections;

/// <summary>
/// Behavior tests for event-derived journal entries, including the purchase
/// entry produced for a mixed event stream.
/// </summary>
public sealed class GameLogEntryLegacyProjectionTests
{
    /// <summary>
    /// JournalLogProjector produces the expected entries for a mixed event
    /// stream, including a Purchase entry for StoreItemPurchased. The persisted
    /// journal read path is exercised against PostgreSQL by
    /// ReadRepositoriesProjectComposedSessionAndJournalViews.
    /// </summary>
    [Fact]
    public void JournalLogProjector_ReproducesLegacyLogSequence_IncludesPurchaseEntries()
    {
        var projector = new JournalLogProjector();
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
                GameDifficulty = GameDifficulty.Standard,
                SaltSource = SaltSource.CreateFixed("test"),
                GameEntropy = GameEntropy.Classic
            },
            new TownActionContextEntered { Day = 1, Turn = 1, Context = TownActionContext.SheriffOffice, TownId = new TownId("pinecross"), TimeOfDay = TimeOfDay.Afternoon, PursuitHeat = 0 },
            new InvestigationPerformed { SourceKind = InvestigationSourceKind.SheriffWarrants, TownId = new TownId("pinecross"), Message = "A public lead is noted." },
            new StoreItemPurchased { TownId = new TownId("pinecross"), ItemKind = ItemKind.Food, DisplayName = "Trail Biscuits", Quantity = 1, UnitPrice = 2m, TotalPrice = 2m, WalletAfter = 23m }
        };

        var log = projector.Project(events);

        // Opening + case update + purchase entry.
        Assert.Equal(3, log.Count);
        Assert.Equal(GameLogEntryKind.Opening, log[0].Kind);
        Assert.Equal("The hunt begins in Pinecross.", log[0].Message);
        Assert.Equal(GameLogEntryKind.CaseUpdate, log[1].Kind);
        Assert.Equal("A public lead is noted.", log[1].Message);
        Assert.Equal(1, log[1].Day);
        Assert.Equal(1, log[1].Turn);
        Assert.Equal(GameLogEntryKind.Purchase, log[2].Kind);
        Assert.Equal("Purchased Trail Biscuits for $2.00.", log[2].Message);
        Assert.Equal(1, log[2].Day);
        Assert.Equal(1, log[2].Turn);
    }
}
