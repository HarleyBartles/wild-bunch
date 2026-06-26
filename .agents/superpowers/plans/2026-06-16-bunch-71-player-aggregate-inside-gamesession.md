# BUNCH-71 Player Aggregate Inside GameSession Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Introduce a clearer player-owned boundary inside `GameSession` by moving wallet, inventory, health, town, and closely related capability access behind `Player` methods without changing gameplay behavior or persistence shape.

**Architecture:** Keep `GameSession` as the aggregate root and command orchestrator. Add small, session-owned behavior methods to `Player` for wallet, inventory, health, and state queries, then replace direct mutation call sites in `GameSession` with those methods. Preserve `Inventory` and `Wallet` as the concrete state holders underneath `Player`.

**Tech Stack:** C# / .NET, xUnit, existing Wild Bunch domain and integration test projects.

---

### Task 1: Add Player-owned behavior coverage

**Files:**
- Create: `tests/WildBunch.Domain.Tests/PlayerTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
using WildBunch.Domain.Economy;
using WildBunch.Domain.Game;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.World;

namespace WildBunch.Domain.Tests;

public sealed class PlayerTests
{
    [Fact]
    public void PlayerAdjustsWalletInventoryHealthAndTownThroughOwnedBehavior()
    {
        var player = new Player(
            "Ranger Vale",
            new TownId("pinecross"),
            1000,
            Wallet.Starting(25m),
            new Inventory(new[]
            {
                new InventoryItem(ItemKind.Food, 1),
                new InventoryItem(ItemKind.Canteen, 1)
            }));

        player.SpendCash(5m);
        player.AddCash(3m);
        player.AddInventoryItem(ItemKind.Food, 2);
        player.RemoveInventoryQuantity(ItemKind.Food, 1);
        player.AdjustHealth(-20);
        player.TravelTo(new TownId("holloway"));

        Assert.Equal(23m, player.Wallet.Cash);
        Assert.Equal(2, player.Inventory.GetQuantity(ItemKind.Food));
        Assert.Equal(980, player.Health);
        Assert.Equal(new TownId("holloway"), player.CurrentTownId);
    }

    [Fact]
    public void PlayerExposesHorseAndCanteenStateThroughOwnedBehavior()
    {
        var player = new Player(
            "Ranger Vale",
            new TownId("pinecross"),
            1000,
            Wallet.Starting(25m),
            new Inventory(new[]
            {
                new InventoryItem(ItemKind.Horse, 1, HorseTravelState.Healthy),
                new InventoryItem(ItemKind.Canteen, 1, CanteenState.Full(10))
            }));

        Assert.True(player.HasItem(ItemKind.Horse));
        Assert.Equal(HorseTravelState.Healthy, player.GetHorseState());
        Assert.Equal(10, player.GetCanteenState()!.Charges);
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --filter FullyQualifiedName~PlayerTests`
Expected: compile or member-missing failures because the new Player methods do not exist yet.

- [ ] **Step 3: Write the minimal Player API**

Add small wallet, inventory, and state wrapper methods to `src/WildBunch.Domain/Game/Player.cs` so the tests compile and pass.

- [ ] **Step 4: Run the test to verify it passes**

Run: `dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --filter FullyQualifiedName~PlayerTests`
Expected: PASS.

### Task 2: Route GameSession through Player-owned behavior

**Files:**
- Modify: `src/WildBunch.Domain/Game/GameSession.cs`
- Modify: `src/WildBunch.Domain/Game/Player.cs`
- Modify: `tests/WildBunch.Domain.Tests/GameSessionPurchaseTests.cs`
- Modify: `tests/WildBunch.Domain.Tests/GameSessionSheriffTurnInTests.cs`
- Modify: `tests/WildBunch.Domain.Tests/GameSessionSaloonPersonOfInterestTests.cs`

- [ ] **Step 1: Add or adjust behavior-preserving assertions**

Keep the existing purchase, settlement, and saloon regression tests green while the implementation switches to `Player` methods.

- [ ] **Step 2: Replace direct wallet/inventory mutation call sites**

Update `GameSession` to call `Player` methods for wallet delta application, purchase spending, bounty payout, fine application, item addition/removal, horse/canteen state updates, and the direct capability queries that can be expressed through `Player`.

- [ ] **Step 3: Run focused domain tests**

Run the Player tests plus the touched GameSession regression tests to confirm behavior is unchanged.

### Task 3: Run repo validation and capture evidence

**Files:**
- No source changes expected unless validation exposes a narrow defect.

- [ ] **Step 1: Run repository validation**

Run `dotnet build`, `dotnet test`, and the repo PostgreSQL validation lane if the slice touches persistence or integration behavior.

- [ ] **Step 2: Record branch and publication proof**

Capture the final branch name, head commit, remote head, PR URL, changed files, and cleanup status for return evidence.
