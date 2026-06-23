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
            new TownActionContextEntered
            {
                Context = TownActionContext.Saloon,
                Day = 1,
                Turn = 1,
                TimeOfDay = TimeOfDay.Morning
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

    // --- BUNCH-80: Bounty/Saloon event projection tests ---

    [Fact]
    public void DiaryProjector_SaloonPersonOfInterestSpotted_AppendsDiaryEntryWhenRecordLog()
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
            new TownActionContextEntered { Context = TownActionContext.Saloon, Day = 1, Turn = 1, TimeOfDay = TimeOfDay.Morning },
            new SaloonPersonOfInterestSpotted
            {
                SourceKind = InvestigationSourceKind.SaloonLookAround,
                TownId = new TownId("pinecross"),
                Message = "You look around the saloon and spot a shady figure.",
                RecordLog = true
            }
        };

        var diary = projector.Project(events);

        Assert.Contains(diary.Entries, e => e.Summary.Contains("shady figure"));
    }

    [Fact]
    public void DiaryProjector_SaloonPersonOfInterestSpotted_DoesNotAppendWhenRecordLogFalse()
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
            new TownActionContextEntered { Context = TownActionContext.Saloon, Day = 1, Turn = 1, TimeOfDay = TimeOfDay.Morning },
            new SaloonPersonOfInterestSpotted
            {
                SourceKind = InvestigationSourceKind.SaloonLookAround,
                TownId = new TownId("pinecross"),
                Message = "You look around the saloon and spot a townsfolk.",
                RecordLog = false
            }
        };

        var diary = projector.Project(events);

        Assert.DoesNotContain(diary.Entries, e => e.Summary.Contains("townsfolk"));
    }

    [Fact]
    public void DiaryProjector_WantedSuspectConfronted_AppendsDiaryEntry()
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
            new WantedSuspectConfronted
            {
                TargetSuspectId = new SuspectId("suspect-1"),
                TargetName = "Cole Tanner",
                Disposition = WarrantDisposition.DeadOrAlive,
                Choice = WantedSuspectConfrontationChoice.Surrendered,
                Outcome = WantedSuspectConfrontationOutcome.Surrendered,
                IsAlive = true,
                IsSecured = true,
                Message = "You confront Cole Tanner. He surrenders."
            }
        };

        var diary = projector.Project(events);

        Assert.Contains(diary.Entries, e => e.Summary.Contains("Cole Tanner"));
    }

    [Fact]
    public void DiaryProjector_SheriffTurnInSettled_AppendsDiaryEntry()
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

        var diary = projector.Project(events);

        Assert.Contains(diary.Entries, e => e.Summary.Contains("sheriff pays"));
    }

    [Fact]
    public void DiaryProjector_SaloonPersonOfInterestConfronted_DoesNotAppendDiaryEntry()
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
            new SaloonPersonOfInterestConfronted
            {
                Message = "Wrong declaration.",
                TargetName = "the stranger",
                PersonOfInterestKind = SaloonPersonOfInterestKind.Citizen,
                Outcome = SaloonPersonOfInterestConfrontationOutcome.WrongWantedDeclaration,
                IsCitizen = true
            }
        };

        var diary = projector.Project(events);

        // SaloonPersonOfInterestConfronted never produces a diary entry —
        // log entries come from delegated WantedSuspectConfronted/SheriffTurnInSettled events
        Assert.DoesNotContain(diary.Entries, e => e.Summary.Contains("Wrong declaration"));
    }

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
                Difficulty = TravelDifficulty.Normal,
                TravelRandomness = TravelRandomnessState.CreateDeterministic(string.Empty),
                Entropy = AdventureRandomnessPolicy.Standard
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
                Difficulty = TravelDifficulty.Normal,
                TravelRandomness = TravelRandomnessState.CreateDeterministic(string.Empty),
                Entropy = AdventureRandomnessPolicy.Standard
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

    // --- BUNCH-80: CaseFileViewProjector confrontation/settlement state ---

    [Fact]
    public void CaseFileViewProjector_WantedSuspectConfronted_AddsConfrontationToProjection()
    {
        var projector = new CaseFileViewProjector();
        var suspectId = new SuspectId("suspect-1");
        var suspects = new[]
        {
            new Suspect(suspectId, "Ira Flint", SuspectTraits.Empty, SuspectStatus.AtLarge)
        };
        var caseFile = new CaseFile(null, suspects, new SuspectId("suspect-1"), Array.Empty<Clue>());
        var events = new IDomainEvent[]
        {
            new WantedSuspectConfronted
            {
                TargetSuspectId = suspectId,
                TargetName = "Ira Flint",
                Disposition = WarrantDisposition.DeadOrAlive,
                Choice = WantedSuspectConfrontationChoice.Surrendered,
                Outcome = WantedSuspectConfrontationOutcome.Surrendered,
                IsAlive = true,
                IsSecured = true,
                Message = "You confront Ira Flint. He surrenders."
            }
        };

        var view = projector.Project(Guid.NewGuid(), caseFile, events);

        Assert.Contains(view.Confrontations, c => c.SuspectId.Equals(suspectId));
        Assert.Equal(WantedSuspectConfrontationOutcome.Surrendered, view.Confrontations.Single().Outcome);
    }

    [Fact]
    public void CaseFileViewProjector_SheriffTurnInSettled_AddsSettlementToProjection()
    {
        var projector = new CaseFileViewProjector();
        var suspectId = new SuspectId("suspect-1");
        var suspects = new[]
        {
            new Suspect(suspectId, "Ira Flint", SuspectTraits.Empty, SuspectStatus.AtLarge)
        };
        var caseFile = new CaseFile(null, suspects, new SuspectId("suspect-1"), Array.Empty<Clue>());
        var events = new IDomainEvent[]
        {
            new WantedSuspectConfronted
            {
                TargetSuspectId = suspectId,
                TargetName = "Ira Flint",
                Disposition = WarrantDisposition.DeadOrAlive,
                Choice = WantedSuspectConfrontationChoice.Surrendered,
                Outcome = WantedSuspectConfrontationOutcome.Surrendered,
                IsAlive = true,
                IsSecured = true,
                Message = "You confront Ira Flint. He surrenders."
            },
            new SheriffTurnInSettled
            {
                TargetSuspectId = suspectId,
                TargetName = "Ira Flint",
                Disposition = WarrantDisposition.DeadOrAlive,
                IsAlive = true,
                BountyAmount = 50m,
                Message = "The sheriff pays you $50.00.",
                Day = 1,
                Turn = 1
            }
        };

        var view = projector.Project(Guid.NewGuid(), caseFile, events);

        Assert.Contains(view.Settlements, s => s.SuspectId.Equals(suspectId));
        Assert.Equal(50m, view.Settlements.Single().BountyAmount);
    }

    [Fact]
    public void CaseFileViewProjector_WantedSuspectConfrontedAbandoned_DoesNotAddConfrontation()
    {
        var projector = new CaseFileViewProjector();
        var suspectId = new SuspectId("suspect-1");
        var suspects = new[]
        {
            new Suspect(suspectId, "Ira Flint", SuspectTraits.Empty, SuspectStatus.AtLarge)
        };
        var caseFile = new CaseFile(null, suspects, new SuspectId("suspect-1"), Array.Empty<Clue>());
        var events = new IDomainEvent[]
        {
            new WantedSuspectConfronted
            {
                TargetSuspectId = suspectId,
                TargetName = "Ira Flint",
                Disposition = WarrantDisposition.DeadOrAlive,
                Choice = WantedSuspectConfrontationChoice.Abandoned,
                Outcome = WantedSuspectConfrontationOutcome.Abandoned,
                IsAlive = true,
                IsSecured = false,
                Message = "You let the opportunity pass."
            }
        };

        var view = projector.Project(Guid.NewGuid(), caseFile, events);

        Assert.Empty(view.Confrontations);
    }
}
