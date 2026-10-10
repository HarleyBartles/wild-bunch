using WildBunch.Application.Abstractions;
using WildBunch.Application.Projections;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Economy;
using WildBunch.Domain.Events;
using WildBunch.Domain.Game;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using DomainWorld = WildBunch.Domain.World.World;
using DomainInventory = WildBunch.Domain.Inventory.Inventory;
using DomainInventoryItem = WildBunch.Domain.Inventory.InventoryItem;
using DomainItemKind = WildBunch.Domain.Inventory.ItemKind;

namespace WildBunch.Application.Tests.Projections;

public sealed class ProjectionTests
{
    [Fact]
    public void HudProjector_GameStarted_ProducesActiveHudWithStartingState()
    {
        var projector = new HudProjector();
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
            }
        };

        var hud = projector.Project(events);

        Assert.Equal(GameStatus.Active, hud.Status);
        Assert.Equal("Ranger Vale", hud.PlayerName);
        Assert.Equal(100, hud.Health);
        Assert.Equal(25m, hud.WalletCash);
        Assert.Equal(new TownId("pinecross"), hud.CurrentTownId);
        Assert.Equal("Pinecross", hud.CurrentTownName);
        Assert.Equal(2, hud.InventoryItems.Count);
        Assert.Equal(3, hud.InventoryItems.Single(i => i.ItemKind == DomainItemKind.Food).Quantity);
        Assert.Equal(1, hud.InventoryItems.Single(i => i.ItemKind == DomainItemKind.Canteen).Quantity);
    }

    [Fact]
    public void HudProjector_StoreItemPurchased_UpdatesWalletAndInventory()
    {
        var projector = new HudProjector();
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
                    new DomainInventoryItem(DomainItemKind.Food, 1)
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
                Quantity = 3,
                UnitPrice = 2m,
                TotalPrice = 6m,
                WalletAfter = 19m
            }
        };

        var hud = projector.Project(events);

        Assert.Equal(19m, hud.WalletCash);
        Assert.Equal(4, hud.InventoryItems.Single(i => i.ItemKind == DomainItemKind.Food).Quantity);
    }

    [Fact]
    public void FullAuditProjector_SaloonDevOverrideEvents_ProduceReadableSummaries()
    {
        var projector = new FullAuditProjector();
        var events = new IDomainEvent[]
        {
            new DevSaloonOverrideForced
            {
                ForcedKind = DevSaloonPoiKind.Suspect,
                ForcedSuspectId = new SuspectId("suspect-1")
            },
            new DevSaloonOverrideCleared(),
            new DevSaloonOverrideConsumed()
        };

        var audit = projector.Project(events.Select((domainEvent, index) => new RecordedDomainEvent(domainEvent, index + 1, DateTime.UnixEpoch)).ToArray());

        Assert.Equal(3, audit.Entries.Count);
        Assert.Equal("DevSaloonOverrideForced", audit.Entries[0].EventType);
        Assert.Equal("DevSaloonOverrideCleared", audit.Entries[1].EventType);
        Assert.Equal("DevSaloonOverrideConsumed", audit.Entries[2].EventType);
        Assert.Contains("Forced saloon override", audit.Entries[0].Summary);
        Assert.Contains("suspect-1", audit.Entries[0].Summary);
        Assert.Equal("Cleared pending saloon override.", audit.Entries[1].Summary);
        Assert.Equal("Consumed pending saloon override during saloon look-around.", audit.Entries[2].Summary);
    }

    [Fact]
    public void Projectors_DoNotMutateInputEvents()
    {
        // Projectors are pure functions — they must not mutate the input events.
        var projector = new HudProjector();
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
                    new DomainInventoryItem(DomainItemKind.Food, 1)
                },
                GameDifficulty = GameDifficulty.Standard,
                SaltSource = SaltSource.CreateFixed(string.Empty),
                GameEntropy = GameEntropy.Classic
            }
        };

        var hud1 = projector.Project(events);
        var hud2 = projector.Project(events);

        // Idempotent: projecting the same events twice produces the same result
        Assert.Equal(hud1.WalletCash, hud2.WalletCash);
        Assert.Equal(hud1.Health, hud2.Health);
        Assert.Equal(hud1.InventoryItems.Count, hud2.InventoryItems.Count);
    }

    [Fact]
    public void HudProjector_ThrowsWhenStreamHasNotStartedGame()
    {
        var projector = new HudProjector();
        var events = new IDomainEvent[]
        {
            new PlayerSetupCompleted
            {
                PlayerName = "Ranger Vale",
                GameDifficulty = GameDifficulty.Standard,
                GameEntropy = GameEntropy.Classic,
                SeedCode = "00000000-0000-0000-0000-000000000000"
            }
        };

        Assert.Throws<InvalidOperationException>(() => projector.Project(events));
    }

    // --- BUNCH-80: Bounty/Saloon event projection tests ---

    // --- BUNCH-80: HudProjector wallet changes from bounty/saloon events ---

    [Fact]
    public void HudProjector_SheriffTurnInSettled_AddsBountyToWallet()
    {
        var projector = new HudProjector();
        var events = new IDomainEvent[]
        {
            new GameStarted
            {
                PlayerName = "Ranger Vale",
                StartingTownId = new TownId("pinecross"),
                StartingTownName = "Pinecross",
                StartingHealth = 100,
                StartingWallet = 10m,
                StartingInventoryItems = Array.Empty<DomainInventoryItem>(),
                GameDifficulty = GameDifficulty.Standard,
                SaltSource = SaltSource.CreateFixed(string.Empty),
                GameEntropy = GameEntropy.Classic
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
            }
        };

        var hud = projector.Project(events);

        Assert.Equal(60m, hud.WalletCash);
    }

    [Fact]
    public void HudProjector_SaloonPersonOfInterestConfronted_WithFine_SetsWalletAfter()
    {
        var projector = new HudProjector();
        var events = new IDomainEvent[]
        {
            new GameStarted
            {
                PlayerName = "Ranger Vale",
                StartingTownId = new TownId("pinecross"),
                StartingTownName = "Pinecross",
                StartingHealth = 100,
                StartingWallet = 100m,
                StartingInventoryItems = Array.Empty<DomainInventoryItem>(),
                GameDifficulty = GameDifficulty.Standard,
                SaltSource = SaltSource.CreateFixed(string.Empty),
                GameEntropy = GameEntropy.Classic
            },
            new SaloonPersonOfInterestConfronted
            {
                Message = "Wrong declaration.",
                TargetName = "the stranger",
                PersonOfInterestKind = SaloonPersonOfInterestKind.Citizen,
                Outcome = SaloonPersonOfInterestConfrontationOutcome.WrongWantedDeclaration,
                FineAmount = 25m,
                WalletBefore = 100m,
                WalletAfter = 75m,
                IsCitizen = true
            }
        };

        var hud = projector.Project(events);

        Assert.Equal(75m, hud.WalletCash);
    }

    [Fact]
    public void HudProjector_PlaythroughArchived_SetsStatusToArchived()
    {
        var projector = new HudProjector();
        var events = new IDomainEvent[]
        {
            new GameStarted
            {
                PlayerName = "Ranger Vale",
                StartingTownId = new TownId("pinecross"),
                StartingTownName = "Pinecross",
                StartingHealth = 100,
                StartingWallet = 25m,
                StartingInventoryItems = Array.Empty<DomainInventoryItem>(),
                GameDifficulty = GameDifficulty.Standard,
                SaltSource = SaltSource.CreateFixed(string.Empty),
                GameEntropy = GameEntropy.Classic
            },
            new PlaythroughArchived
            {
                ArchivedAtUtc = DateTime.UtcNow,
                ArchiveReason = "Completed",
                PlayerName = "Ranger Vale",
                LastTownId = new TownId("pinecross"),
                LastTownName = "Pinecross",
                Day = 1,
                Turn = "Morning",
                StatusBeforeArchive = GameStatus.Completed
            }
        };

        var hud = projector.Project(events);

        Assert.Equal(GameStatus.Archived, hud.Status);
        Assert.Equal(new TownId("pinecross"), hud.CurrentTownId);
        Assert.Equal("Pinecross", hud.CurrentTownName);
    }

}
