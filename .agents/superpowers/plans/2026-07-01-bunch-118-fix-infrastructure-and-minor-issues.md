# BUNCH-118: Fix infrastructure and minor issues — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix three infrastructure/minor issues: (1) CORS policy too restrictive for Vite port fallback, (2) malformed prologue culprit identifier substitution via a proper structured language-model refactor, (3) duplicate "Horse feed" display names from different vendors.

**Architecture:** Issues 1 and 3 are small, independent fixes — CORS registration broadening and store-catalog display-name disambiguation. Issue 2 is a larger domain language-model refactor: it replaces pre-baked feature sentence strings with structured `FeatureDescriptor` tokens + a `FeatureLanguageService` that generates context-appropriate phrasing at runtime, and eliminates the fragile `NormalizeFeatureDescriptor` prefix-stripping that caused the bug. Issue 2 adds new domain files, changes `SuspectIdentityFact`, changes the JSON snapshot shape, and intentionally breaks old dev saves (acceptable per AGENTS.md). Issues 1 and 3 are independently testable and committable; Issue 2 is a multi-task sequence (2a->2b->2c->2d) that must be committed in order.

**Tech Stack:** C#/.NET 10, ASP.NET Core CORS, xUnit, React + TypeScript + Vitest.

## Global Constraints

- Workers branch from current `main` and publish through a PR.
- `GameSession` is the live-play aggregate root; game mutations flow through `GameSession`. None of these fixes touch that path.
- Hidden culprit truth remains internal; no culprit ids or internal suspect ids exposed in prologue text.
- Horse and saddle are separate inventory concepts; `ItemKind.HorseFeed` is the shared inventory kind — do NOT split it. The fix is display-name disambiguation only.
- Run `dotnet build` and `dotnet test` for backend validation.
- Run `npm test` in `src/WildBunch.Web` for frontend validation (only if frontend files change).
- Run `python scripts/generate_index_mesh.py` and commit updated INDEX.md after files are added. **Issue 2 adds new files** (`FeatureLanguage.cs`, `FeatureLanguageService.cs`, test files, and a new `WildBunch.Api.Tests` project for Issue 1). The index mesh regeneration is **required**, not a no-op.
- **Issue 2 changes `SuspectIdentityFactSnapshot` shape.** Old JSON saves will fail to deserialize. Per AGENTS.md: "In this greenfield repo, current mainline model correctness wins over old-save or legacy internal compatibility" and "Dev database drop/recreate is allowed." Run `.\scripts\postgres-dev.ps1 reset` if integration tests fail on stale snapshot data.
- The CORS and Horse feed fixes (Issues 1 and 3) are independent of the language refactor. The language refactor (Issue 2) is **not** a small formatter-only patch — it touches the Domain, GameContent, Application, and Persistence layers.

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

Run: `dotnet test tests/WildBunch.Api.Tests --filter CorsPolicyTests`
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

Run: `dotnet test tests/WildBunch.Api.Tests --filter CorsPolicyTests`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/WildBunch.Api/DependencyInjection.cs tests/WildBunch.Api.Tests/CorsPolicyTests.cs
git commit -m "BUNCH-118: broaden dev CORS to allow any localhost port"
```

---

## Issue 2: Prologue culprit identifier grammar — proper fix with structured features + runtime language service

### Root cause

The prologue variant templates contain `"The one with {trueCulpritMainIdentifier}."`. The placeholder is substituted by `GetPrologueHandler` with the output of `PrologueDescriptorResolver.ResolveTrueCulpritDescriptor`, which calls `SaloonPersonOfInterestDescriptor.Describe`.

`SaloonPersonOfInterestDescriptor.FormatPublicDescriptor` wraps the feature text as `"a stranger with {NormalizeFeatureDescriptor(...)}"`. `NormalizeFeatureDescriptor` strips prefixes like `"has a "`, `"wears a "`, `"wearing a "` — but it does NOT handle the `"Is missing the right ear."` pattern from `CaseSuspectFeaturePool.cs:94-95`.

When the feature text is `"Is missing the right ear."`, normalization returns it unchanged, producing: `"a stranger with Is missing the right ear"` → `"The one with a stranger with Is missing the right ear."` — the reported malformed text.

**The prefix-stripping approach is fundamentally fragile.** `NormalizeFeatureDescriptor` also fails for accessories starting with `"Keeps a"`, `"Prefers a"`, `"Carries a"`, `"Leaves"`, or `"Has no"`. Adding more prefix entries is papering over the problem.

**Proper fix:** Store language variants with each feature and construct the appropriate phrasing at runtime. Primary markers (limp, missing ear, scar, no eyebrows) are stored as structured tokens (e.g., `FeatureCategory.Limp`, body part `"leg"`, side `Left`) and a `FeatureLanguageService` generates all context forms. Accessories store their pre-written variant forms directly. The fragile `NormalizeFeatureDescriptor` is eliminated entirely.

**Note:** The issue description names `PrologueContent.cs` as the file to fix, but the templates are correct. The bug is in the feature storage and descriptor formatting pipeline.

### Design

This is the first implementation of a foundational runtime language service that the game will rely on for a bunch of stuff later. The design leaves explicit seams for future language concerns — person (first/second/third), tense, time-relative language, and other narrative voice variations (diary "I met..." vs event notification "You spot..." vs prologue "The one with...").

**New Domain namespace:** `src/WildBunch.Domain/Language/` — general-purpose language infrastructure, not case-specific.

**Narrative voice seam** (`src/WildBunch.Domain/Language/NarrativeVoice.cs`):

```csharp
/// <summary>
/// The narrative voice in which game content is rendered. This is the
/// primary seam for the language service — different game surfaces need
/// different voices (diary = first person, event notifications = second
/// person, prologue/clue anchors = third person). Only ThirdPerson is
/// implemented in BUNCH-118; SecondPerson and FirstPerson are explicit
/// future seams that throw NotImplementedException until implemented.
/// </summary>
public enum NarrativeVoice
{
    ThirdPerson = 0,   // "Has a limp in the left leg." / "a stranger with..."
    SecondPerson = 1,  // FUTURE: "You notice a limp in their left leg." (event notifications)
    FirstPerson = 2    // FUTURE: "I noticed a limp in their left leg." (diary/journal)
}
```

**Language context** (`src/WildBunch.Domain/Language/LanguageContext.cs`):

```csharp
/// <summary>
/// Carries the narrative context in which language is rendered. This is
/// the extension point for future language concerns — tense, formality,
/// time-relative language, etc. Only Voice is used in BUNCH-118; future
/// fields are explicit seams.
/// </summary>
public sealed record LanguageContext(
    NarrativeVoice Voice = NarrativeVoice.ThirdPerson
    // FUTURE SEAMS — do not implement until needed:
    // , NarrativeTense Tense = NarrativeTense.Past
    // , NarrativeFormality Formality = NarrativeFormality.Plain
    // , TimeRelativity TimeRelativity = TimeRelativity.Absolute
    )
{
    public static LanguageContext Default { get; } = new();
};
```

**Feature descriptor** (`src/WildBunch.Domain/Language/FeatureDescriptor.cs`):

```csharp
public enum FeatureCategory
{
    Limp = 0,
    MissingPart = 1,
    Scar = 2,
    Absence = 3
}

public enum FeatureSide
{
    None = 0,
    Left = 1,
    Right = 2
}

public sealed record FeatureDescriptor(FeatureCategory Category, string BodyPart, FeatureSide Side);
```

**Feature language** (`src/WildBunch.Domain/Language/FeatureLanguage.cs`):

```csharp
/// <summary>
/// The rendered language forms for a feature in a specific narrative voice.
/// Currently only ThirdPerson forms are populated; SecondPerson and FirstPerson
/// forms are explicit future seams (nullable, throw when accessed if voice is
/// unsupported). The four ThirdPerson forms cover all current consumer surfaces:
/// warrants/clue anchors (HasForm), saloon POI (WithForm), clue subordinate
/// clauses (WhoForm), and opening leads (OpeningLeadForm).
/// </summary>
public sealed record FeatureLanguage(
    string HasForm,           // "Has a limp in the left leg." — full sentence for warrants/clue anchors
    string WithForm,          // "a limp in the left leg" — noun phrase after "a stranger with"
    string WhoForm,           // "has a limp in the left leg" — lowercase clause after "who"
    string? OpeningLeadForm,  // "The culprit walks with a limp in the left leg." — null for accessories
    NarrativeVoice Voice = NarrativeVoice.ThirdPerson)
{
    /// <summary>
    /// Constructs a FeatureLanguage from explicit forms, for test fixtures
    /// and non-feature-pool identity facts that don't have structured tokens.
    /// </summary>
    public static FeatureLanguage Raw(string hasForm, string withForm, string? whoForm = null)
        => new(hasForm, withForm, whoForm ?? hasForm.ToLowerInvariant(), null);
}
```

**Language service** (`src/WildBunch.Domain/Language/LanguageService.cs`):

The umbrella entry point for all language rendering. Currently delegates to `FeatureLanguageService` for feature descriptors. Future content types (diary entries, event notifications, travel diary flavour, etc.) plug into the same service.

```csharp
/// <summary>
/// The top-level language service for the game. This is the umbrella entry
/// point for all runtime language rendering. BUNCH-118 implements only
/// feature-descriptor rendering in ThirdPerson. Future content types
/// (diary, notifications, travel flavour) and future voices (SecondPerson,
/// FirstPerson) are explicit seams — add new renderers here, do not create
/// parallel language services.
/// </summary>
public static class LanguageService
{
    /// <summary>
    /// Renders a feature descriptor in the given narrative context.
    /// Currently only ThirdPerson is implemented.
    /// </summary>
    public static FeatureLanguage Render(FeatureDescriptor descriptor, LanguageContext? context = null)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        var ctx = context ?? LanguageContext.Default;

        return ctx.Voice switch
        {
            NarrativeVoice.ThirdPerson => FeatureLanguageService.RenderThirdPerson(descriptor),
            NarrativeVoice.SecondPerson => throw new NotImplementedException(
                "SecondPerson feature rendering is not yet implemented. This is a future seam — "
                + "event notifications will need 'You notice...' forms."),
            NarrativeVoice.FirstPerson => throw new NotImplementedException(
                "FirstPerson feature rendering is not yet implemented. This is a future seam — "
                + "diary/journal entries will need 'I noticed...' forms."),
            _ => throw new ArgumentOutOfRangeException(nameof(ctx), ctx.Voice, "Unsupported narrative voice.")
        };
    }
}
```

**Feature language service** (`src/WildBunch.Domain/Language/FeatureLanguageService.cs`):

The concrete renderer for feature descriptors in ThirdPerson. Each `FeatureCategory` has templates for all four forms. Accessories get hand-written `FeatureLanguage` values (their copy is too varied to template). This is internal to the language service — callers go through `LanguageService.Render`.

```csharp
internal static class FeatureLanguageService
{
    public static FeatureLanguage RenderThirdPerson(FeatureDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        return descriptor.Category switch
        {
            FeatureCategory.Limp => ForLimp(descriptor),
            FeatureCategory.MissingPart => ForMissingPart(descriptor),
            FeatureCategory.Scar => ForScar(descriptor),
            FeatureCategory.Absence => ForAbsence(descriptor),
            _ => throw new ArgumentOutOfRangeException(nameof(descriptor), descriptor.Category, "Unsupported feature category.")
        };
    }

    // ... per-category template methods (ForLimp, ForMissingPart, ForScar, ForAbsence) ...
}
```

**Future seams (explicit, not implemented in BUNCH-118):**

| Seam | Where | What | When |
|------|-------|------|------|
| SecondPerson voice | `LanguageService.Render` switch | "You notice a limp in their left leg." | Event notifications, saloon look-around |
| FirstPerson voice | `LanguageService.Render` switch | "I noticed a limp in their left leg." | Diary/journal entries |
| Tense | `LanguageContext` | Past vs present vs future | Travel diary past tense, encounter present tense |
| New content types | `LanguageService` | Diary entries, notifications, travel flavour | When those surfaces adopt structured language |
| New feature categories | `FeatureLanguageService` | New `FeatureCategory` enum values + template methods | When new primary marker types are added |

**Data flow change:**

```
Before: CaseSuspectFeaturePool stores "Has a limp in the left leg." (string)
        -> SuspectIdentityFact(Description: "Has a limp in the left leg.")
        -> SaloonPersonOfInterestDescriptor tries to reverse-engineer noun phrase via NormalizeFeatureDescriptor

After:  CaseSuspectFeaturePool stores FeatureDescriptor(Limp, "leg", Left)
        -> LanguageService.Render(descriptor) -> FeatureLanguage(HasForm, WithForm, WhoForm, OpeningLeadForm)
        -> SuspectIdentityFact(Language: featureLanguage)
        -> SaloonPersonOfInterestDescriptor uses Language.WithForm directly — no normalization
        -> Future: LanguageService.Render(descriptor, new LanguageContext(Voice: FirstPerson)) for diary
```

**What gets eliminated:**
- `SaloonPersonOfInterestDescriptor.NormalizeFeatureDescriptor` — gone
- `SaloonPersonOfInterestDescriptor.FormatPublicDescriptor` — simplified to `$"a stranger with {language.WithForm}"`
- `SeedCaseBuilder.DescribeFeatureClause` — replaced by `Language.WhoForm`
- `CaseSuspectFeatureProfile.Description` and `.OpeningLeadText` — replaced by `.Language`

**Persistence impact:** `SuspectIdentityFactSnapshot` changes from `(string Description, bool IsPrimary)` to `(FeatureLanguageSnapshot Language, bool IsPrimary)`. Old saves break; dev DB drop/recreate is acceptable per AGENTS.md.

**Test fixture impact:** 26 `new SuspectIdentityFact(string)` constructions in tests change to `new SuspectIdentityFact(FeatureLanguage.Raw(...))`. A `FeatureLanguage.Raw(hasForm, withForm, whoForm?)` factory provides a concise way to construct test-fixture feature languages without structured tokens.

### Task 2a: Add FeatureLanguage, FeatureDescriptor, and FeatureLanguageService to Domain

**Files:**
- Create: `src/WildBunch.Domain/Cases/FeatureLanguage.cs`
- Create: `src/WildBunch.Domain/Cases/FeatureLanguageService.cs`
- Test: `tests/WildBunch.Domain.Tests/FeatureLanguageServiceTests.cs` (create)

**Interfaces:**
- Produces: `FeatureLanguage` record, `FeatureDescriptor` record, `FeatureCategory` enum, `FeatureSide` enum, `FeatureLanguageService.For(FeatureDescriptor)` method, `FeatureLanguage.Raw(string, string, string?)` factory

- [ ] **Step 1: Write the failing test**

Create `tests/WildBunch.Domain.Tests/FeatureLanguageServiceTests.cs`:

```csharp
using WildBunch.Domain.Cases;

namespace WildBunch.Domain.Tests;

public sealed class FeatureLanguageServiceTests
{
    [Fact]
    public void LimpLeftLeg_ProducesAllForms()
    {
        var descriptor = new FeatureDescriptor(FeatureCategory.Limp, "leg", FeatureSide.Left);
        var language = FeatureLanguageService.For(descriptor);

        Assert.Equal("Has a limp in the left leg.", language.HasForm);
        Assert.Equal("a limp in the left leg", language.WithForm);
        Assert.Equal("has a limp in the left leg", language.WhoForm);
        Assert.Equal("The culprit walks with a limp in the left leg.", language.OpeningLeadForm);
    }

    [Fact]
    public void MissingRightEar_ProducesAllForms()
    {
        var descriptor = new FeatureDescriptor(FeatureCategory.MissingPart, "ear", FeatureSide.Right);
        var language = FeatureLanguageService.For(descriptor);

        Assert.Equal("Is missing the right ear.", language.HasForm);
        Assert.Equal("a missing right ear", language.WithForm);
        Assert.Equal("is missing the right ear", language.WhoForm);
        Assert.Equal("The culprit is missing the right ear.", language.OpeningLeadForm);
    }

    [Fact]
    public void ScarLeftCheek_ProducesAllForms()
    {
        var descriptor = new FeatureDescriptor(FeatureCategory.Scar, "cheek", FeatureSide.Left);
        var language = FeatureLanguageService.For(descriptor);

        Assert.Equal("Has a scar on the left cheek.", language.HasForm);
        Assert.Equal("a scar on the left cheek", language.WithForm);
        Assert.Equal("has a scar on the left cheek", language.WhoForm);
        Assert.Equal("The culprit has a scar on the left cheek.", language.OpeningLeadForm);
    }

    [Fact]
    public void NoEyebrows_ProducesAllForms()
    {
        var descriptor = new FeatureDescriptor(FeatureCategory.Absence, "eyebrows", FeatureSide.None);
        var language = FeatureLanguageService.For(descriptor);

        Assert.Equal("Has no eyebrows.", language.HasForm);
        Assert.Equal("no eyebrows", language.WithForm);
        Assert.Equal("has no eyebrows", language.WhoForm);
        Assert.Equal("The culprit has no eyebrows.", language.OpeningLeadForm);
    }

    [Fact]
    public void Raw_FactoryProducesExplicitForms()
    {
        var language = FeatureLanguage.Raw(
            "A pale scar cuts across the left cheek.",
            "a pale scar across the left cheek",
            "has a pale scar across the left cheek");

        Assert.Equal("A pale scar cuts across the left cheek.", language.HasForm);
        Assert.Equal("a pale scar across the left cheek", language.WithForm);
        Assert.Equal("has a pale scar across the left cheek", language.WhoForm);
        Assert.Null(language.OpeningLeadForm);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.Domain.Tests --filter FeatureLanguageServiceTests`
Expected: FAIL — types don't exist yet.

- [ ] **Step 3: Implement FeatureLanguage and FeatureLanguageService**

Create `src/WildBunch.Domain/Cases/FeatureLanguage.cs`:

```csharp
namespace WildBunch.Domain.Cases;

public enum FeatureCategory
{
    Limp = 0,
    MissingPart = 1,
    Scar = 2,
    Absence = 3
}

public enum FeatureSide
{
    None = 0,
    Left = 1,
    Right = 2
}

public sealed record FeatureDescriptor(FeatureCategory Category, string BodyPart, FeatureSide Side);

public sealed record FeatureLanguage(
    string HasForm,
    string WithForm,
    string WhoForm,
    string? OpeningLeadForm)
{
    /// <summary>
    /// Constructs a FeatureLanguage from explicit forms, for test fixtures
    /// and non-feature-pool identity facts that don't have structured tokens.
    /// </summary>
    public static FeatureLanguage Raw(string hasForm, string withForm, string? whoForm = null)
        => new(hasForm, withForm, whoForm ?? hasForm.ToLowerInvariant(), null);
}
```

Create `src/WildBunch.Domain/Cases/FeatureLanguageService.cs`:

```csharp
namespace WildBunch.Domain.Cases;

public static class FeatureLanguageService
{
    public static FeatureLanguage For(FeatureDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        return descriptor.Category switch
        {
            FeatureCategory.Limp => ForLimp(descriptor),
            FeatureCategory.MissingPart => ForMissingPart(descriptor),
            FeatureCategory.Scar => ForScar(descriptor),
            FeatureCategory.Absence => ForAbsence(descriptor),
            _ => throw new ArgumentOutOfRangeException(nameof(descriptor), descriptor.Category, "Unsupported feature category.")
        };
    }

    private static string Location(FeatureDescriptor d)
        => d.Side == FeatureSide.None ? d.BodyPart : $"{SideWord(d.Side)} {d.BodyPart}";

    private static string SideWord(FeatureSide side) => side switch
    {
        FeatureSide.Left => "left",
        FeatureSide.Right => "right",
        _ => string.Empty
    };

    private static FeatureLanguage ForLimp(FeatureDescriptor d)
    {
        var location = Location(d);
        return new FeatureLanguage(
            HasForm: $"Has a limp in the {location}.",
            WithForm: $"a limp in the {location}",
            WhoForm: $"has a limp in the {location}",
            OpeningLeadForm: $"The culprit walks with a limp in the {location}.");
    }

    private static FeatureLanguage ForMissingPart(FeatureDescriptor d)
    {
        var location = Location(d);
        return new FeatureLanguage(
            HasForm: $"Is missing the {location}.",
            WithForm: $"a missing {location}",
            WhoForm: $"is missing the {location}",
            OpeningLeadForm: $"The culprit is missing the {location}.");
    }

    private static FeatureLanguage ForScar(FeatureDescriptor d)
    {
        var location = Location(d);
        return new FeatureLanguage(
            HasForm: $"Has a scar on the {location}.",
            WithForm: $"a scar on the {location}",
            WhoForm: $"has a scar on the {location}",
            OpeningLeadForm: $"The culprit has a scar on the {location}.");
    }

    private static FeatureLanguage ForAbsence(FeatureDescriptor d)
        => new(
            HasForm: $"Has no {d.BodyPart}.",
            WithForm: $"no {d.BodyPart}",
            WhoForm: $"has no {d.BodyPart}",
            OpeningLeadForm: $"The culprit has no {d.BodyPart}.");
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/WildBunch.Domain.Tests --filter FeatureLanguageServiceTests`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/WildBunch.Domain/Cases/FeatureLanguage.cs src/WildBunch.Domain/Cases/FeatureLanguageService.cs tests/WildBunch.Domain.Tests/FeatureLanguageServiceTests.cs
git commit -m "BUNCH-118: add FeatureLanguage and FeatureLanguageService for structured feature text"
```

### Task 2b: Migrate SuspectIdentityFact to carry FeatureLanguage

**Files:**
- Modify: `src/WildBunch.Domain/Cases/SuspectProfile.cs` (line 3 — `SuspectIdentityFact` record)
- Modify: `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.Components.cs` (lines 404-411 — snapshot)
- Modify: `src/WildBunch.Domain/Game/GameSession.cs` (line 3895 — `CollectSuspectFeatureDescriptions`)
- Modify: `src/WildBunch.Application/Dev/Mapping/SaloonDevContextMapper.cs` (line 55)
- Test: update all 26 `new SuspectIdentityFact(string)` constructions in tests

**Interfaces:**
- Consumes: `FeatureLanguage` from Task 2a
- Produces: `SuspectIdentityFact(FeatureLanguage Language, bool IsPrimary)` — breaking change from `(string Description, bool IsPrimary)`

- [ ] **Step 1: Update SuspectIdentityFact record**

In `src/WildBunch.Domain/Cases/SuspectProfile.cs`, change:

```csharp
public readonly record struct SuspectIdentityFact(string Description, bool IsPrimary = true);
```

to:

```csharp
public readonly record struct SuspectIdentityFact(FeatureLanguage Language, bool IsPrimary = true);
```

- [ ] **Step 2: Update persistence snapshot**

In `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.Components.cs`, replace `SuspectIdentityFactSnapshot`:

```csharp
    private sealed record FeatureLanguageSnapshot(
        string HasForm,
        string WithForm,
        string WhoForm,
        string? OpeningLeadForm)
    {
        public static FeatureLanguageSnapshot FromDomain(FeatureLanguage language)
            => new(language.HasForm, language.WithForm, language.WhoForm, language.OpeningLeadForm);

        public static FeatureLanguage ToDomain(FeatureLanguageSnapshot snapshot)
            => new(snapshot.HasForm, snapshot.WithForm, snapshot.WhoForm, snapshot.OpeningLeadForm);
    }

    private sealed record SuspectIdentityFactSnapshot(FeatureLanguageSnapshot Language, bool IsPrimary)
    {
        public static SuspectIdentityFactSnapshot FromDomain(SuspectIdentityFact fact)
            => new(FeatureLanguageSnapshot.FromDomain(fact.Language), fact.IsPrimary);

        public static SuspectIdentityFact ToDomain(SuspectIdentityFactSnapshot snapshot)
            => new(FeatureLanguageSnapshot.ToDomain(snapshot.Language), snapshot.IsPrimary);
    }
```

- [ ] **Step 3: Update GameSession.CollectSuspectFeatureDescriptions**

In `src/WildBunch.Domain/Game/GameSession.cs` line ~3895, change `.Select(f => f.Description)` to `.Select(f => f.Language.HasForm)`:

```csharp
private IReadOnlyList<string> CollectSuspectFeatureDescriptions()
    => CaseFile.Suspects
        .SelectMany(s => s.Profile.IdentifyingFacts)
        .Where(f => f.IsPrimary)
        .Select(f => f.Language.HasForm)
        .Where(d => !string.IsNullOrWhiteSpace(d))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList();
```

- [ ] **Step 4: Update SaloonDevContextMapper**

In `src/WildBunch.Application/Dev/Mapping/SaloonDevContextMapper.cs` line 55, change `.Select(f => f.Description)` to `.Select(f => f.Language.HasForm)`:

```csharp
IdentifyingFacts: s.Profile.IdentifyingFacts.Select(f => f.Language.HasForm).ToList(),
```

- [ ] **Step 5: Update all test fixtures**

Search for all `new SuspectIdentityFact(` in tests (26 occurrences). Replace each string argument with `FeatureLanguage.Raw(...)`. Examples:

For `"Has a scar on the left cheek."`:
```csharp
new SuspectIdentityFact(FeatureLanguage.Raw("Has a scar on the left cheek.", "a scar on the left cheek", "has a scar on the left cheek"))
```

For `"A pale scar cuts across the left cheek."`:
```csharp
new SuspectIdentityFact(FeatureLanguage.Raw("A pale scar cuts across the left cheek.", "a pale scar across the left cheek", "has a pale scar across the left cheek"))
```

For `"a brass buckle with a cracked star engraving"`:
```csharp
new SuspectIdentityFact(FeatureLanguage.Raw("a brass buckle with a cracked star engraving", "a brass buckle with a cracked star engraving", "has a brass buckle with a cracked star engraving"))
```

For test fixtures in `CitizenCastTests.cs` that use feature strings:
```csharp
new SuspectIdentityFact(FeatureLanguage.Raw("Has a limp in the left leg.", "a limp in the left leg", "has a limp in the left leg")),
new SuspectIdentityFact(FeatureLanguage.Raw("Wears a distinctive earring in the left ear.", "a distinctive earring in the left ear", "wears a distinctive earring in the left ear")),
// etc.
```

Also update any test assertions that read `.Description` on `SuspectIdentityFact` to read `.Language.HasForm` instead. Key files:
- `tests/WildBunch.Domain.Tests/CaseProgressTests.cs` line 56: `suspect.Profile.IdentifyingFacts[0].Description` → `.Language.HasForm`
- `tests/WildBunch.GameContent.Tests/SeededNewGameFactoryTests.cs` lines 62-63: update assertions

- [ ] **Step 6: Build and run tests to verify compilation and find remaining breakage**

Run: `dotnet build`
Expected: Build may fail on remaining `.Description` references — fix them to `.Language.HasForm` or `.Language.WithForm` as appropriate for each consumer.

Run: `dotnet test`
Expected: Some tests may fail on assertion strings that need updating to match new `FeatureLanguage` forms. Fix assertions to match the explicit forms provided in test fixtures.

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "BUNCH-118: migrate SuspectIdentityFact to carry FeatureLanguage"
```

### Task 2c: Update CaseSuspectFeaturePool to store FeatureLanguage

**Files:**
- Modify: `src/WildBunch.GameContent/NewGame/CaseSuspectFeaturePool.cs` (lines 40-49 — record, lines 90-121 — feature definitions, lines 201-211 — BuildOpeningLead, lines 213-250 — factory methods)
- Modify: `src/WildBunch.GameContent/NewGame/SeedCaseBuilder.cs` (line 113 — CreateSuspect, lines 131-263 — clue anchors, lines 331-347 — DescribeFeatureClause/DescribeUnnamedRider/DescribePersonWithFeature)
- Modify: `src/WildBunch.GameContent/NewGame/CaseCharacterRoster.cs` (line 794 — Tokenize)
- Test: update `tests/WildBunch.GameContent.Tests/SeededNewGameFactoryTests.cs`, `tests/WildBunch.GameContent.Tests/CaseCharacterRosterTests.cs`

**Interfaces:**
- Consumes: `FeatureLanguage`, `FeatureDescriptor`, `FeatureLanguageService` from Task 2a
- Produces: `CaseSuspectFeatureProfile.Language` (replaces `.Description` and `.OpeningLeadText`)

- [ ] **Step 1: Update CaseSuspectFeatureProfile record**

In `src/WildBunch.GameContent/NewGame/CaseSuspectFeaturePool.cs`, change the record:

```csharp
internal sealed record CaseSuspectFeatureProfile(
    string Key,
    FeatureLanguage Language,
    CaseFeatureKind Kind,
    string FamilyKey,
    CaseFeatureSide Side,
    IReadOnlyList<CaseSuspectFeatureTag> Tags,
    IReadOnlyList<string> IncompatibleKeys,
    string SourceNote)
{
    public bool SupportsOpeningLead => HasTag(CaseSuspectFeatureTags.OpeningLeadCapable);

    public bool IsClassicNod => HasTag(CaseSuspectFeatureTags.ClassicNod);

    // ... existing HasTag and IsCompatibleWith methods unchanged ...
}
```

Remove the `Description` and `OpeningLeadText` fields. Existing internal consumers will use `Language.HasForm`, `Language.WhoForm`, `Language.OpeningLeadForm` directly.

- [ ] **Step 2: Update factory methods**

Update `NodFeature` to construct `FeatureLanguage` from a `FeatureDescriptor` via `FeatureLanguageService`:

```csharp
    private static CaseSuspectFeatureProfile NodFeature(
        string key,
        FeatureCategory category,
        string bodyPart,
        CaseFeatureSide side,
        IReadOnlyList<CaseSuspectFeatureTag> tags,
        string sourceNote,
        params string[] incompatibleKeys)
    {
        var featureSide = side switch
        {
            CaseFeatureSide.Left => FeatureSide.Left,
            CaseFeatureSide.Right => FeatureSide.Right,
            _ => FeatureSide.None
        };
        var descriptor = new FeatureDescriptor(category, bodyPart, featureSide);
        var language = FeatureLanguageService.For(descriptor);
        return new CaseSuspectFeatureProfile(
            key,
            language,
            CaseFeatureKind.PrimaryMarker,
            FamilyKey: bodyPart == "leg" ? "limp" : bodyPart == "ear" ? "ear" : bodyPart == "cheek" ? "cheek-scar" : "brow",
            side,
            tags,
            incompatibleKeys,
            sourceNote);
    }
```

Update `AccessoryFeature` to accept a `FeatureLanguage` directly (accessories have hand-written copy):

```csharp
    private static CaseSuspectFeatureProfile AccessoryFeature(
        string key,
        FeatureLanguage language,
        string familyKey,
        CaseFeatureSide side,
        IReadOnlyList<CaseSuspectFeatureTag> tags,
        string sourceNote,
        params string[] incompatibleKeys)
        => new(
            key,
            language,
            CaseFeatureKind.AccessoryMarker,
            familyKey,
            side,
            tags,
            incompatibleKeys,
            sourceNote);
```

- [ ] **Step 3: Update PrimaryFeatures array**

Replace the pre-baked sentence strings with structured calls:

```csharp
    private static readonly CaseSuspectFeatureProfile[] PrimaryFeatures =
    [
        NodFeature("limp-left-leg", FeatureCategory.Limp, "leg", CaseFeatureSide.Left, [CaseSuspectFeatureTags.PhysicalMarker, CaseSuspectFeatureTags.Gait, CaseSuspectFeatureTags.Visible, CaseSuspectFeatureTags.SideAware, CaseSuspectFeatureTags.Leg, CaseSuspectFeatureTags.ClassicNod, CaseSuspectFeatureTags.OpeningLeadCapable], "Original feature text; used to build the opening lead."),
        NodFeature("limp-right-leg", FeatureCategory.Limp, "leg", CaseFeatureSide.Right, [CaseSuspectFeatureTags.PhysicalMarker, CaseSuspectFeatureTags.Gait, CaseSuspectFeatureTags.Visible, CaseSuspectFeatureTags.SideAware, CaseSuspectFeatureTags.Leg, CaseSuspectFeatureTags.ClassicNod, CaseSuspectFeatureTags.OpeningLeadCapable], "Original feature text; used to build the opening lead."),
        NodFeature("no-left-ear", FeatureCategory.MissingPart, "ear", CaseFeatureSide.Left, [CaseSuspectFeatureTags.PhysicalMarker, CaseSuspectFeatureTags.MissingPart, CaseSuspectFeatureTags.Visible, CaseSuspectFeatureTags.SideAware, CaseSuspectFeatureTags.Ear, CaseSuspectFeatureTags.ClassicNod, CaseSuspectFeatureTags.OpeningLeadCapable], "Original feature text; used to build the opening lead.", "distinctive-left-earring"),
        NodFeature("no-right-ear", FeatureCategory.MissingPart, "ear", CaseFeatureSide.Right, [CaseSuspectFeatureTags.PhysicalMarker, CaseSuspectFeatureTags.MissingPart, CaseSuspectFeatureTags.Visible, CaseSuspectFeatureTags.SideAware, CaseSuspectFeatureTags.Ear, CaseSuspectFeatureTags.ClassicNod, CaseSuspectFeatureTags.OpeningLeadCapable], "Original feature text; used to build the opening lead.", "distinctive-right-earring"),
        NodFeature("scar-left-cheek", FeatureCategory.Scar, "cheek", CaseFeatureSide.Left, [CaseSuspectFeatureTags.PhysicalMarker, CaseSuspectFeatureTags.Scar, CaseSuspectFeatureTags.Visible, CaseSuspectFeatureTags.SideAware, CaseSuspectFeatureTags.Face, CaseSuspectFeatureTags.ClassicNod, CaseSuspectFeatureTags.OpeningLeadCapable], "Original feature text; used to build the opening lead."),
        NodFeature("scar-right-cheek", FeatureCategory.Scar, "cheek", CaseFeatureSide.Right, [CaseSuspectFeatureTags.PhysicalMarker, CaseSuspectFeatureTags.Scar, CaseSuspectFeatureTags.Visible, CaseSuspectFeatureTags.SideAware, CaseSuspectFeatureTags.Face, CaseSuspectFeatureTags.ClassicNod, CaseSuspectFeatureTags.OpeningLeadCapable], "Original feature text; used to build the opening lead."),
        NodFeature("no-eyebrows", FeatureCategory.Absence, "eyebrows", CaseFeatureSide.None, [CaseSuspectFeatureTags.PhysicalMarker, CaseSuspectFeatureTags.MissingPart, CaseSuspectFeatureTags.Visible, CaseSuspectFeatureTags.Face, CaseSuspectFeatureTags.ClassicNod, CaseSuspectFeatureTags.OpeningLeadCapable], "Original feature text; used to build the opening lead.")
    ];
```

- [ ] **Step 4: Update AccessoryFeatures array**

Each accessory gets a `FeatureLanguage` with hand-written forms. The `WithForm` is the noun phrase after "with" (strip the verb and lowercase the article). The `WhoForm` is the lowercase full sentence. Example for the first few:

```csharp
    private static readonly CaseSuspectFeatureProfile[] AccessoryFeatures =
    [
        AccessoryFeature("distinctive-left-earring",
            new FeatureLanguage("Wears a distinctive earring in the left ear.", "a distinctive earring in the left ear", "wears a distinctive earring in the left ear", null),
            "earring", CaseFeatureSide.Left, [CaseSuspectFeatureTags.Accessory, CaseSuspectFeatureTags.Wearable, CaseSuspectFeatureTags.Visible, CaseSuspectFeatureTags.SideAware, CaseSuspectFeatureTags.DistinctiveItem, CaseSuspectFeatureTags.Ear], "Original feature text.", "no-left-ear"),
        AccessoryFeature("distinctive-right-earring",
            new FeatureLanguage("Wears a distinctive earring in the right ear.", "a distinctive earring in the right ear", "wears a distinctive earring in the right ear", null),
            "earring", CaseFeatureSide.Right, [CaseSuspectFeatureTags.Accessory, CaseSuspectFeatureTags.Wearable, CaseSuspectFeatureTags.Visible, CaseSuspectFeatureTags.SideAware, CaseSuspectFeatureTags.DistinctiveItem, CaseSuspectFeatureTags.Ear], "Original feature text.", "no-right-ear"),
        AccessoryFeature("eyepatch-left",
            new FeatureLanguage("Wears an eyepatch over the left eye.", "an eyepatch over the left eye", "wears an eyepatch over the left eye", null),
            "eyepatch", CaseFeatureSide.Left, [CaseSuspectFeatureTags.Accessory, CaseSuspectFeatureTags.Wearable, CaseSuspectFeatureTags.Visible, CaseSuspectFeatureTags.SideAware, CaseSuspectFeatureTags.Eye, CaseSuspectFeatureTags.DistinctiveItem], "Original feature text."),
        // ... continue for all 18 accessories. Each gets:
        //   HasForm: the original Description string (with trailing period)
        //   WithForm: the noun phrase (strip leading verb, keep article, lowercase first letter)
        //   WhoForm: the original Description lowercased (no trailing period)
        //   OpeningLeadForm: null (accessories don't have opening leads)
    ];
```

Full list of accessory `FeatureLanguage` values (HasForm / WithForm / WhoForm):

| Key | HasForm | WithForm | WhoForm |
|-----|---------|----------|---------|
| distinctive-left-earring | Wears a distinctive earring in the left ear. | a distinctive earring in the left ear | wears a distinctive earring in the left ear |
| distinctive-right-earring | Wears a distinctive earring in the right ear. | a distinctive earring in the right ear | wears a distinctive earring in the right ear |
| eyepatch-left | Wears an eyepatch over the left eye. | an eyepatch over the left eye | wears an eyepatch over the left eye |
| eyepatch-right | Wears an eyepatch over the right eye. | an eyepatch over the right eye | wears an eyepatch over the right eye |
| cracked-gauntlet | Wears a cracked leather gauntlet on the right hand. | a cracked leather gauntlet on the right hand | wears a cracked leather gauntlet on the right hand |
| stitched-brim-hat | Prefers a sand-colored hat with the brim stitched flat. | a sand-colored hat with the brim stitched flat | prefers a sand-colored hat with the brim stitched flat |
| black-stained-cuff | Has a black-stained cuff on the left sleeve. | a black-stained cuff on the left sleeve | has a black-stained cuff on the left sleeve |
| split-finger-glove | Keeps a split-finger glove tucked into a coat pocket. | a split-finger glove tucked into a coat pocket | keeps a split-finger glove tucked into a coat pocket |
| silver-tooth | Has a silver tooth that catches the light when he smiles. | a silver tooth that catches the light when he smiles | has a silver tooth that catches the light when he smiles |
| copper-ribbon | Keeps a copper ribbon tied in her hair. | a copper ribbon tied in her hair | keeps a copper ribbon tied in her hair |
| rope-burn-scar | Carries a rope-burn scar on the left wrist. | a rope-burn scar on the left wrist | carries a rope-burn scar on the left wrist |
| faded-blue-scarf | Wears a faded blue scarf over a dark vest. | a faded blue scarf over a dark vest | wears a faded blue scarf over a dark vest |
| iron-rim-spectacles | Keeps iron-rim spectacles tucked into a coat pocket. | iron-rim spectacles tucked into a coat pocket | keeps iron-rim spectacles tucked into a coat pocket |
| dust-colored-duster | Wears a long dust-colored duster with a frayed hem. | a long dust-colored duster with a frayed hem | wears a long dust-colored duster with a frayed hem |
| brass-spur | Keeps a brass spur tucked into a coat pocket. | a brass spur tucked into a coat pocket | keeps a brass spur tucked into a coat pocket |
| tobacco-stained-gloves | Leaves tobacco-stained glove prints on ledgers and rail notices. | tobacco-stained glove prints on ledgers and rail notices | leaves tobacco-stained glove prints on ledgers and rail notices |
| copper-spur-ribbon | Keeps a brass spur tied to a faded blue sash. | a brass spur tied to a faded blue sash | keeps a brass spur tied to a faded blue sash |
| straw-hat | Wears a straw hat with the crown creased low. | a straw hat with the crown creased low | wears a straw hat with the crown creased low |

- [ ] **Step 5: Update BuildOpeningLead**

In `CaseSuspectFeaturePool.cs`, update `BuildOpeningLead` to use `Language.OpeningLeadForm`:

```csharp
    public static string BuildOpeningLead(CaseSuspectFeatureProfile feature)
    {
        ArgumentNullException.ThrowIfNull(feature);

        if (!feature.HasTag(CaseSuspectFeatureTags.OpeningLeadCapable) || string.IsNullOrWhiteSpace(feature.Language.OpeningLeadForm))
        {
            throw new InvalidOperationException($"Feature '{feature.Key}' does not support an opening lead.");
        }

        return feature.Language.OpeningLeadForm!;
    }
```

- [ ] **Step 6: Update SeedCaseBuilder**

In `src/WildBunch.GameContent/NewGame/SeedCaseBuilder.cs`:

**Line 113** — `CreateSuspect`: change `fact.Description` to `fact.Language`:

```csharp
    private static Suspect CreateSuspect(SuspectId id, CaseCharacterProfile profile, CaseSuspectFeatureAssignment feature)
        => new(
            id,
            profile.DisplayName,
            new SuspectProfile(profile.GameAliases, feature.AllFeatures.Select(fact => new SuspectIdentityFact(fact.Language, fact.Kind == CaseFeatureKind.PrimaryMarker))),
            profile.Traits,
            SuspectStatus.AtLarge);
```

**Lines 131-263** — clue anchors: change `culpritFeature.Description` and `features[n].PrimaryFeature.Description` to `.Language.HasForm`:

```csharp
// Line 134 example:
new ClueSubjectAnchor(culpritFeature.Language.HasForm, Feature: culpritFeature.Language.HasForm, Fact: "opening lead"),
```

Apply the same `.Language.HasForm` replacement to all `feature.Description` and `feature.PrimaryFeature.Description` references in clue anchor construction (lines 134, 167, 183, 199, 215, 243, 263).

**Lines 331-347** — eliminate `DescribeFeatureClause`, `DescribeUnnamedRider`, `DescribePersonWithFeature` and replace with direct `Language.WhoForm` usage:

```csharp
    private static string DescribeUnnamedRider(CaseSuspectFeatureProfile feature)
        => $"an unnamed rider who {feature.Language.WhoForm}";

    private static string DescribePersonWithFeature(CaseSuspectFeatureProfile feature, string person)
        => $"{person} who {feature.Language.WhoForm}";
```

Delete `DescribeFeatureClause` entirely — `Language.WhoForm` serves the same purpose without fragile string manipulation.

- [ ] **Step 7: Update CaseCharacterRoster**

In `src/WildBunch.GameContent/NewGame/CaseCharacterRoster.cs` line 794, change `openingLeadFeature.Description` to `openingLeadFeature.Language.HasForm`:

```csharp
        var openingLeadTokens = new HashSet<string>(
            Tokenize(openingLeadFeature.Language.HasForm).Where(token => token.Length > 3),
            StringComparer.OrdinalIgnoreCase);
```

- [ ] **Step 8: Update tests that reference CaseSuspectFeaturePool.Description**

In `tests/WildBunch.GameContent.Tests/SeededNewGameFactoryTests.cs`:
- Line 62: `culpritOpeningFeature.Description` → `culpritOpeningFeature.Language.HasForm`
- Line 63: `feature.Description == fact.Description` → `feature.Language.HasForm == fact.Language.HasForm`

In `tests/WildBunch.GameContent.Tests/CaseCharacterRosterTests.cs`:
- Line 144: `feature.Description` → `feature.Language.HasForm`

- [ ] **Step 9: Build and run tests**

Run: `dotnet build`
Expected: BUILD succeeds (all `.Description` and `.OpeningLeadText` references updated).

Run: `dotnet test`
Expected: PASS — test assertions on opening lead text ("The culprit has a scar on the left cheek.") should still match because `FeatureLanguageService` generates the same text. Clue text assertions that embed feature descriptions should also match because `Language.WhoForm` produces the same text as the old `DescribeFeatureClause` for all existing features.

- [ ] **Step 10: Commit**

```bash
git add -A
git commit -m "BUNCH-118: store FeatureLanguage on CaseSuspectFeatureProfile, eliminate DescribeFeatureClause"
```

### Task 2d: Eliminate NormalizeFeatureDescriptor from SaloonPersonOfInterestDescriptor

**Files:**
- Modify: `src/WildBunch.Domain/Cases/SaloonPersonOfInterestDescriptor.cs` (lines 7-83 — entire formatter)
- Test: `tests/WildBunch.Domain.Tests/SaloonPersonOfInterestDescriptorTests.cs` (create)
- Test: update `tests/WildBunch.Application.Tests/SaloonPersonOfInterestDescriptorParityTests.cs`

**Interfaces:**
- Consumes: `FeatureLanguage.WithForm` from `SuspectIdentityFact.Language`
- Produces: `SaloonPersonOfInterestDescriptor.Describe` returns grammatically correct descriptors for all feature types, with no string normalization

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
        var suspect = CreateSuspect(FeatureLanguage.Raw(
            "Is missing the right ear.", "a missing right ear", "is missing the right ear"));
        var caseFile = CreateCaseFile(suspect);
        var descriptor = SaloonPersonOfInterestDescriptor.Describe(suspect, caseFile);
        Assert.Equal("a stranger with a missing right ear", descriptor);
    }

    [Fact]
    public void Describe_LimpFeatureProducesGrammaticalDescriptor()
    {
        var suspect = CreateSuspect(FeatureLanguage.Raw(
            "Has a limp in the left leg.", "a limp in the left leg", "has a limp in the left leg"));
        var caseFile = CreateCaseFile(suspect);
        var descriptor = SaloonPersonOfInterestDescriptor.Describe(suspect, caseFile);
        Assert.Equal("a stranger with a limp in the left leg", descriptor);
    }

    [Fact]
    public void Describe_ScarFeatureProducesGrammaticalDescriptor()
    {
        var suspect = CreateSuspect(FeatureLanguage.Raw(
            "Has a scar on the left cheek.", "a scar on the left cheek", "has a scar on the left cheek"));
        var caseFile = CreateCaseFile(suspect);
        var descriptor = SaloonPersonOfInterestDescriptor.Describe(suspect, caseFile);
        Assert.Equal("a stranger with a scar on the left cheek", descriptor);
    }

    [Fact]
    public void Describe_AccessoryWithKeepsVerbProducesGrammaticalDescriptor()
    {
        var suspect = CreateSuspect(FeatureLanguage.Raw(
            "Keeps a split-finger glove tucked into a coat pocket.",
            "a split-finger glove tucked into a coat pocket",
            "keeps a split-finger glove tucked into a coat pocket"));
        var caseFile = CreateCaseFile(suspect);
        var descriptor = SaloonPersonOfInterestDescriptor.Describe(suspect, caseFile);
        Assert.Equal("a stranger with a split-finger glove tucked into a coat pocket", descriptor);
    }

    private static Suspect CreateSuspect(FeatureLanguage language)
        => new(
            new SuspectId("suspect-1"),
            "Mira Cline",
            new SuspectProfile(Array.Empty<SuspectAlias>(), new[] { new SuspectIdentityFact(language) }),
            SuspectTraits.Empty,
            SuspectStatus.AtLarge);

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

Run: `dotnet test tests/WildBunch.Domain.Tests --filter SaloonPersonOfInterestDescriptorTests`
Expected: FAIL — the current `FormatPublicDescriptor` still calls `NormalizeFeatureDescriptor` which mangles "Is missing the right ear." and "Keeps a split-finger glove...".

- [ ] **Step 3: Rewrite SaloonPersonOfInterestDescriptor**

Replace the entire `SaloonPersonOfInterestDescriptor` class in `src/WildBunch.Domain/Cases/SaloonPersonOfInterestDescriptor.cs`:

```csharp
namespace WildBunch.Domain.Cases;

public static class SaloonPersonOfInterestDescriptor
{
    public static string Describe(Suspect suspect, CaseFile caseFile)
    {
        ArgumentNullException.ThrowIfNull(suspect);
        ArgumentNullException.ThrowIfNull(caseFile);

        var warrantDescriptor = caseFile.KnownWarrants.FirstOrDefault(warrant => MatchesKnownWarrant(warrant, suspect));
        if (warrantDescriptor is not null)
        {
            var feature = warrantDescriptor.Terms.KnownFeatures.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(feature))
            {
                return $"a stranger with {TrimFeature(feature)}";
            }
        }

        var profileFact = suspect.Profile.IdentifyingFacts.FirstOrDefault();
        if (profileFact.Language is not null && !string.IsNullOrWhiteSpace(profileFact.Language.WithForm))
        {
            return $"a stranger with {profileFact.Language.WithForm}";
        }

        var traitDescriptor = suspect.Traits.Tags.FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(traitDescriptor.Value))
        {
            return $"a stranger who is {FormatTraitDescriptor(traitDescriptor.Value)}";
        }

        return "an unfamiliar person";
    }

    /// <summary>
    /// Trims trailing punctuation from warrant feature strings (noun phrases like "Raven-feather pin").
    /// Warrant features are not structured FeatureLanguage; they are plain strings from the warrant pool.
    /// </summary>
    private static string TrimFeature(string feature)
        => feature.Trim().TrimEnd('.', '!', '?');

    private static string FormatTraitDescriptor(string traitTag)
        => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(traitTag.Trim().Replace('-', ' '));

    private static string FormatPublicTraitDescriptor(string descriptor)
        => $"a stranger who is {descriptor.ToLowerInvariant()}";

    private static bool MatchesKnownWarrant(Warrant warrant, Suspect targetSuspect)
    {
        ArgumentNullException.ThrowIfNull(warrant);
        ArgumentNullException.ThrowIfNull(targetSuspect);

        if (string.Equals(warrant.TargetName, targetSuspect.Name, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return warrant.Terms.KnownAliases.Any(alias => string.Equals(alias, targetSuspect.Name, StringComparison.OrdinalIgnoreCase));
    }
}
```

Key changes:
- `FormatPublicDescriptor` and `NormalizeFeatureDescriptor` are **eliminated**
- Profile fact path uses `profileFact.Language.WithForm` directly — no normalization
- Warrant feature path uses simple `TrimFeature` (trailing punctuation only) — warrant features are noun phrases, not sentences
- `TrimDescriptor` is eliminated (was only used by `FormatPublicDescriptor`)

Note: `FormatTraitDescriptor` and `FormatPublicTraitDescriptor` remain for the trait fallback path, which is unchanged.

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/WildBunch.Domain.Tests --filter SaloonPersonOfInterestDescriptorTests`
Expected: PASS

- [ ] **Step 5: Update parity tests**

In `tests/WildBunch.Application.Tests/SaloonPersonOfInterestDescriptorParityTests.cs`, update the test fixtures to use `FeatureLanguage.Raw` for `SuspectIdentityFact`:

```csharp
// Line 47: profile-based POI
new[] { new SuspectIdentityFact(FeatureLanguage.Raw(
    "a brass buckle with a cracked star engraving",
    "a brass buckle with a cracked star engraving",
    "has a brass buckle with a cracked star engraving")) })

// Line 30: warrant-based POI — warrant KnownFeatures stay as strings, no change needed
new[] { "Has a scar on the left cheek." }  // warrant features are still strings
```

The warrant-based test (line 38) asserts `"a stranger with a scar on the left cheek"` — this still passes because warrant features go through `TrimFeature` which just strips the period, producing "a stranger with Has a scar on the left cheek".

Wait — that's wrong. Warrant features are full sentences like "Has a scar on the left cheek." but the expected output is "a stranger with a scar on the left cheek" (without "Has"). The old `NormalizeFeatureDescriptor` stripped "has a " → "a ". With the new code, `TrimFeature` only strips punctuation, so it would produce "a stranger with Has a scar on the left cheek" — which breaks the test.

**Resolution:** Warrant `KnownFeatures` that are full sentences need the same treatment. Two options:
1. Change the warrant feature strings to noun phrases ("a scar on the left cheek" instead of "Has a scar on the left cheek.")
2. Keep a minimal normalization for warrant features only

Option 1 is cleaner. The warrant features in `CaseCharacterRoster` are currently: "Raven-feather pin", "Black felt hat", "Split-finger glove" — these are already noun phrases. But the test fixture in `SaloonPersonOfInterestDescriptorParityTests` uses `"Has a scar on the left cheek."` as a warrant feature, which is a full sentence.

**Decision:** Warrant `KnownFeatures` should be noun phrases, not full sentences. Update the test fixture to use `"a scar on the left cheek"` instead of `"Has a scar on the left cheek."`. This is consistent with the real warrant features ("Raven-feather pin", etc.) and eliminates the need for normalization on the warrant path too.

Update `SaloonPersonOfInterestDescriptorParityTests.cs` line 30:
```csharp
new[] { "a scar on the left cheek" },  // was "Has a scar on the left cheek."
```

The assertion at line 38 stays: `AssertDescriptorParity(session, "a stranger with a scar on the left cheek")` — now it matches because `TrimFeature("a scar on the left cheek")` = "a scar on the left cheek".

- [ ] **Step 6: Run all descriptor and prologue tests**

Run: `dotnet test tests/WildBunch.Domain.Tests --filter SaloonPersonOfInterestDescriptorTests && dotnet test tests/WildBunch.Application.Tests --filter "SaloonPersonOfInterestDescriptorParityTests|PrologueHandlerTests"`
Expected: PASS

- [ ] **Step 7: Run full test suite to find remaining breakage**

Run: `dotnet test`
Expected: Some tests may fail if they assert on descriptor output for warrant features that were full sentences. Fix by changing warrant feature strings to noun phrases in test fixtures. Key files to check:
- `tests/WildBunch.Domain.Tests/GameSessionSaloonPersonOfInterestTests.cs` — assertions on "a stranger with Raven-feather pin" (should still pass — "Raven-feather pin" is already a noun phrase)
- `tests/WildBunch.Domain.Tests/GameSessionSaloonWantedSuspectLoopTests.cs` — same
- `tests/WildBunch.Integration.Tests/GameSessionDifficultyPersistenceTests.cs` — assertions on "a stranger with a limp in the left leg" (should pass — comes from profile fact `Language.WithForm`)

- [ ] **Step 8: Commit**

```bash
git add -A
git commit -m "BUNCH-118: eliminate NormalizeFeatureDescriptor, use FeatureLanguage.WithForm directly"
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

Run: `dotnet test tests/WildBunch.Domain.Tests --filter HorseFeedDisplayNamesAreDisambiguatedByVendor`
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

Run: `dotnet test tests/WildBunch.Domain.Tests --filter TownStoreCatalogResolverTests`
Expected: PASS

- [ ] **Step 5: Run purchase and store-offers tests to verify no regressions**

Run: `dotnet test tests/WildBunch.Application.Tests --filter "PurchaseStoreItemHandlerTests|GetTownStoreOffersHandlerTests"`
Expected: PASS — these tests assert on `ItemKind` and `VendorType`, not on the exact `DisplayName` string. If any test asserts the old `"Horse feed"` display name, update it to the new disambiguated name.

- [ ] **Step 6: Commit**

```bash
git add src/WildBunch.Domain/Economy/TownStoreCatalogModels.cs tests/WildBunch.Domain.Tests/TownStoreCatalogResolverTests.cs
git commit -m "BUNCH-118: disambiguate duplicate Horse feed display names by vendor"
```

---

## Issue 2 closeout: snapshot shape migration + falsification

Issue 2 changes `SuspectIdentityFactSnapshot` from `(string Description, bool IsPrimary)` to `(FeatureLanguageSnapshot Language, bool IsPrimary)`. This is an intentional, breaking snapshot shape change. Per AGENTS.md, current mainline model correctness wins over old-save compatibility and dev database drop/recreate is allowed. This section is the explicit verification that the break is real, expected, and cleanly handled — not silently swallowed.

### Task 2e: Verify snapshot shape break + dev reset

**Files:**
- Read-only verification: `src/WildBunch.Persistence/.../SuspectIdentityFactSnapshot.cs` (or equivalent codec)
- Read-only verification: `tests/WildBunch.Integration.Tests/` (snapshot round-trip tests)

- [ ] **Step 1: Confirm the new snapshot shape is in place**

Run: `grep -rn "FeatureLanguageSnapshot" src/WildBunch.Persistence`
Expected: At least one hit in the snapshot codec that defines/uses `FeatureLanguageSnapshot` for `SuspectIdentityFactSnapshot.Language`.

- [ ] **Step 2: Falsify old-save deserialization (intentional break proof)**

Write a temporary test (or add a `[Fact]` to an existing persistence test project) that attempts to deserialize a JSON payload with the OLD shape (`"Description": "...", "IsPrimary": true` and no `Language` field) into the new `SuspectIdentityFactSnapshot`. Expected: deserialization FAILS (throws or produces a `null`/default `Language`). This proves the break is real and not silently masked by a compatibility shim.

If deserialization silently succeeds by defaulting `Language` to null/empty, that means a compatibility shim was accidentally introduced — remove it and re-run. Per AGENTS.md: "Do not add compatibility shims for obsolete old saves or internal models unless Harley explicitly asks for one."

Delete the temporary test after it confirms the break (or keep it as a regression guard if it cleanly asserts the throw — worker's discretion, but do NOT keep a test that passes by silently accepting old saves).

- [ ] **Step 3: Run EF migrations list to confirm no schema drift**

Run: `dotnet tool restore && dotnet ef migrations list --project src/WildBunch.Persistence --startup-project src/WildBunch.Api`
Expected: Lists current migrations without error. Runtime session persistence is JSON snapshot-oriented (not table-shaped for suspect identity), so no new migration is expected. If a migration is unexpectedly required, STOP and report — that indicates the snapshot codec leaked into table shape.

- [ ] **Step 4: Reset dev database if integration tests fail on stale snapshot data**

Run: `.\scripts\postgres-dev.ps1 ensure`
Then: `.\scripts\postgres-dev.ps1 test -- dotnet test tests/WildBunch.Integration.Tests`
Expected: PASS. If integration tests fail with deserialization errors on `SuspectIdentityFactSnapshot` (stale rows from the old shape), run `.\scripts\postgres-dev.ps1 reset` and re-run. The reset is the sanctioned path per AGENTS.md.

- [ ] **Step 5: Confirm no compatibility shim was added**

Run: `grep -rn "Description" src/WildBunch.Persistence | grep -i "SuspectIdentity"` (or equivalent)
Expected: No leftover `Description` field on `SuspectIdentityFactSnapshot` and no `[Obsolete]`/`JsonExtensionData`/`[JsonInclude]` shim that maps the old `Description` string onto the new `Language` field. The old field must be fully removed, not layered over.

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
- Prologue grammar malformed → Tasks 2a-2d (structured features + language service, eliminates fragile normalization) + Task 2e (snapshot shape break verification + dev reset) ✓
- Duplicate "Horse feed" names → Task 3 ✓
- Validation: `dotnet test`, `npm test`, manual playtest → Final validation steps ✓ (manual playtest is issue-listed but not automatable here; the unit/integration tests cover the behavioral assertions)
- Snapshot shape migration + falsification → Task 2e explicitly verifies the break, confirms no compatibility shim, and runs the sanctioned dev-database reset path ✓
- "Store language variants with each and construct at runtime" → `FeatureLanguage` record stores all context forms; `FeatureLanguageService` generates them from structured `FeatureDescriptor` tokens for primary markers ✓
- "Don't paper over it, do a full and proper fix" → `NormalizeFeatureDescriptor` eliminated entirely, not patched ✓

**2. Placeholder scan:** The accessory FeatureLanguage table in Task 2c lists all 18 accessories with explicit HasForm/WithForm/WhoForm values — no "etc." or "continue for all" without the actual data. The code blocks contain complete, runnable code. The only abbreviated section is the accessory array in Step 4 which shows the first 3 entries inline and provides the full table above for the remaining 15.

**3. Type consistency:**
- `FeatureLanguage` — defined in Task 2a, consumed in Tasks 2b, 2c, 2d. Fields: `HasForm`, `WithForm`, `WhoForm`, `OpeningLeadForm` (nullable).
- `FeatureDescriptor` — defined in Task 2a, consumed in Task 2c. Fields: `Category`, `BodyPart`, `Side`.
- `SuspectIdentityFact` — changes from `(string Description, bool IsPrimary)` to `(FeatureLanguage Language, bool IsPrimary)` in Task 2b. All consumers updated in Tasks 2b-2d.
- `CaseSuspectFeatureProfile` — `Description` and `OpeningLeadText` fields replaced by `Language` in Task 2c. All consumers updated.
- `SaloonPersonOfInterestDescriptor.Describe(Suspect, CaseFile)` — signature unchanged; internal implementation rewritten in Task 2d.
- `StoreOffer` record — unchanged; only `DisplayName` string values change in Task 3.
- `CorsOptions.GetPolicy("ViteDevClient")` — standard ASP.NET Core CORS API.
- `FeatureLanguage.Raw(hasForm, withForm, whoForm?)` — factory used in test fixtures; produces `FeatureLanguage` with `OpeningLeadForm = null`.

**4. Persistence impact:**
- `SuspectIdentityFactSnapshot` changes from `(string Description, bool IsPrimary)` to `(FeatureLanguageSnapshot Language, bool IsPrimary)`.
- Old JSON saves will fail to deserialize. Per AGENTS.md: "In this greenfield repo, current mainline model correctness wins over old-save or legacy internal compatibility" and "Dev database drop/recreate is allowed."
- `ClueSubjectAnchorSnapshot` is unchanged — clue anchors still store `Feature` as a string (the `HasForm`).
- Task 2e is the explicit closeout that falsifies old-save deserialization, confirms no compatibility shim, checks EF migrations for schema drift, and runs the sanctioned `.\scripts\postgres-dev.ps1 reset` if integration tests fail on stale data.

**5. Scope discipline check:**
- The language service refactor is explicitly requested by the user ("do a full and proper fix").
- No unrelated refactors — CORS and Horse feed fixes are independent.
- The `FeatureLanguage` types are in the Domain layer where `SuspectIdentityFact` and `SaloonPersonOfInterestDescriptor` live.
- Accessory copy is preserved verbatim — only the storage shape changes (from one string to a `FeatureLanguage` record with pre-written forms).
