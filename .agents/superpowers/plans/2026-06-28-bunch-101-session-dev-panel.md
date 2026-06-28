# Session Dev Panel Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a `session-dev` dev overlay panel that owns game/session-level setup and control state — session identity/status/phase, seed/setup posture (RNG salt), difficulty/entropy inspection, and a safe high-level scenario setup control (lock/clear the RNG salt) — without implementing BUNCH-93 entropy semantics or BUNCH-94 difficulty semantics.

**Architecture:** The panel registers through the existing `DevPanelRegistry` and is available on all gameplay surfaces (it is not a surface owner, so it never displaces Saloon/Travel defaults). It fetches a new `SessionDevContextDto` from a guarded `/api/dev/sessions/{id}/session-context` query endpoint and dispatches dev commands to `/api/dev/sessions/{id}/session/lock-rng` and `/api/dev/sessions/{id}/session/clear-rng`. Dev commands flow through Application handlers that load `GameSession` via the repository, invoke new aggregate command methods (`ForceDevSaltSource` / `ClearDevSaltSource`), and produce typed domain events (`DevSaltSourceForced` / `DevSaltSourceCleared`) that are part of the event stream. The `SaltSource` (RNG posture) is already persisted in the session snapshot, so rehydration after a salt change requires no new persistence shape. Difficulty and entropy are inspection-only in this slice; their mutation semantics are owned by BUNCH-94 and BUNCH-93.

**Tech Stack:** C#/.NET 10, ASP.NET Core Minimal APIs, EF Core, xUnit, React 18, TanStack Query, styled-components, Vitest.

## Global Constraints

- `GameSession` is the live-play aggregate root; all gameplay mutation flows through it.
- Typed domain events are plain sealed records implementing `IDomainEvent`; `Apply` is the single mutation path.
- Dev endpoints live under `/api/dev/` and are gated by `DevRoleGuard.EnsureDevAccess()`.
- Dev DTOs are separate types from player DTOs (per ADR-0030 §7).
- Normal player APIs must remain clean of dev-only state and must not gain dev mutation powers.
- Do not implement BUNCH-94 difficulty semantics or BUNCH-93 entropy semantics. Difficulty and entropy are inspection-only here.
- Do not replace Session Audit's read-heavy event/log role.
- Do not turn Session dev into a universal editor for player, travel, saloon, casefile, suspect, inventory, or final gameplay outcomes.
- Do not force normal gameplay actions or final gameplay outcomes (dev-overlay doctrine §1 state/action boundary).
- The RNG salt lock sets up reproducibility state; it does not force any encounter result. Normal gameplay still resolves encounters through existing rules.
- The original game-start UUID seed code is not retained on the live `GameSession` (it is consumed at `StartNew` to derive world/difficulty/entropy/salt). Session dev must say this honestly rather than fabricate a seed code.
- The `SeedWorldVariant` is not retained on the `World` domain model after construction. Session dev shows what the session actually retains (current town, difficulty, entropy, salt posture) and does not invent a variant field.
- Worker environment uses PowerShell; do not use `&&` for command chaining.
- Run `.\scripts\postgres-dev.ps1 ensure` before PostgreSQL-dependent validation.
- styled-components for component styling; reference design tokens via `var(--token-name)`. No plain CSS classes.
- Expanded mode must use width (cards/columns), not a tall single column (dev-overlay doctrine §4).

---

## File Structure

### Domain layer (src/WildBunch.Domain/)

| File | Responsibility |
|------|----------------|
| `Events/DevSaltSourceForced.cs` | New typed domain event: dev locked the RNG to a fixed salt |
| `Events/DevSaltSourceCleared.cs` | New typed domain event: dev restored runtime RNG |
| `Game/GameSession.cs` (modify) | Add `ForceDevSaltSource(SaltSource)` / `ClearDevSaltSource()` command methods, `Apply(DevSaltSourceForced)` / `Apply(DevSaltSourceCleared)` methods |
| `Game/GameSessionEventReplay.cs` (modify) | Add dev salt event cases to `ApplyEvent` switch |
| `Game/GameSession.cs` `ApplyProducedEvent` (modify) | Add dev salt event cases to the produce-time dispatch switch |

### Application layer (src/WildBunch.Application/)

| File | Responsibility |
|------|----------------|
| `Dev/Models/SessionDevContextDto.cs` | New dev DTO: session identity, status, phase/clock, current town, difficulty, entropy, salt posture |
| `Dev/Models/LockRngRequestDto.cs` | New dev DTO: request shape for locking RNG (optional explicit salt; if absent, generate a fresh fixed salt) |
| `Dev/Queries/GetSessionDevContextQuery.cs` | New query record |
| `Dev/Queries/GetSessionDevContextHandler.cs` | New query handler: loads session, maps dev context |
| `Dev/Commands/ForceDevSaltSourceCommand.cs` | New command record |
| `Dev/Commands/ForceDevSaltSourceHandler.cs` | New command handler: load → aggregate command → store → commit |
| `Dev/Commands/ClearDevSaltSourceCommand.cs` | New command record |
| `Dev/Commands/ClearDevSaltSourceHandler.cs` | New command handler: load → aggregate command → store → commit |
| `Dev/Mapping/SessionDevContextMapper.cs` | New mapper: domain session → dev DTO (separate from player mappers) |

### API layer (src/WildBunch.Api/)

| File | Responsibility |
|------|----------------|
| `Dev/DevEndpoints.cs` (modify) | Add `session-context` GET, `session/lock-rng` POST, `session/clear-rng` POST endpoints |
| `DependencyInjection.cs` (modify) | Register the three new dev handlers in the DI container |

### Frontend (src/WildBunch.Web/src/)

| File | Responsibility |
|------|----------------|
| `dev/panels/SessionDevPanel.tsx` | New panel: inspect session state + lock/clear RNG controls, compact and expanded layouts |
| `dev/devApi.ts` (modify) | Add `getSessionDevContext`, `lockRng`, `clearRng` client functions |
| `dev/types.ts` (modify) | Add `SessionDevContextDto`, `LockRngRequestDto` TypeScript types |
| `dev/DevPanelRegistry.tsx` (modify) | Register `session-dev` panel (no `surfaces` filter, not a surface owner) |
| `tests/SessionDevPanel.test.tsx` | New Vitest suite: rendering, no-session, lock/clear command dispatch |

### Tests (backend)

| File | Responsibility |
|------|----------------|
| `tests/WildBunch.Application.Tests/Dev/GetSessionDevContextHandlerTests.cs` | New handler unit tests: inspection DTO shape, salt posture, difficulty/entropy read |
| `tests/WildBunch.Application.Tests/Dev/ForceDevSaltSourceHandlerTests.cs` | New handler unit tests: lock RNG produces event + persists salt |
| `tests/WildBunch.Application.Tests/Dev/ClearDevSaltSourceHandlerTests.cs` | New handler unit tests: clear RNG restores runtime mode |
| `tests/WildBunch.Domain.Tests/DevSaltSourceTests.cs` | New aggregate unit tests: Force/ClearDevSaltSource + Apply round-trip |
| `tests/WildBunch.Integration.Tests/Dev/DevSessionEndpointTests.cs` | New integration tests: 200/403/404, lock/clear round-trip, normal API boundary unchanged |

### Plans / mesh

| File | Responsibility |
|------|----------------|
| `.agents/superpowers/plans/2026-06-28-bunch-101-session-dev-panel.md` (this file) | Plan record; checkboxes updated as work lands |
| `.agents/superpowers/plans/INDEX.md` (generated) | Regenerated by `scripts/generate_index_mesh.py` after this file is committed |

---

## Task 1: Domain — DevSaltSource events + aggregate command methods

**Files:**
- Create: `src/WildBunch.Domain/Events/DevSaltSourceForced.cs`
- Create: `src/WildBunch.Domain/Events/DevSaltSourceCleared.cs`
- Modify: `src/WildBunch.Domain/Game/GameSession.cs` (add command methods + Apply methods + ApplyProducedEvent cases)
- Modify: `src/WildBunch.Domain/Game/GameSessionEventReplay.cs` (add event cases to ApplyEvent switch)
- Test: `tests/WildBunch.Domain.Tests/DevSaltSourceTests.cs`

**Interfaces:**
- Consumes: `WildBunch.Domain.Travel.SaltSource` (existing record `SaltSource(SaltSourceMode Mode, string Salt)` with `CreateRuntime()` / `CreateFixed(string)`).
- Produces: `GameSession.ForceDevSaltSource(SaltSource saltSource)`, `GameSession.ClearDevSaltSource()`, `GameSession.Apply(DevSaltSourceForced)`, `GameSession.Apply(DevSaltSourceCleared)`, events `DevSaltSourceForced(Guid GameSessionId, SaltSource SaltSource)`, `DevSaltSourceCleared(Guid GameSessionId)`.

- [ ] **Step 1: Write the failing aggregate test**

Create `tests/WildBunch.Domain.Tests/DevSaltSourceTests.cs`. Use the existing `SeededNewGameFactory` to build a session through the seed system (per AGENTS.md UUID seed codec rules — do not bypass the seed system for session construction). Derive a seed UUID via `StartingWorldDescriptorResolver.CreateRepresentativeSeedCode(descriptor)` from a descriptor.

```csharp
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.GameContent.NewGame;
using WildBunch.Domain.Tests.TestDoubles; // adjust to actual test-double namespace used by DevSaloonOverrideTests

namespace WildBunch.Domain.Tests;

public sealed class DevSaltSourceTests
{
    [Fact]
    public void ForceDevSaltSource_SetsFixedSaltAndProducesEvent()
    {
        var session = SeededSessionFactory.Build(); // helper added in Step 3
        session.MarkEventsCommitted();

        var fixedSalt = SaltSource.CreateFixed("deadbeef");
        session.ForceDevSaltSource(fixedSalt);

        Assert.Equal(SaltSourceMode.Fixed, session.SaltSource.Mode);
        Assert.Equal("deadbeef", session.SaltSource.Salt);
        Assert.Contains(session.UncommittedEvents, e => e is DevSaltSourceForced);
    }

    [Fact]
    public void ClearDevSaltSource_RestoresRuntimeModeAndProducesEvent()
    {
        var session = SeededSessionFactory.Build();
        session.ForceDevSaltSource(SaltSource.CreateFixed("deadbeef"));
        session.MarkEventsCommitted();

        session.ClearDevSaltSource();

        Assert.Equal(SaltSourceMode.Runtime, session.SaltSource.Mode);
        Assert.Contains(session.UncommittedEvents, e => e is DevSaltSourceCleared);
    }

    [Fact]
    public void Apply_DevSaltSourceForced_RestoresSaltOnReplay()
    {
        var session = SeededSessionFactory.Build();
        var forced = new DevSaltSourceForced(session.Id.Value, SaltSource.CreateFixed("cafe"));
        session.Apply(forced);
        Assert.Equal(SaltSourceMode.Fixed, session.SaltSource.Mode);
        Assert.Equal("cafe", session.SaltSource.Salt);
    }
}
```

Note: `UncommittedEvents` / `MarkEventsCommitted` are the existing aggregate event hooks used by `DevSaloonOverrideTests.cs` — match the exact names exposed there. If `UncommittedEvents` is not public, use the same reflection/inspection pattern `DevSaloonOverrideTests` uses.

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.Domain.Tests --filter "FullyQualifiedName~DevSaltSourceTests"`
Expected: FAIL with "ForceDevSaltSource not defined" / "DevSaltSourceForced not found".

- [ ] **Step 3: Add the SeededSessionFactory test helper (if not already present)**

Inspect `tests/WildBunch.Domain.Tests/DevSaloonOverrideTests.cs` for the exact session-construction helper it uses. If a shared `SeededSessionFactory.Build()` (or equivalent) already exists, reuse it. If not, add a small helper in the test project that builds a session via `SeededNewGameFactory` + a descriptor-derived UUID (do not store a UUID in the fixture; derive it on the fly via `StartingWorldDescriptorResolver.CreateRepresentativeSeedCode`). Match the construction pattern used by `DevSaloonOverrideTests.cs` to stay consistent.

- [ ] **Step 4: Create the DevSaltSourceForced event**

Create `src/WildBunch.Domain/Events/DevSaltSourceForced.cs`:

```csharp
using WildBunch.Domain.Events;
using WildBunch.Domain.Travel;

namespace WildBunch.Domain.Events;

public sealed record DevSaltSourceForced(Guid GameSessionId, SaltSource SaltSource) : IDomainEvent
{
    Guid IDomainEvent.GameSessionId => GameSessionId;
}
```

Match the exact `IDomainEvent` implementation pattern used by `DevSaloonOverrideForced.cs` (read that file first and mirror its shape — some events use a base class or explicit interface implementation).

- [ ] **Step 5: Create the DevSaltSourceCleared event**

Create `src/WildBunch.Domain/Events/DevSaltSourceCleared.cs`, mirroring `DevSaloonOverrideCleared.cs`:

```csharp
using WildBunch.Domain.Events;

namespace WildBunch.Domain.Events;

public sealed record DevSaltSourceCleared(Guid GameSessionId) : IDomainEvent
{
    Guid IDomainEvent.GameSessionId => GameSessionId;
}
```

- [ ] **Step 6: Add aggregate command + Apply methods to GameSession.cs**

In `src/WildBunch.Domain/Game/GameSession.cs`, near the existing `ForceDevSaloonOverride` / `ClearDevSaloonOverride` methods (search for `ForceDevSaloonOverride` to find the exact region), add:

```csharp
/// <summary>
/// Dev command: lock the RNG to a fixed salt for reproducible playtesting.
/// Sets up reproducibility state; does not force any encounter outcome.
/// Per dev-overlay doctrine §1 (state/action boundary).
/// </summary>
internal void ForceDevSaltSource(SaltSource saltSource)
{
    ArgumentNullException.ThrowIfNull(saltSource);
    if (saltSource.Mode != SaltSourceMode.Fixed)
    {
        throw new ArgumentException("ForceDevSaltSource requires a Fixed salt source.", nameof(saltSource));
    }
    ProduceEvent(new DevSaltSourceForced(Id.Value, saltSource));
}

/// <summary>
/// Dev command: restore runtime RNG.
/// </summary>
internal void ClearDevSaltSource()
{
    ProduceEvent(new DevSaltSourceCleared(Id.Value));
}

/// <summary>
/// Applies a DevSaltSourceForced event. Replaces the RNG salt posture.
/// </summary>
internal void Apply(DevSaltSourceForced e)
{
    SaltSource = e.SaltSource;
}

/// <summary>
/// Applies a DevSaltSourceCleared event. Restores runtime RNG.
/// </summary>
internal void Apply(DevSaltSourceCleared e)
{
    SaltSource = SaltSource.CreateRuntime();
}
```

- [ ] **Step 7: Add produce-time dispatch cases to ApplyProducedEvent**

In `src/WildBunch.Domain/Game/GameSession.cs`, in the `ApplyProducedEvent(IDomainEvent e)` switch (search for `case DevSaloonOverrideForced dsf:` to find the region), add:

```csharp
case DevSaltSourceForced dsf:
    Apply(dsf);
    break;
case DevSaltSourceCleared dsc:
    Apply(dsc);
    break;
```

- [ ] **Step 8: Add replay cases to GameSessionEventReplay.cs**

In `src/WildBunch.Domain/Game/GameSessionEventReplay.cs`, in the `ApplyEvent` switch (search for `case DevSaloonOverrideForced dsf:`), add:

```csharp
case DevSaltSourceForced dsf:
    Apply(dsf);
    break;
case DevSaltSourceCleared dsc:
    Apply(dsc);
    break;
```

- [ ] **Step 9: Run the aggregate tests to verify they pass**

Run: `dotnet test tests/WildBunch.Domain.Tests --filter "FullyQualifiedName~DevSaltSourceTests"`
Expected: PASS (3 tests).

- [ ] **Step 10: Run full domain test suite to verify no regressions**

Run: `dotnet test tests/WildBunch.Domain.Tests`
Expected: PASS, no regressions.

- [ ] **Step 11: Commit**

```bash
git add src/WildBunch.Domain/Events/DevSaltSourceForced.cs src/WildBunch.Domain/Events/DevSaltSourceCleared.cs src/WildBunch.Domain/Game/GameSession.cs src/WildBunch.Domain/Game/GameSessionEventReplay.cs tests/WildBunch.Domain.Tests/DevSaltSourceTests.cs
git commit -m "feat(domain): add DevSaltSource forced/cleared events and aggregate commands for BUNCH-101"
```

---

## Task 2: Application — SessionDevContext query (inspection DTO + handler + mapper)

**Files:**
- Create: `src/WildBunch.Application/Dev/Models/SessionDevContextDto.cs`
- Create: `src/WildBunch.Application/Dev/Queries/GetSessionDevContextQuery.cs`
- Create: `src/WildBunch.Application/Dev/Queries/GetSessionDevContextHandler.cs`
- Create: `src/WildBunch.Application/Dev/Mapping/SessionDevContextMapper.cs`
- Test: `tests/WildBunch.Application.Tests/Dev/GetSessionDevContextHandlerTests.cs`

**Interfaces:**
- Consumes: `IGameSessionRepository` (existing), `GameSession` properties: `Id`, `Status`, `GameDifficulty`, `GameEntropy`, `SaltSource`, `Clock` (Day/Turn/TimeOfDay), `CurrentTown` (TownId/TownName), `CurrentActionContext`, `Journey`.
- Produces: `SessionDevContextDto`, `GetSessionDevContextHandler.HandleAsync(GetSessionDevContextQuery)`.

- [ ] **Step 1: Write the failing handler test**

Create `tests/WildBunch.Application.Tests/Dev/GetSessionDevContextHandlerTests.cs`. Mirror the construction pattern from `GetSaloonDevContextHandlerTests.cs` (use the same `InMemoryGameSessionRepository` and session builder). Build a session, seed it, call the handler, assert the DTO shape.

```csharp
using WildBunch.Application.Dev.Queries;
using WildBunch.Application.Tests.TestDoubles;
// ... usings mirroring GetSaloonDevContextHandlerTests.cs

namespace WildBunch.Application.Tests.Dev;

public sealed class GetSessionDevContextHandlerTests
{
    [Fact]
    public async Task HandleAsync_ReturnsSessionContext_WithSetupPosture()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateSeededSession(); // mirror GetSaloonDevContextHandlerTests helper
        repository.Seed(session);

        var handler = new GetSessionDevContextHandler(repository);

        var result = await handler.HandleAsync(new GetSessionDevContextQuery(session.Id.Value));

        Assert.Equal(session.Id.Value, result.SessionId);
        Assert.Equal("Active", result.Status);
        Assert.Equal(session.GameDifficulty.ToString(), result.GameDifficulty);
        Assert.Equal(session.GameEntropy.ToString(), result.GameEntropy);
        Assert.NotNull(result.SaltPosture);
        Assert.Equal(session.SaltSource.Mode.ToString(), result.SaltPosture!.Mode);
        Assert.Equal(session.SaltSource.Salt, result.SaltPosture.Salt);
        Assert.Equal(session.Clock.Day, result.Clock.Day);
        Assert.Equal(session.Clock.Turn, result.Clock.Turn);
        Assert.Equal(session.CurrentTown.TownId.Value, result.CurrentTownId);
        Assert.Equal(session.CurrentTown.TownName, result.CurrentTownName);
        Assert.Equal(session.CurrentActionContext.ToString(), result.CurrentActionContext);
        // Seed code is honestly reported as not retained
        Assert.False(result.SeedCodeRetained);
        Assert.Null(result.SeedCodeText);
    }

    [Fact]
    public async Task HandleAsync_AfterForceDevSaltSource_ReflectsFixedSalt()
    {
        var repository = new InMemoryGameSessionRepository();
        var session = CreateSeededSession();
        session.ForceDevSaltSource(WildBunch.Domain.Travel.SaltSource.CreateFixed("deadbeef"));
        session.MarkEventsCommitted();
        repository.Seed(session);

        var handler = new GetSessionDevContextHandler(repository);

        var result = await handler.HandleAsync(new GetSessionDevContextQuery(session.Id.Value));

        Assert.Equal("Fixed", result.SaltPosture!.Mode);
        Assert.Equal("deadbeef", result.SaltPosture.Salt);
    }

    // Reuse the CreateSeededSession / CreateSessionWithSaloonSuspect helper pattern
    // from GetSaloonDevContextHandlerTests.cs, going through the seed system.
    private static GameSession CreateSeededSession() { /* ... */ }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/WildBunch.Application.Tests --filter "FullyQualifiedName~GetSessionDevContextHandlerTests"`
Expected: FAIL with "SessionDevContextDto / GetSessionDevContextHandler not found".

- [ ] **Step 3: Create the SessionDevContextDto**

Create `src/WildBunch.Application/Dev/Models/SessionDevContextDto.cs`:

```csharp
namespace WildBunch.Application.Dev.Models;

public sealed record SessionDevContextDto(
    Guid SessionId,
    string Status,
    string GameDifficulty,
    string GameEntropy,
    SaltPostureDevDto SaltPosture,
    ClockDevDto Clock,
    string? CurrentTownId,
    string? CurrentTownName,
    string CurrentActionContext,
    bool HasActiveJourney,
    bool SeedCodeRetained,
    string? SeedCodeText);

public sealed record SaltPostureDevDto(string Mode, string Salt);

public sealed record ClockDevDto(int Day, int Turn, string TimeOfDay);
```

`SeedCodeRetained` is always `false` and `SeedCodeText` is always `null` in this slice — the original game-start UUID is not retained on the live session. This is the honest dev-overlay answer (dev-overlay unslop: do not invent missing domain categories).

- [ ] **Step 4: Create the query record**

Create `src/WildBunch.Application/Dev/Queries/GetSessionDevContextQuery.cs`, mirroring `GetSaloonDevContextQuery.cs`:

```csharp
namespace WildBunch.Application.Dev.Queries;

public sealed record GetSessionDevContextQuery(Guid SessionId);
```

- [ ] **Step 5: Create the mapper**

Create `src/WildBunch.Application/Dev/Mapping/SessionDevContextMapper.cs`:

```csharp
using WildBunch.Application.Dev.Models;
using WildBunch.Domain.Game;

namespace WildBunch.Application.Dev.Mapping;

public static class SessionDevContextMapper
{
    public static SessionDevContextDto ToDto(GameSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        return new SessionDevContextDto(
            SessionId: session.Id.Value,
            Status: session.Status.ToString(),
            GameDifficulty: session.GameDifficulty.ToString(),
            GameEntropy: session.GameEntropy.ToString(),
            SaltPosture: new SaltPostureDevDto(session.SaltSource.Mode.ToString(), session.SaltSource.Salt),
            Clock: new ClockDevDto(session.Clock.Day, session.Clock.Turn, session.Clock.TimeOfDay.ToString()),
            CurrentTownId: session.CurrentTown.TownId.Value,
            CurrentTownName: session.CurrentTown.TownName,
            CurrentActionContext: session.CurrentActionContext.ToString(),
            HasActiveJourney: session.Journey is not null,
            // The original game-start UUID seed code is consumed at StartNew and not
            // retained on the live session. Report this honestly.
            SeedCodeRetained: false,
            SeedCodeText: null);
    }
}
```

- [ ] **Step 6: Create the handler**

Create `src/WildBunch.Application/Dev/Queries/GetSessionDevContextHandler.cs`, mirroring `GetSaloonDevContextHandler.cs`:

```csharp
using WildBunch.Application.Abstractions;
using WildBunch.Application.Dev.Mapping;
using WildBunch.Application.Dev.Models;
using WildBunch.Application.Games.Exceptions;
using WildBunch.Domain.Game;

namespace WildBunch.Application.Dev.Queries;

public sealed class GetSessionDevContextHandler
{
    private readonly IGameSessionRepository _repository;

    public GetSessionDevContextHandler(IGameSessionRepository repository)
    {
        _repository = repository;
    }

    public async Task<SessionDevContextDto> HandleAsync(GetSessionDevContextQuery query, CancellationToken cancellationToken = default)
    {
        var sessionId = new GameSessionId(query.SessionId);
        var session = await _repository.GetByIdAsync(sessionId, cancellationToken).ConfigureAwait(false);
        if (session is null)
        {
            throw new GameSessionNotFoundException(sessionId);
        }

        return SessionDevContextMapper.ToDto(session);
    }
}
```

- [ ] **Step 7: Run the handler tests to verify they pass**

Run: `dotnet test tests/WildBunch.Application.Tests --filter "FullyQualifiedName~GetSessionDevContextHandlerTests"`
Expected: PASS.

- [ ] **Step 8: Commit**

```bash
git add src/WildBunch.Application/Dev/Models/SessionDevContextDto.cs src/WildBunch.Application/Dev/Queries/GetSessionDevContextQuery.cs src/WildBunch.Application/Dev/Queries/GetSessionDevContextHandler.cs src/WildBunch.Application/Dev/Mapping/SessionDevContextMapper.cs tests/WildBunch.Application.Tests/Dev/GetSessionDevContextHandlerTests.cs
git commit -m "feat(application): add SessionDevContext query for BUNCH-101"
```

---

## Task 3: Application — ForceDevSaltSource / ClearDevSaltSource command handlers

**Files:**
- Create: `src/WildBunch.Application/Dev/Models/LockRngRequestDto.cs`
- Create: `src/WildBunch.Application/Dev/Commands/ForceDevSaltSourceCommand.cs`
- Create: `src/WildBunch.Application/Dev/Commands/ForceDevSaltSourceHandler.cs`
- Create: `src/WildBunch.Application/Dev/Commands/ClearDevSaltSourceCommand.cs`
- Create: `src/WildBunch.Application/Dev/Commands/ClearDevSaltSourceHandler.cs`
- Test: `tests/WildBunch.Application.Tests/Dev/ForceDevSaltSourceHandlerTests.cs`
- Test: `tests/WildBunch.Application.Tests/Dev/ClearDevSaltSourceHandlerTests.cs`

**Interfaces:**
- Consumes: `IGameSessionRepository`, `IGameSessionUnitOfWork` (via `GameSessionCommandHandler` base), `GameSession.ForceDevSaltSource` / `ClearDevSaltSource` (from Task 1), `SaltSource.CreateFixed` / `CreateRuntime`.
- Produces: `ForceDevSaltSourceHandler.HandleAsync(ForceDevSaltSourceCommand)`, `ClearDevSaltSourceHandler.HandleAsync(ClearDevSaltSourceCommand)`.

- [ ] **Step 1: Write the failing command handler tests**

Create `tests/WildBunch.Application.Tests/Dev/ForceDevSaltSourceHandlerTests.cs` and `ClearDevSaltSourceHandlerTests.cs`. Mirror the handler-test pattern from the existing saloon/travel command handler tests (use `InMemoryGameSessionRepository` + the unit-of-work test double used by `ForceSaloonOverrideHandler` tests). Build a seeded session, seed the repo, invoke the handler, reload, assert the salt posture changed and the event is in the stream.

```csharp
using WildBunch.Application.Dev.Commands;
using WildBunch.Application.Tests.TestDoubles;
using WildBunch.Domain.Travel;

namespace WildBunch.Application.Tests.Dev;

public sealed class ForceDevSaltSourceHandlerTests
{
    [Fact]
    public async Task HandleAsync_LocksRngToFixedSalt_WhenSaltProvided()
    {
        var repository = new InMemoryGameSessionRepository();
        var uow = new InMemoryGameSessionUnitOfWork(); // mirror existing test double
        var session = CreateSeededSession();
        repository.Seed(session);

        var handler = new ForceDevSaltSourceHandler(repository, uow);
        await handler.HandleAsync(new ForceDevSaltSourceCommand(session.Id.Value, "deadbeef"));

        var reloaded = await repository.GetByIdAsync(new(session.Id.Value));
        Assert.Equal(SaltSourceMode.Fixed, reloaded!.SaltSource.Mode);
        Assert.Equal("deadbeef", reloaded.SaltSource.Salt);
    }

    [Fact]
    public async Task HandleAsync_LocksRngWithGeneratedSalt_WhenSaltIsNull()
    {
        var repository = new InMemoryGameSessionRepository();
        var uow = new InMemoryGameSessionUnitOfWork();
        var session = CreateSeededSession();
        repository.Seed(session);

        var handler = new ForceDevSaltSourceHandler(repository, uow);
        await handler.HandleAsync(new ForceDevSaltSourceCommand(session.Id.Value, Salt: null));

        var reloaded = await repository.GetByIdAsync(new(session.Id.Value));
        Assert.Equal(SaltSourceMode.Fixed, reloaded!.SaltSource.Mode);
        Assert.False(string.IsNullOrEmpty(reloaded.SaltSource.Salt));
    }
}
```

`ClearDevSaltSourceHandlerTests` asserts that after a forced salt, clearing restores `SaltSourceMode.Runtime`.

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/WildBunch.Application.Tests --filter "FullyQualifiedName~ForceDevSaltSourceHandlerTests|FullyQualifiedName~ClearDevSaltSourceHandlerTests"`
Expected: FAIL with handlers not found.

- [ ] **Step 3: Create the request DTO**

Create `src/WildBunch.Application/Dev/Models/LockRngRequestDto.cs`:

```csharp
namespace WildBunch.Application.Dev.Models;

public sealed record LockRngRequestDto(string? Salt);
```

If `Salt` is null/empty, the handler generates a fresh fixed salt via `SaltSource.CreateFixed(Convert.ToHexString(RandomNumberGenerator.GetBytes(16)))`.

- [ ] **Step 4: Create the command records**

Create `src/WildBunch.Application/Dev/Commands/ForceDevSaltSourceCommand.cs`:

```csharp
namespace WildBunch.Application.Dev.Commands;

public sealed record ForceDevSaltSourceCommand(Guid GameSessionId, string? Salt);
```

Create `src/WildBunch.Application/Dev/Commands/ClearDevSaltSourceCommand.cs`:

```csharp
namespace WildBunch.Application.Dev.Commands;

public sealed record ClearDevSaltSourceCommand(Guid GameSessionId);
```

- [ ] **Step 5: Create the ForceDevSaltSourceHandler**

Create `src/WildBunch.Application/Dev/Commands/ForceDevSaltSourceHandler.cs`, mirroring `ForceSaloonOverrideHandler.cs` (extends `GameSessionCommandHandler`):

```csharp
using System.Security.Cryptography;
using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Execution;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;

namespace WildBunch.Application.Dev.Commands;

public sealed class ForceDevSaltSourceHandler : GameSessionCommandHandler
{
    public ForceDevSaltSourceHandler(
        IGameSessionRepository gameSessionRepository,
        IGameSessionUnitOfWork gameSessionUnitOfWork)
        : base(gameSessionRepository, gameSessionUnitOfWork)
    {
    }

    public async Task HandleAsync(ForceDevSaltSourceCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var sessionId = new GameSessionId(command.GameSessionId);
        var salt = string.IsNullOrWhiteSpace(command.Salt)
            ? SaltSource.CreateFixed(Convert.ToHexString(RandomNumberGenerator.GetBytes(16)))
            : SaltSource.CreateFixed(command.Salt);

        await ExecuteWithRetryAsync(sessionId, (session, ct) =>
        {
            session.ForceDevSaltSource(salt);
            return Task.FromResult(true);
        }, cancellationToken).ConfigureAwait(false);
    }
}
```

- [ ] **Step 6: Create the ClearDevSaltSourceHandler**

Create `src/WildBunch.Application/Dev/Commands/ClearDevSaltSourceHandler.cs`, mirroring `ClearSaloonOverrideHandler.cs`:

```csharp
using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Execution;
using WildBunch.Domain.Game;

namespace WildBunch.Application.Dev.Commands;

public sealed class ClearDevSaltSourceHandler : GameSessionCommandHandler
{
    public ClearDevSaltSourceHandler(
        IGameSessionRepository gameSessionRepository,
        IGameSessionUnitOfWork gameSessionUnitOfWork)
        : base(gameSessionRepository, gameSessionUnitOfWork)
    {
    }

    public async Task HandleAsync(ClearDevSaltSourceCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var sessionId = new GameSessionId(command.GameSessionId);

        await ExecuteWithRetryAsync(sessionId, (session, ct) =>
        {
            session.ClearDevSaltSource();
            return Task.FromResult(true);
        }, cancellationToken).ConfigureAwait(false);
    }
}
```

- [ ] **Step 7: Run the command handler tests to verify they pass**

Run: `dotnet test tests/WildBunch.Application.Tests --filter "FullyQualifiedName~ForceDevSaltSourceHandlerTests|FullyQualifiedName~ClearDevSaltSourceHandlerTests"`
Expected: PASS.

- [ ] **Step 8: Commit**

```bash
git add src/WildBunch.Application/Dev/Models/LockRngRequestDto.cs src/WildBunch.Application/Dev/Commands/ForceDevSaltSourceCommand.cs src/WildBunch.Application/Dev/Commands/ForceDevSaltSourceHandler.cs src/WildBunch.Application/Dev/Commands/ClearDevSaltSourceCommand.cs src/WildBunch.Application/Dev/Commands/ClearDevSaltSourceHandler.cs tests/WildBunch.Application.Tests/Dev/ForceDevSaltSourceHandlerTests.cs tests/WildBunch.Application.Tests/Dev/ClearDevSaltSourceHandlerTests.cs
git commit -m "feat(application): add ForceDevSaltSource/ClearDevSaltSource command handlers for BUNCH-101"
```

---

## Task 4: API — wire dev endpoints + DI registration

**Files:**
- Modify: `src/WildBunch.Api/Dev/DevEndpoints.cs`
- Modify: `src/WildBunch.Api/DependencyInjection.cs`
- Test: `tests/WildBunch.Integration.Tests/Dev/DevSessionEndpointTests.cs`

**Interfaces:**
- Consumes: `GetSessionDevContextHandler`, `ForceDevSaltSourceHandler`, `ClearDevSaltSourceHandler` (from Tasks 2–3), `DevRoleGuard`, `GameSessionNotFoundException`.
- Produces: `GET /api/dev/sessions/{id}/session-context`, `POST /api/dev/sessions/{id}/session/lock-rng`, `POST /api/dev/sessions/{id}/session/clear-rng`.

- [ ] **Step 1: Write the failing integration tests**

Create `tests/WildBunch.Integration.Tests/Dev/DevSessionEndpointTests.cs`, mirroring `DevSaloonEndpointTests.cs` (uses `PostgreSqlApiFactory`, `NonDevApiFactory`, and the `CreateSessionAsync` helper from that file). Cover: 200 in dev, 403 in non-dev, 404 when session missing, lock-rng 204 + reflected in context, clear-rng 204 + reflected in context, and a normal API boundary check (player `GET /api/games/{id}` DTO does not contain a `saltPosture` field).

```csharp
using System.Net;
using System.Net.Http.Json;
using WildBunch.Application.Dev.Models;
using WildBunch.Application.Games.Models;
using WildBunch.Integration.Tests.TestInfrastructure;

namespace WildBunch.Integration.Tests.Dev;

public sealed class DevSessionEndpointTests
{
    [Fact]
    public async Task GetSessionDevContext_Returns200_InDevEnvironment()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();
        var gameId = await CreateSessionAsync(client);

        var response = await client.GetAsync($"/api/dev/sessions/{gameId}/session-context");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var context = await response.Content.ReadFromJsonAsync<SessionDevContextDto>();
        Assert.NotNull(context);
        Assert.Equal(gameId, context!.SessionId);
        Assert.NotNull(context.SaltPosture);
        Assert.False(context.SeedCodeRetained);
    }

    [Fact]
    public async Task GetSessionDevContext_Returns403_InNonDevEnvironment()
    {
        using var factory = new NonDevApiFactory();
        using var client = factory.CreateClient();
        var response = await client.GetAsync($"/api/dev/sessions/{Guid.NewGuid()}/session-context");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetSessionDevContext_Returns404_WhenSessionDoesNotExist()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();
        var response = await client.GetAsync($"/api/dev/sessions/{Guid.NewGuid()}/session-context");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task LockRng_Returns204_AndReflectedInContext()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();
        var gameId = await CreateSessionAsync(client);

        var lockResponse = await client.PostAsJsonAsync(
            $"/api/dev/sessions/{gameId}/session/lock-rng",
            new LockRngRequestDto(Salt: "deadbeef"));
        Assert.Equal(HttpStatusCode.NoContent, lockResponse.StatusCode);

        var context = await (await client.GetAsync($"/api/dev/sessions/{gameId}/session-context"))
            .Content.ReadFromJsonAsync<SessionDevContextDto>();
        Assert.Equal("Fixed", context!.SaltPosture.Mode);
        Assert.Equal("deadbeef", context.SaltPosture.Salt);
    }

    [Fact]
    public async Task ClearRng_Returns204_AndRestoresRuntimeMode()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();
        var gameId = await CreateSessionAsync(client);
        await client.PostAsJsonAsync($"/api/dev/sessions/{gameId}/session/lock-rng", new LockRngRequestDto("deadbeef"));

        var clearResponse = await client.PostAsync($"/api/dev/sessions/{gameId}/session/clear-rng", null);
        Assert.Equal(HttpStatusCode.NoContent, clearResponse.StatusCode);

        var context = await (await client.GetAsync($"/api/dev/sessions/{gameId}/session-context"))
            .Content.ReadFromJsonAsync<SessionDevContextDto>();
        Assert.Equal("Runtime", context!.SaltPosture.Mode);
    }

    [Fact]
    public async Task PlayerGameDto_DoesNotContainDevSaltPosture()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateClient();
        var gameId = await CreateSessionAsync(client);

        var game = await (await client.GetAsync($"/api/games/{gameId}"))
            .Content.ReadFromJsonAsync<GameSessionDto>();
        // Player DTO must not carry dev-only salt posture. Serialize and assert no "saltPosture" token.
        var json = await (await client.GetAsync($"/api/games/{gameId}")).Content.ReadAsStringAsync();
        Assert.DoesNotContain("saltPosture", json, StringComparison.OrdinalIgnoreCase);
    }

    // Reuse the CreateSessionAsync helper from DevSaloonEndpointTests.cs.
    private static async Task<Guid> CreateSessionAsync(HttpClient client) { /* ... */ }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run (requires PostgreSQL lane): `.\scripts\postgres-dev.ps1 test -- dotnet test tests/WildBunch.Integration.Tests --filter "FullyQualifiedName~DevSessionEndpointTests"`
Expected: FAIL with route not found / 404 on the dev routes.

- [ ] **Step 3: Register handlers in DI**

In `src/WildBunch.Api/DependencyInjection.cs`, in the dev-only services block (after `services.AddScoped<ClearSaloonOverrideHandler>();`), add:

```csharp
services.AddScoped<GetSessionDevContextHandler>();
services.AddScoped<ForceDevSaltSourceHandler>();
services.AddScoped<ClearDevSaltSourceHandler>();
```

- [ ] **Step 4: Add the endpoints to DevEndpoints.cs**

In `src/WildBunch.Api/Dev/DevEndpoints.cs`, in `MapDevEndpoints` (after the saloon endpoints, before `return app;`), add:

```csharp
dev.MapGet("/sessions/{id:guid}/session-context", GetSessionDevContextAsync)
    .WithName("GetSessionDevContext")
    .Produces<SessionDevContextDto>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status403Forbidden)
    .Produces(StatusCodes.Status404NotFound);

dev.MapPost("/sessions/{id:guid}/session/lock-rng", LockRngAsync)
    .WithName("LockRng")
    .Produces(StatusCodes.Status204NoContent)
    .Produces(StatusCodes.Status403Forbidden)
    .Produces(StatusCodes.Status404NotFound);

dev.MapPost("/sessions/{id:guid}/session/clear-rng", ClearRngAsync)
    .WithName("ClearRng")
    .Produces(StatusCodes.Status204NoContent)
    .Produces(StatusCodes.Status403Forbidden)
    .Produces(StatusCodes.Status404NotFound);
```

Add the three private handler methods, mirroring the exact try/catch shape of `GetSaloonDevContextAsync` / `ForceSaloonOverrideAsync` / `ClearSaloonOverrideAsync` (catch `DevAccessDeniedException` → 403, `GameSessionNotFoundException` → 404). `LockRngAsync` binds `LockRngRequestDto request` and calls `ForceDevSaltSourceHandler` with `new ForceDevSaltSourceCommand(id, request.Salt)`. `ClearRngAsync` calls `ClearDevSaltSourceHandler` with `new ClearDevSaltSourceCommand(id)` and sends `null` body.

- [ ] **Step 5: Run the integration tests to verify they pass**

Run: `.\scripts\postgres-dev.ps1 test -- dotnet test tests/WildBunch.Integration.Tests --filter "FullyQualifiedName~DevSessionEndpointTests"`
Expected: PASS (6 tests).

- [ ] **Step 6: Commit**

```bash
git add src/WildBunch.Api/Dev/DevEndpoints.cs src/WildBunch.Api/DependencyInjection.cs tests/WildBunch.Integration.Tests/Dev/DevSessionEndpointTests.cs
git commit -m "feat(api): wire Session dev endpoints (session-context, lock-rng, clear-rng) for BUNCH-101"
```

---

## Task 5: Frontend — SessionDevPanel + devApi + types + registry

**Files:**
- Modify: `src/WildBunch.Web/src/dev/types.ts`
- Modify: `src/WildBunch.Web/src/dev/devApi.ts`
- Create: `src/WildBunch.Web/src/dev/panels/SessionDevPanel.tsx`
- Modify: `src/WildBunch.Web/src/dev/DevPanelRegistry.tsx`
- Test: `src/WildBunch.Web/src/tests/SessionDevPanel.test.tsx`

**Interfaces:**
- Consumes: `useGameSession` (existing), `useQuery`/`useQueryClient` (TanStack Query), `requestJson` (existing httpClient), `DevPanelRenderProps` (existing).
- Produces: `SessionDevPanel` component, `getSessionDevContext`/`lockRng`/`clearRng` client functions, `SessionDevContextDto`/`LockRngRequestDto`/`SaltPostureDevDto`/`ClockDevDto` TS types, `session-dev` registry entry.

- [ ] **Step 1: Write the failing frontend test**

Create `src/WildBunch.Web/src/tests/SessionDevPanel.test.tsx`, mirroring `TravelDevPanel.test.tsx` (mock `devApi`, mock `wildBunchApi`, `GameSessionProvider`, `QueryClientProvider`, `seedGameId` via localStorage). Cover: no-active-session message, renders session context when loaded, lock-rng button calls `lockRng`, clear-rng button calls `clearRng`, expanded prop is accepted.

```tsx
import { afterEach, describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { cleanup, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { SessionDevPanel } from "../dev/panels/SessionDevPanel";
import { GameSessionProvider } from "../state/GameSessionProvider";
import { clearRng, getSessionDevContext, lockRng } from "../dev/devApi";

vi.mock("../dev/devApi", () => ({
  getSessionDevContext: vi.fn(),
  lockRng: vi.fn(),
  clearRng: vi.fn(),
  getSessionAudit: vi.fn(),
}));
vi.mock("../api/wildBunchApi", () => ({
  // mirror the full mock list from TravelDevPanel.test.tsx
  getGame: vi.fn(), createGame: vi.fn(), /* ...rest... */
}));

const mockedGetContext = vi.mocked(getSessionDevContext);
const mockedLock = vi.mocked(lockRng);
const mockedClear = vi.mocked(clearRng);

afterEach(() => { cleanup(); vi.clearAllMocks(); window.localStorage.clear(); });

function renderPanel(expanded = false) {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } });
  render(
    <QueryClientProvider client={queryClient}>
      <GameSessionProvider>
        <SessionDevPanel expanded={expanded} />
      </GameSessionProvider>
    </QueryClientProvider>,
  );
}
function seedGameId(id: string) { window.localStorage.setItem("wild-bunch.current-game-id", id); }

describe("SessionDevPanel", () => {
  it("shows no active session message when gameId is missing", () => {
    renderPanel();
    expect(screen.getByText(/no active session/i)).toBeInTheDocument();
  });

  it("renders session context when loaded", async () => {
    seedGameId("test-game-1");
    mockedGetContext.mockResolvedValue({
      sessionId: "test-game-1", status: "Active", gameDifficulty: "Standard", gameEntropy: "Classic",
      saltPosture: { mode: "Runtime", salt: "abc" }, clock: { day: 1, turn: 0, timeOfDay: "Dawn" },
      currentTownId: "town-1", currentTownName: "Dodge", currentActionContext: "None",
      hasActiveJourney: false, seedCodeRetained: false, seedCodeText: null,
    });
    renderPanel();
    await waitFor(() => { expect(screen.getByText("Active")).toBeInTheDocument(); });
    expect(screen.getByText("Standard")).toBeInTheDocument();
    expect(screen.getByText("Classic")).toBeInTheDocument();
    expect(screen.getByText("Runtime")).toBeInTheDocument();
  });

  it("calls lockRng when Lock RNG is clicked", async () => {
    seedGameId("test-game-2");
    mockedGetContext.mockResolvedValue({ /* ...same shape... */ });
    mockedLock.mockResolvedValue(undefined);
    renderPanel();
    await waitFor(() => { expect(screen.getByRole("button", { name: /lock rng/i })).toBeInTheDocument(); });
    const user = userEvent.setup();
    await user.click(screen.getByRole("button", { name: /lock rng/i }));
    await waitFor(() => { expect(mockedLock).toHaveBeenCalledWith("test-game-2", expect.objectContaining({})); });
  });

  it("calls clearRng when Clear RNG is clicked", async () => {
    seedGameId("test-game-3");
    mockedGetContext.mockResolvedValue({ /* ...salt mode Fixed... */ });
    mockedClear.mockResolvedValue(undefined);
    renderPanel();
    await waitFor(() => { expect(screen.getByRole("button", { name: /clear rng/i })).toBeInTheDocument(); });
    const user = userEvent.setup();
    await user.click(screen.getByRole("button", { name: /clear rng/i }));
    await waitFor(() => { expect(mockedClear).toHaveBeenCalledWith("test-game-3"); });
  });
});
```

- [ ] **Step 2: Run test to verify it fails**

Run (from `src/WildBunch.Web`): `npx vitest run src/tests/SessionDevPanel.test.tsx`
Expected: FAIL with module not found / `SessionDevPanel` not defined.

- [ ] **Step 3: Add the TypeScript types**

In `src/WildBunch.Web/src/dev/types.ts`, append:

```ts
export interface SaltPostureDevDto {
  mode: string;
  salt: string;
}

export interface ClockDevDto {
  day: number;
  turn: number;
  timeOfDay: string;
}

export interface SessionDevContextDto {
  sessionId: string;
  status: string;
  gameDifficulty: string;
  gameEntropy: string;
  saltPosture: SaltPostureDevDto;
  clock: ClockDevDto;
  currentTownId: string | null;
  currentTownName: string | null;
  currentActionContext: string;
  hasActiveJourney: boolean;
  seedCodeRetained: boolean;
  seedCodeText: string | null;
}

export interface LockRngRequestDto {
  salt?: string | null;
}
```

- [ ] **Step 4: Add the devApi client functions**

In `src/WildBunch.Web/src/dev/devApi.ts`, add the import for the new types and:

```ts
export function getSessionDevContext(gameId: string) {
  return requestJson<SessionDevContextDto>(`/api/dev/sessions/${gameId}/session-context`);
}

export function lockRng(gameId: string, request: LockRngRequestDto) {
  return requestJson<void>(`/api/dev/sessions/${gameId}/session/lock-rng`, {
    method: "POST",
    body: JSON.stringify(request),
  });
}

export function clearRng(gameId: string) {
  return requestJson<void>(`/api/dev/sessions/${gameId}/session/clear-rng`, {
    method: "POST",
  });
}
```

- [ ] **Step 5: Create the SessionDevPanel component**

Create `src/WildBunch.Web/src/dev/panels/SessionDevPanel.tsx`. Mirror the styled-components structure of `TravelDevPanel.tsx` / `SaloonDevPanel.tsx` (Container, Section, SectionTitle, Row, Label, Value, Field, Input, Button, ButtonRow, MutedText, ErrorText). Accept `expanded?: boolean` and use a two-column layout in expanded mode (LeftColumn / RightColumn) per dev-overlay doctrine §4. Compact mode stacks vertically.

Sections:
1. **Session** — SessionId (truncated), Status, Current action context, Has active journey.
2. **Clock** — Day, Turn, Time of day.
3. **Location** — Current town id + name.
4. **Setup posture** — GameDifficulty (inspection-only, labeled "Difficulty (inspect)"), GameEntropy (inspection-only, labeled "Entropy (inspect)"), Salt posture mode + salt, Seed code retained (honestly "No — not retained on live session").
5. **RNG controls** — Optional salt input + "Lock RNG" button + "Clear RNG" button. A note: "Locking RNG makes the run reproducible. It does not force encounter outcomes."

Use domain-facing labels, not raw IDs, for difficulty/entropy (they are enum names already). The salt input is an explicit dev field, not a domain candidate select — this is acceptable because the salt is a dev-only reproducibility token with no domain candidates (dev-overlay unslop §4 is about domain candidates; a free-form salt token is the honest shape here).

```tsx
import { useState } from "react";
import styled from "styled-components";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useGameSession } from "../../state/useGameSession";
import { clearRng, getSessionDevContext, lockRng } from "../devApi";

interface SessionDevPanelProps {
  expanded?: boolean;
}

export function SessionDevPanel({ expanded = false }: SessionDevPanelProps) {
  const { gameId } = useGameSession();
  const queryClient = useQueryClient();
  const [saltInput, setSaltInput] = useState<string>("");
  const [error, setError] = useState<string | null>(null);
  const [actionPending, setActionPending] = useState(false);

  const { data, isLoading } = useQuery({
    queryKey: ["dev-session-context", gameId],
    queryFn: () => getSessionDevContext(gameId as string),
    enabled: Boolean(gameId),
    retry: false,
  });

  if (!gameId) {
    return <MutedText>No active session.</MutedText>;
  }
  if (isLoading) {
    return <MutedText>Loading session context...</MutedText>;
  }

  const refresh = () => queryClient.invalidateQueries({ queryKey: ["dev-session-context", gameId] });

  const handleLock = async () => {
    setError(null);
    setActionPending(true);
    try {
      await lockRng(gameId, { salt: saltInput.trim() === "" ? null : saltInput.trim() });
      refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to lock RNG.");
    } finally {
      setActionPending(false);
    }
  };

  const handleClear = async () => {
    setError(null);
    setActionPending(true);
    try {
      await clearRng(gameId);
      refresh();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to clear RNG.");
    } finally {
      setActionPending(false);
    }
  };

  // ...JSX with the sections above. In expanded mode, wrap in a two-column grid.
}
```

(Full styled-components definitions mirror `TravelDevPanel.tsx`. The `expanded` prop switches `Container` to `display: grid; grid-template-columns: 1fr 1fr;` when true.)

- [ ] **Step 6: Register the panel in DevPanelRegistry**

In `src/WildBunch.Web/src/dev/DevPanelRegistry.tsx`, add the import and a new entry in `devPanels`. Session dev is available on all surfaces (no `surfaces` filter) and is NOT a surface owner (no `isSurfaceOwner`), so it never displaces Saloon/Travel defaults:

```tsx
import { SessionDevPanel } from "./panels/SessionDevPanel";
// ...
{
  id: "session-dev",
  label: "Session dev",
  render: ({ expanded }) => <SessionDevPanel expanded={expanded} />,
  // Available on all surfaces; not a surface owner (per dev-overlay doctrine §3)
},
```

Place it after `session-audit` and before the surface-owner panels.

- [ ] **Step 7: Run the frontend test to verify it passes**

Run (from `src/WildBunch.Web`): `npx vitest run src/tests/SessionDevPanel.test.tsx`
Expected: PASS (4 tests).

- [ ] **Step 8: Run frontend typecheck + build + styling enforcement**

Run (from `src/WildBunch.Web`):
```
npx tsc --noEmit
npm run build
npx vitest run src/tests/stylingEnforcement.test.ts
```
Expected: PASS (no styled-components violations, no plain CSS classes).

- [ ] **Step 9: Commit**

```bash
git add src/WildBunch.Web/src/dev/types.ts src/WildBunch.Web/src/dev/devApi.ts src/WildBunch.Web/src/dev/panels/SessionDevPanel.tsx src/WildBunch.Web/src/dev/DevPanelRegistry.tsx src/WildBunch.Web/src/tests/SessionDevPanel.test.tsx
git commit -m "feat(web): add SessionDevPanel with RNG posture controls for BUNCH-101"
```

---

## Task 6: Validation, browser proof, plan closeout, index mesh

**Files:**
- Modify: `.agents/superpowers/plans/2026-06-28-bunch-101-session-dev-panel.md` (this file — check off boxes)
- Modify: `.agents/superpowers/plans/INDEX.md` (regenerated)

- [ ] **Step 1: Run the full backend validation suite**

```
.\scripts\postgres-dev.ps1 ensure
dotnet build
dotnet test
.\scripts\postgres-dev.ps1 validate
```
Expected: build clean, all tests pass, EF migrations list clean.

- [ ] **Step 2: Run the full frontend validation suite**

```
cd src/WildBunch.Web
npx tsc --noEmit
npm run build
npx vitest run
cd ../..
```
Expected: typecheck clean, build clean, all Vitest suites pass.

- [ ] **Step 3: Regenerate the index mesh**

```
python scripts/generate_index_mesh.py
```
Expected: `INDEX.md` files updated to include the new plan file and any new source files. Commit any changed `INDEX.md` files.

- [ ] **Step 4: Browser/screenshot proof (compact + expanded)**

Start the API + web dev servers, start a game, open the dev overlay, select "Session dev", capture compact and expanded screenshots. Save screenshots under `.agents/superpowers/output/screenshots/` (git-ignored — do NOT commit). Cite the local filenames in the PR body. Verify:
- Session dev appears in the panel sidebar on a town surface and on a trail surface.
- Saloon surface still defaults to Saloon dev (Session dev does not steal the default).
- Lock RNG → context refresh shows Fixed salt; Clear RNG → context refresh shows Runtime.
- Expanded mode uses two columns, not a tall single column.

- [ ] **Step 5: Normal gameplay unchanged proof**

With no dev command issued, perform a normal saloon look-around and a normal travel action. Confirm the player `GET /api/games/{id}` DTO and journal are unchanged (no `saltPosture`, no dev fields). Cite the integration test `PlayerGameDto_DoesNotContainDevSaltPosture` as the automated proof, plus the browser check.

- [ ] **Step 6: Check off all plan checkboxes and commit the plan closeout**

Update every `- [ ]` in this plan to `- [x]`. Commit:

```bash
git add .agents/superpowers/plans/2026-06-28-bunch-101-session-dev-panel.md .agents/superpowers/plans/INDEX.md
git commit -m "chore(plan): close out BUNCH-101 session dev panel plan checkboxes"
```

- [ ] **Step 7: Push the branch and open the PR**

```bash
git push -u origin bunch-101-exec
gh pr create --title "BUNCH-101: Session dev panel for difficulty, randomness, and game-level setup controls" --body "$(cat <<'EOF'
## Summary
- Adds `session-dev` dev overlay panel owning game/session-level setup and control state.
- New guarded `/api/dev/sessions/{id}/session-context` query, `session/lock-rng` and `session/clear-rng` commands.
- New `DevSaltSourceForced`/`DevSaltSourceCleared` domain events + `GameSession.ForceDevSaltSource`/`ClearDevSaltSource` aggregate commands (event-sourced, snapshot already persists SaltSource).
- Difficulty and entropy are inspection-only here; their mutation semantics stay owned by BUNCH-94/BUNCH-93.
- Session dev is available on all surfaces and is not a surface owner, so it never displaces Saloon/Travel defaults.

## DOD mapping (Session dev vs Session Audit boundary)
- Session dev owns: session identity/status/phase, seed/setup posture (RNG salt), difficulty/entropy inspection, RNG lock/clear.
- Session Audit still owns: read-heavy event/log inspection. Not touched.
- Player/travel/saloon/casefile/suspect/inventory deep editors: not built (out of scope).

#### Test plan
- [x] `dotnet build` clean
- [x] `dotnet test` (domain + application + integration via postgres-dev lane)
- [x] Frontend `tsc --noEmit`, `npm run build`, `vitest run`
- [x] `PlayerGameDto_DoesNotContainDevSaltPosture` proves normal API boundary unchanged
- [x] Browser proof: compact + expanded screenshots under `.agents/superpowers/output/screenshots/` (git-ignored)
- [x] Plan checkboxes checked off in `.agents/superpowers/plans/2026-06-28-bunch-101-session-dev-panel.md`

Generated with [Devin](https://devin.ai)
EOF
)"
```

---

## Split conditions

Stop and split or return AMBER if the work requires:
- Implementing BUNCH-93 entropy semantics or BUNCH-94 difficulty semantics (mutation, not inspection).
- Adding a deep Player/Casefile/Suspect editor.
- Redesigning the dev overlay shell (DevOverlay drawer, registry mechanics, surface context).
- Changing hidden-truth doctrine.
- Normalizing runtime session state into new database tables.
- A PR too large for meaningful review (split into query-only + command slices).

If the SaltSource mutation turns out to require changing the `GameStarted` event shape or breaking the snapshot/rehydration contract, stop and report — do not silently widen persistence scope.
