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
                Difficulty = TravelDifficulty.Normal,
                TravelRandomness = TravelRandomnessState.CreateDeterministic(string.Empty),
                Entropy = AdventureRandomnessPolicy.Standard
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
                Difficulty = TravelDifficulty.Normal,
                TravelRandomness = TravelRandomnessState.CreateDeterministic(string.Empty),
                Entropy = AdventureRandomnessPolicy.Standard
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
    public void DiaryProjector_GameStarted_ProducesArrivalEntry()
    {
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
                StartingInventoryItems = Array.Empty<DomainInventoryItem>(),
                Difficulty = TravelDifficulty.Normal,
                TravelRandomness = TravelRandomnessState.CreateDeterministic(string.Empty),
                Entropy = AdventureRandomnessPolicy.Standard
            }
        };

        var diary = projector.Project(events);

        Assert.Single(diary.Entries);
        Assert.Contains("Pinecross", diary.Entries[0].Summary);
        Assert.Equal("Pinecross", diary.CurrentTownName);
    }

    [Fact]
    public void DiaryProjector_StoreItemPurchased_AddsDiaryEntry()
    {
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
                StartingInventoryItems = Array.Empty<DomainInventoryItem>(),
                Difficulty = TravelDifficulty.Normal,
                TravelRandomness = TravelRandomnessState.CreateDeterministic(string.Empty),
                Entropy = AdventureRandomnessPolicy.Standard
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
            }
        };

        var diary = projector.Project(events);

        Assert.Equal(2, diary.Entries.Count);
        Assert.Contains("store", diary.Entries[1].Summary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FullAuditProjector_ProducesEntryForEachEvent()
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
                StartingInventoryItems = Array.Empty<DomainInventoryItem>(),
                Difficulty = TravelDifficulty.Normal,
                TravelRandomness = TravelRandomnessState.CreateDeterministic(string.Empty),
                Entropy = AdventureRandomnessPolicy.Standard
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
            }
        };

        var audit = projector.Project(events);

        Assert.Equal(2, audit.Entries.Count);
        Assert.Equal(1, audit.Entries[0].Sequence);
        Assert.Equal("GameStarted", audit.Entries[0].EventType);
        Assert.Equal(2, audit.Entries[1].Sequence);
        Assert.Equal("StoreItemPurchased", audit.Entries[1].EventType);
        Assert.Contains("Ranger Vale", audit.Entries[0].Summary);
        Assert.Contains("Trail Biscuits", audit.Entries[1].Summary);
    }

    [Fact]
    public void CaseFileViewProjector_ProducesViewFromSeedCaseFile()
    {
        var projector = new CaseFileViewProjector();
        var suspects = new[]
        {
            new Suspect(new SuspectId("suspect-1"), "Ira Flint", SuspectTraits.FromTags(SuspectTraitTags.Local, SuspectTraitTags.Desperate), SuspectStatus.AtLarge)
        };
        var caseFile = new CaseFile(null, suspects, new SuspectId("suspect-1"), Array.Empty<Clue>());
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
                Difficulty = TravelDifficulty.Normal,
                TravelRandomness = TravelRandomnessState.CreateDeterministic(string.Empty),
                Entropy = AdventureRandomnessPolicy.Standard
            }
        };

        var view = projector.Project(Guid.NewGuid(), caseFile, events);

        Assert.Single(view.DiscoveredSuspects);
        Assert.Equal("Ira Flint", view.DiscoveredSuspects[0].Name);
        Assert.NotNull(view.CaseSummary);
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
                Difficulty = TravelDifficulty.Normal,
                TravelRandomness = TravelRandomnessState.CreateDeterministic(string.Empty),
                Entropy = AdventureRandomnessPolicy.Standard
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
    public void HudProjector_EmptyEventStream_ProducesDefaultProjection()
    {
        var projector = new HudProjector();
        var events = Array.Empty<IDomainEvent>();

        var hud = projector.Project(events);

        Assert.Equal(GameStatus.Active, hud.Status);
        Assert.Equal(0m, hud.WalletCash);
        Assert.Empty(hud.InventoryItems);
    }

    [Fact]
    public void DiaryProjector_InvestigationPerformed_AddsDiaryEntryWithMessage()
    {
        var projector = new DiaryProjector();
        var events = new IDomainEvent[]
        {
            new GameStarted
            {
                PlayerName = "Ranger Vale",
                StartingTownId = new TownId("dustvale"),
                StartingTownName = "Dustvale",
                StartingHealth = 100,
                StartingWallet = 25m,
                StartingInventoryItems = Array.Empty<DomainInventoryItem>(),
                Difficulty = TravelDifficulty.Normal,
                TravelRandomness = TravelRandomnessState.CreateDeterministic(string.Empty),
                Entropy = AdventureRandomnessPolicy.Standard
            },
            new InvestigationPerformed
            {
                SourceKind = InvestigationSourceKind.LocalGossip,
                TownId = new TownId("dustvale"),
                Message = "You ask around for local gossip and uncover a public lead: a dusty boot print."
            }
        };

        var projection = projector.Project(events);

        Assert.Equal(2, projection.Entries.Count);
        Assert.Contains(projection.Entries, e => e.Summary.Contains("public lead"));
        Assert.Equal(1, projection.Entries[1].Turn);
    }
}
