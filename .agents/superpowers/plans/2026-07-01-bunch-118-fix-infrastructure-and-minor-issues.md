# BUNCH-118: Fix infrastructure and minor issues — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix three infrastructure/minor issues: (1) CORS policy too restrictive for Vite port fallback, (2) malformed prologue culprit identifier substitution, (3) duplicate "Horse feed" display names from different vendors.

**Architecture:** Three independent, small fixes touching the API CORS registration, the domain descriptor formatter, and the domain store catalog. No aggregate-root or persistence changes. Each fix is independently testable and committable.

**Tech Stack:** C#/.NET 9, ASP.NET Core CORS, xUnit, React + TypeScript + Vitest.

## Global Constraints

- Workers branch from current `main` and publish through a PR.
- `GameSession` is the live-play aggregate root; game mutations flow through `GameSession`. None of these fixes touch that path.
- Hidden culprit truth remains internal; no culprit ids or internal suspect ids exposed in prologue text.
- Horse and saddle are separate inventory concepts; `ItemKind.HorseFeed` is the shared inventory kind — do NOT split it. The fix is display-name disambiguation only.
- Run `dotnet build` and `dotnet test` for backend validation.
- Run `npm test` in `src/WildBunch.Web` for frontend validation (only if frontend files change).
- Run `python scripts/generate_index_mesh.py` and commit updated INDEX.md if any file is added/removed (no new files are added in this plan, so this should be a no-op, but verify).

---

## Issue 1: CORS policy too restrictive

### Root cause

`src/WildBunch.Api/DependencyInjection.cs:28` hard-codes only `http://localhost:5173` and `http://127.0.0.1:5173`. When Vite's default port 5173 is occupied, it falls back to 5174 (or higher), and all API requests fail with CORS errors. The CORS policy is already gated to `IsDevelopment()` in `Program.cs:13-17`, so broadening it in development is safe.

### Fix

Replace `WithOrigins(...)` with `SetIsOriginAllowed(...)` that allows any `localhost` or `127.0.0.1` origin in the dev CORS policy. This covers any Vite fallback port without opening CORS to non-local origins.

### Task 1: Broaden dev CORS to allow any localhost port

**Files:**
- Modify: `src/WildBunch.Api/DependencyInjection.cs:24-32`
- Test: `tests/WildBunch.Api.Tests/CorsPolicyTests.cs` (create)

**Interfaces:**
- Consumes: `IServiceCollection` from ASP.NET Core
- Produces: A CORS policy named `"ViteDevClient"` that allows any `http://localhost:*` or `http://127.0.0.1:*` origin in development

- [ ] **Step 1: Write the failing test**

Create `tests/WildBunch.Api.Tests/CorsPolicyTests.cs`:

```csharp
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using WildBunch.Api;

namespace WildBunch.Api.Tests;

public sealed class CorsPolicyTests
{
    [Fact]
    public void ViteDevClientPolicyAllowsAnyLocalhostPort()
    {
        var services = new ServiceCollection();
        services.AddWildBunchServices(new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build());

        var corsOptions = services.BuildServiceProvider()
            .GetRequiredService<Microsoft.Extensions.Options.IOptions<CorsOptions>>()
            .Value;

        var policy = corsOptions.GetPolicy("ViteDevClient");
        Assert.NotNull(policy);

        Assert.True(policy!.IsOriginAllowed("http://localhost:5173"));
        Assert.True(policy.IsOriginAllowed("http://localhost:5174"));
        Assert.True(policy.IsOriginAllowed("http://localhost:3000"));
        Assert.True(policy.IsOriginAllowed("http://127.0.0.1:5173"));
        Assert.True(policy.IsOriginAllowed("http://127.0.0.1:5174"));

        // Non-local origins must still be rejected
        Assert.False(policy.IsOriginAllowed("http://example.com:5173"));
        Assert.False(policy.IsOriginAllowed("https://evil.com"));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.Api.Tests/CorsPolicyTests.cs`
Expected: FAIL — `IsOriginAllowed("http://localhost:5174")` returns false because the current policy only allows port 5173.

- [ ] **Step 3: Implement the fix**

Replace the `WithOrigins(...)` call in `src/WildBunch.Api/DependencyInjection.cs`:

```csharp
        services.AddCors(options =>
        {
            options.AddPolicy("ViteDevClient", policy =>
            {
                policy.SetIsOriginAllowed(origin =>
                        origin.StartsWith("http://localhost:", StringComparison.OrdinalIgnoreCase)
                        || origin.StartsWith("http://127.0.0.1:", StringComparison.OrdinalIgnoreCase))
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/WildBunch.Api.Tests/CorsPolicyTests.cs`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/WildBunch.Api/DependencyInjection.cs tests/WildBunch.Api.Tests/CorsPolicyTests.cs
git commit -m "BUNCH-118: broaden dev CORS to allow any localhost port"
```

---

## Issue 2: Prologue culprit identifier grammar

### Root cause

The prologue variant templates contain `"The one with {trueCulpritMainIdentifier}."`. The placeholder is substituted by `GetPrologueHandler` with the output of `PrologueDescriptorResolver.ResolveTrueCulpritDescriptor`, which calls `SaloonPersonOfInterestDescriptor.Describe`.

`SaloonPersonOfInterestDescriptor.FormatPublicDescriptor` wraps the feature text as `"a stranger with {NormalizeFeatureDescriptor(...)}"`. `NormalizeFeatureDescriptor` strips prefixes like `"has a "`, `"wears a "`, `"wearing a "` — but it does NOT handle the `"Is missing the right ear."` pattern from `CaseSuspectFeaturePool.cs:94-95`.

When the feature text is `"Is missing the right ear."`, normalization returns it unchanged, producing: `"a stranger with Is missing the right ear"` → substituted into the template: `"The one with a stranger with Is missing the right ear."` — which is the reported malformed text.

**Note:** The issue description names `PrologueContent.cs` as the file to fix, but the actual bug is in `SaloonPersonOfInterestDescriptor.NormalizeFeatureDescriptor` (the formatter that produces the descriptor). `PrologueContent.cs` only holds the templates and is correct. The fix belongs in the descriptor formatter.

### Fix

Add an `"is missing "` prefix normalization in `NormalizeFeatureDescriptor` that converts `"Is missing the right ear"` → `"a missing right ear"`, producing `"a stranger with a missing right ear"` → `"The one with a stranger with a missing right ear."` — grammatically correct and consistent with the existing `"a stranger with a scar on the left cheek"` shape.

### Task 2: Fix descriptor normalization for "is missing" features

**Files:**
- Modify: `src/WildBunch.Domain/Cases/SaloonPersonOfInterestDescriptor.cs:43-64`
- Test: `tests/WildBunch.Domain.Tests/SaloonPersonOfInterestDescriptorTests.cs` (create)

**Interfaces:**
- Consumes: `Suspect`, `CaseFile` from `WildBunch.Domain.Cases`
- Produces: `SaloonPersonOfInterestDescriptor.Describe` returns grammatically correct descriptors for all feature pool text patterns, including `"Is missing the left/right ear."`

- [ ] **Step 1: Write the failing test**

Create `tests/WildBunch.Domain.Tests/SaloonPersonOfInterestDescriptorTests.cs`:

```csharp
using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using DomainWorld = WildBunch.Domain.World.World;
using Town = WildBunch.Domain.World.Town;
using Trail = WildBunch.Domain.World.Trail;
using TrailId = WildBunch.Domain.World.TrailId;

namespace WildBunch.Domain.Tests;

public sealed class SaloonPersonOfInterestDescriptorTests
{
    [Fact]
    public void Describe_MissingEarFeatureProducesGrammaticalDescriptor()
    {
        var suspect = new Suspect(
            new SuspectId("suspect-1"),
            "Mira Cline",
            new SuspectProfile(
                Array.Empty<SuspectAlias>(),
                new[] { new SuspectIdentityFact("Is missing the right ear.") }),
            SuspectTraits.Empty,
            SuspectStatus.AtLarge);

        var caseFile = CreateCaseFile(suspect);

        var descriptor = SaloonPersonOfInterestDescriptor.Describe(suspect, caseFile);

        Assert.Equal("a stranger with a missing right ear", descriptor);
    }

    [Fact]
    public void Describe_MissingLeftEarFeatureProducesGrammaticalDescriptor()
    {
        var suspect = new Suspect(
            new SuspectId("suspect-1"),
            "Mira Cline",
            new SuspectProfile(
                Array.Empty<SuspectAlias>(),
                new[] { new SuspectIdentityFact("Is missing the left ear.") }),
            SuspectTraits.Empty,
            SuspectStatus.AtLarge);

        var caseFile = CreateCaseFile(suspect);

        var descriptor = SaloonPersonOfInterestDescriptor.Describe(suspect, caseFile);

        Assert.Equal("a stranger with a missing left ear", descriptor);
    }

    [Fact]
    public void Describe_ScarFeatureStillNormalizesCorrectly()
    {
        var suspect = new Suspect(
            new SuspectId("suspect-1"),
            "Mira Cline",
            new SuspectProfile(
                Array.Empty<SuspectAlias>(),
                new[] { new SuspectIdentityFact("Has a scar on the left cheek.") }),
            SuspectTraits.Empty,
            SuspectStatus.AtLarge);

        var caseFile = CreateCaseFile(suspect);

        var descriptor = SaloonPersonOfInterestDescriptor.Describe(suspect, caseFile);

        Assert.Equal("a stranger with a scar on the left cheek", descriptor);
    }

    private static CaseFile CreateCaseFile(Suspect suspect)
    {
        var currentTown = new Town(new TownId("current"), "Current Town", TownServices.None);
        var connectedTown = new Town(new TownId("connected"), "Connected Town", TownServices.None);
        var world = new DomainWorld(
            new[] { currentTown, connectedTown },
            new[] { new Trail(new TrailId("trail-1"), currentTown.Id, connectedTown.Id, TrailRisk.Low) });

        return new CaseFile(
            accusation: null,
            new[] { suspect },
            trueCulpritId: suspect.Id,
            openingLead: CaseOpeningLead.Create("Follow the public leads and look for a signature mark."),
            knownClues: Array.Empty<Clue>(),
            knownWarrants: Array.Empty<Warrant>());
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.Domain.Tests/SaloonPersonOfInterestDescriptorTests.cs`
Expected: FAIL — `Describe_MissingEarFeatureProducesGrammaticalDescriptor` fails because the current normalizer returns `"a stranger with Is missing the right ear"` instead of `"a stranger with a missing right ear"`.

- [ ] **Step 3: Implement the fix**

In `src/WildBunch.Domain/Cases/SaloonPersonOfInterestDescriptor.cs`, add an `"is missing "` normalization entry to the prefix list in `NormalizeFeatureDescriptor`:

```csharp
    private static string NormalizeFeatureDescriptor(string descriptor)
    {
        foreach (var (prefix, replacement) in new[]
        {
            ("has a ", "a "),
            ("has an ", "an "),
            ("wore a ", "a "),
            ("wore an ", "an "),
            ("wears a ", "a "),
            ("wears an ", "an "),
            ("wearing a ", "a "),
            ("wearing an ", "an "),
            ("is missing the ", "a missing "),
        })
        {
            if (descriptor.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return replacement + descriptor[prefix.Length..];
            }
        }

        return descriptor;
    }
```

This converts `"Is missing the right ear."` → `"a missing right ear."` (after `TrimDescriptor` strips the trailing period), producing `"a stranger with a missing right ear"`.

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/WildBunch.Domain.Tests/SaloonPersonOfInterestDescriptorTests.cs`
Expected: PASS

- [ ] **Step 5: Run existing prologue and parity tests to verify no regressions**

Run: `dotnet test tests/WildBunch.Application.Tests/PrologueHandlerTests.cs tests/WildBunch.Application.Tests/SaloonPersonOfInterestDescriptorParityTests.cs`
Expected: PASS — existing tests use `"Has a scar on the left cheek."` which still normalizes via the `"has a "` prefix.

- [ ] **Step 6: Commit**

```bash
git add src/WildBunch.Domain/Cases/SaloonPersonOfInterestDescriptor.cs tests/WildBunch.Domain.Tests/SaloonPersonOfInterestDescriptorTests.cs
git commit -m "BUNCH-118: fix prologue descriptor grammar for 'is missing' features"
```

---

## Issue 3: Duplicate "Horse feed" display names

### Root cause

`src/WildBunch.Domain/Economy/TownStoreCatalogModels.cs` defines `"Horse feed"` as the `DisplayName` for both general store offers (lines 77, 84, 91, 97) and stable offers (lines 109, 115). In Boomtown and Prosperous towns, both vendors sell "Horse feed" at different prices ($1.00 vs $1.25), so the store offers panel shows two identical-looking cards with different prices — confusing to the player.

The `StoreOffer` record already has a `VendorType` field and a `SourceNote` field, and the frontend `StoreOffersPanel.tsx` already displays the vendor type on a separate line. But the `DisplayName` itself is identical, so the two cards look like duplicates at a glance.

### Fix

Disambiguate the `DisplayName` in the catalog by appending the vendor source: `"Horse feed (General store)"` and `"Horse feed (Stable)"`. This is the approach the issue explicitly requests. The `ItemKind` stays `HorseFeed` — inventory behavior is unchanged. Only the store-offer display name changes.

**Scope note:** Only the `Horse feed` offers need disambiguation. No other item name overlaps across vendors (Food, Canteen, Knife, Horse, Saddle, Revolver, RevolverAmmo, RifleAmmo are each sold by only one vendor type).

### Task 3: Disambiguate duplicate "Horse feed" display names by vendor

**Files:**
- Modify: `src/WildBunch.Domain/Economy/TownStoreCatalogModels.cs` (lines 77, 84, 91, 97, 109, 115)
- Test: `tests/WildBunch.Domain.Tests/TownStoreCatalogResolverTests.cs` (add test)

**Interfaces:**
- Consumes: `Town`, `TownProsperity` from `WildBunch.Domain.World`
- Produces: `StoreOffer.DisplayName` for `ItemKind.HorseFeed` offers includes the vendor source suffix, e.g. `"Horse feed (General store)"` / `"Horse feed (Stable)"`

- [ ] **Step 1: Write the failing test**

Add to `tests/WildBunch.Domain.Tests/TownStoreCatalogResolverTests.cs`:

```csharp
    [Fact]
    public void HorseFeedDisplayNamesAreDisambiguatedByVendor()
    {
        var resolver = new TownStoreCatalogResolver();
        var town = new Town(new TownId("redmesa"), "Red Mesa", TownServices.Telegraph, TownProsperity.Boomtown);

        var catalog = resolver.Resolve(town);

        var horseFeedOffers = catalog.Offers
            .Where(o => o.ItemKind == ItemKind.HorseFeed)
            .ToList();

        // Boomtown has both a general store and a stable selling horse feed
        Assert.Equal(2, horseFeedOffers.Count);

        var generalStoreOffer = horseFeedOffers.Single(o => o.VendorType == StoreVendorType.GeneralStore);
        var stableOffer = horseFeedOffers.Single(o => o.VendorType == StoreVendorType.Stable);

        Assert.Equal("Horse feed (General store)", generalStoreOffer.DisplayName);
        Assert.Equal("Horse feed (Stable)", stableOffer.DisplayName);

        // Display names must be distinct so the store panel doesn't show duplicate-looking cards
        Assert.NotEqual(generalStoreOffer.DisplayName, stableOffer.DisplayName);
    }
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.Domain.Tests/TownStoreCatalogResolverTests.cs --filter HorseFeedDisplayNamesAreDisambiguatedByVendor`
Expected: FAIL — both offers have `DisplayName == "Horse feed"`.

- [ ] **Step 3: Implement the fix**

In `src/WildBunch.Domain/Economy/TownStoreCatalogModels.cs`, update all six `Horse feed` display names:

General store offers (lines 77, 84, 91, 97) — change `"Horse feed"` to `"Horse feed (General store)"`:

```csharp
                new StoreOffer(ItemKind.HorseFeed, "Horse feed (General store)", 1m, StoreVendorType.GeneralStore, StoreOfferAvailability.Available, "General store shelf"),
```
(Repeat for each prosperity tier's general store horse feed offer, keeping the respective price.)

Stable offers (lines 109, 115) — change `"Horse feed"` to `"Horse feed (Stable)"`:

```csharp
                new StoreOffer(ItemKind.HorseFeed, "Horse feed (Stable)", 1.25m, StoreVendorType.Stable, StoreOfferAvailability.Available, "Stable yard tack room")
```
(Repeat for Boomtown and Prosperous stable horse feed offers.)

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/WildBunch.Domain.Tests/TownStoreCatalogResolverTests.cs`
Expected: PASS

- [ ] **Step 5: Run purchase and store-offers tests to verify no regressions**

Run: `dotnet test tests/WildBunch.Application.Tests/PurchaseStoreItemHandlerTests.cs tests/WildBunch.Application.Tests/GetTownStoreOffersHandlerTests.cs`
Expected: PASS — these tests assert on `ItemKind` and `VendorType`, not on the exact `DisplayName` string. If any test asserts the old `"Horse feed"` display name, update it to the new disambiguated name.

- [ ] **Step 6: Commit**

```bash
git add src/WildBunch.Domain/Economy/TownStoreCatalogModels.cs tests/WildBunch.Domain.Tests/TownStoreCatalogResolverTests.cs
git commit -m "BUNCH-118: disambiguate duplicate Horse feed display names by vendor"
```

---

## Final validation

- [ ] **Step 1: Run full backend build and test suite**

Run: `dotnet build && dotnet test`
Expected: BUILD succeeds, all tests PASS.

- [ ] **Step 2: Run frontend tests (no frontend files changed, but verify no breakage from backend DTO changes)**

Run: `cd src/WildBunch.Web && npm test`
Expected: PASS — the `StoreOffersPanel` renders `offer.displayName` directly, so the new disambiguated names will display correctly. If any frontend test asserts the old `"Horse feed"` display name, update it.

- [ ] **Step 3: Verify index mesh is current**

Run: `python scripts/generate_index_mesh.py --check`
Expected: PASS — no new files added at non-ignored paths (test files are under `tests/` which is indexed, so if the check fails, regenerate with `python scripts/generate_index_mesh.py` and commit).

- [ ] **Step 4: Push branch and open PR**

```bash
git push -u origin harleydbartles/bunch-118-fix-infrastructure-and-minor-issues
gh pr create --title "BUNCH-118: Fix infrastructure and minor issues" --body "..."
```

- [ ] **Step 5: Update Linear route state**

Post a comment on BUNCH-118 with the plan path, PR URL, and route state `approved_plan_execution_ready` (after PR merge) or `preflight_complete_pending_approval` (after PR open, before merge).

---

## Self-Review

**1. Spec coverage:**
- CORS too restrictive → Task 1 ✓
- Prologue grammar malformed → Task 2 ✓
- Duplicate "Horse feed" names → Task 3 ✓
- Validation: `dotnet test`, `npm test`, manual playtest → Final validation steps ✓ (manual playtest is issue-listed but not automatable here; the unit/integration tests cover the behavioral assertions)

**2. Placeholder scan:** No TBD/TODO/placeholder text. All code blocks contain complete, runnable code.

**3. Type consistency:**
- `SaloonPersonOfInterestDescriptor.Describe(Suspect, CaseFile)` — signature matches existing usage in `PrologueDescriptorResolver.cs:38` and parity tests.
- `StoreOffer` record signature unchanged — only `DisplayName` string values change.
- `CorsOptions.GetPolicy("ViteDevClient")` — standard ASP.NET Core CORS API.
- `ItemKind.HorseFeed` — unchanged; only display name strings change.
