# Extract bounty declaration matching as a domain policy Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Move the active saloon person-of-interest declaration-to-warrant match decision out of `GameSession` into a small domain policy while preserving all existing confrontation behavior.

**Architecture:** Keep `GameSession` as the aggregate root and leave fines, settlement, wallet mutation, saloon clearing, and result shaping in place. Add one narrow domain policy in `WildBunch.Domain.Cases` that answers whether a declared wanted identity handle matches a specific warrant, then have `GameSession` call that policy instead of inlining the string comparison.

**Tech Stack:** C#/.NET, xUnit, `dotnet build`, `dotnet test`

---

### Task 1: Add focused policy coverage

**Files:**
- Create: `tests/WildBunch.Domain.Tests/BountyDeclarationMatchPolicyTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
using WildBunch.Domain.Cases;

namespace WildBunch.Domain.Tests;

public sealed class BountyDeclarationMatchPolicyTests
{
    [Fact]
    public void MatchesDeclaredWantedIdentityReturnsTrueOnlyForTheExactWarrantId()
    {
        var warrant = new Warrant(
            new WarrantId("warrant-public-1"),
            "Mira Cline",
            new WarrantTerms(
                WarrantDisposition.DeadOrAlive,
                2500m,
                Array.Empty<string>(),
                Array.Empty<string>(),
                "Dodge City Marshal",
                InvestigationTargetKind.TrueCulprit,
                Array.Empty<OutlawGangId>(),
                null));

        Assert.True(BountyDeclarationMatchPolicy.MatchesDeclaredWantedIdentity("warrant-public-1", warrant));
        Assert.False(BountyDeclarationMatchPolicy.MatchesDeclaredWantedIdentity("warrant-public-99", warrant));
        Assert.False(BountyDeclarationMatchPolicy.MatchesDeclaredWantedIdentity(string.Empty, warrant));
        Assert.False(BountyDeclarationMatchPolicy.MatchesDeclaredWantedIdentity(null, warrant));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --filter FullyQualifiedName~BountyDeclarationMatchPolicyTests`
Expected: FAIL because `BountyDeclarationMatchPolicy` does not exist yet.

- [ ] **Step 3: Write minimal implementation**

Add a new static policy in `src/WildBunch.Domain/Cases/BountyDeclarationMatchPolicy.cs` with one ordinal comparison method that returns `false` for null or whitespace handles and `true` only when the handle exactly matches the warrant id value.

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --filter FullyQualifiedName~BountyDeclarationMatchPolicyTests`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add tests/WildBunch.Domain.Tests/BountyDeclarationMatchPolicyTests.cs src/WildBunch.Domain/Cases/BountyDeclarationMatchPolicy.cs
git commit -m "test: cover bounty declaration matching policy"
```

### Task 2: Delegate the matching decision from GameSession

**Files:**
- Modify: `src/WildBunch.Domain/Game/GameSession.cs`
- Modify: `tests/WildBunch.Domain.Tests/GameSessionSaloonPersonOfInterestTests.cs`

- [ ] **Step 1: Write the failing test**

Add or tighten one saloon confrontation test so it still proves a correct declared wanted identity succeeds and a wrong declaration still yields the wrong-declaration path after the policy extraction.

```csharp
// Reuse the existing armed wanted-session setup.
var result = session.ConfrontSaloonPersonOfInterest("warrant-public-1");
Assert.Equal(SaloonPersonOfInterestConfrontationOutcome.Surrendered, result.Outcome);
Assert.Contains("pays you $2500.00", result.Message, StringComparison.OrdinalIgnoreCase);
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --filter FullyQualifiedName~GameSessionSaloonPersonOfInterestTests`
Expected: FAIL only if the extraction accidentally changes behavior.

- [ ] **Step 3: Write minimal implementation**

Replace the inline `string.Equals(declaredWantedIdentityHandle, activeSaloonWarrant.Id.Value, StringComparison.Ordinal)` check in `ConfrontSaloonPersonOfInterest` with the new domain policy.

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --filter FullyQualifiedName~GameSessionSaloonPersonOfInterestTests`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/WildBunch.Domain/Game/GameSession.cs tests/WildBunch.Domain.Tests/GameSessionSaloonPersonOfInterestTests.cs
git commit -m "refactor: extract bounty declaration matching policy"
```

### Task 3: Run the repo validation ladder

**Files:**
- None

- [ ] **Step 1: Run the focused domain tests**

Run: `dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj`

- [ ] **Step 2: Run the full build**

Run: `dotnet build`

- [ ] **Step 3: Run the full test suite**

Run: `dotnet test`

- [ ] **Step 4: Record any warnings separately from failures**

Note whether any warnings are expected, new, or unrelated.

- [ ] **Step 5: Prepare publication evidence**

Record branch name, head SHA, remote head SHA, PR URL, changed files, clean worktree status, and a short handoff note that BUNCH-70 still owns the fine/settlement extraction.

## Self-Review

**Spec coverage:** The plan covers the declaration-match policy extraction only, preserves GameSession as the caller, and keeps fine/settlement logic in place for BUNCH-70.

**Placeholder scan:** No TBDs or missing file paths remain.

**Type consistency:** `BountyDeclarationMatchPolicy.MatchesDeclaredWantedIdentity(string?, Warrant)` is the single new API named in this plan, and the tests call that exact signature.
