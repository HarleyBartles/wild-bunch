# Code Review: PR #159 - Implementation: Town hub deterministic layout resolver and salt controls

**Reviewer:** Self-review (author)
**Date:** 2026-07-10
**Branch:** `harleydbartles/bunch-147-town-hub-deterministic-layout-resolver`
**Base:** `main`

## Review Lenses Applied

### Principal Architect Lens

**DDD/CQRS/Event Sourcing Conformance:**
- ✅ `LayoutSalts` is modeled as a value object (immutable record)
- ✅ `DevLayoutSaltsForced` is a domain event following the event sourcing pattern
- ✅ `GameSession.SetDevLayoutSalts()` produces the event via `ProduceEvent()`
- ✅ Command handlers (`SetTownLayoutSaltsHandler`, `PrepGameSessionHandler`, `StartGameSessionHandler`) follow CQRS pattern
- ✅ `SetTownLayoutSaltsHandler` uses `ExecuteWithRetryAsync` from `GameSessionCommandHandler` (ADR-0028 compliance)
- ✅ `StartGameSessionHandler` uses `ExecuteWithRetryAsync` pattern for concurrency retry
- ✅ Domain logic (seed parsing, GameSetupResolver) is in `INewGameFactory`, not handlers
- ✅ `GameStatus.Prepped` enum value and `GameSession.StartPrepped()` factory method follow aggregate root pattern

**Aggregate Boundary Discipline:**
- ✅ `GameSession` remains the aggregate root for game session state
- ✅ Dev salts are stored in `GameSession.DevLayoutSalts` (aggregate state)
- ✅ `LayoutSalts` are persisted in `TownLayout` (value object in aggregate)
- ✅ No direct infrastructure concerns leaked into domain layer

**Dependency Direction:**
- ✅ Domain → Application → Api direction respected
- ✅ GameContent (domain services) → Application dependency direction correct
- ✅ Handlers depend on abstractions (`IGameSessionRepository`, `INewGameFactory`)

**ADR Freshness:**
- ✅ ADR-0036 created for Dev-Enabled Action Pattern
- ✅ ADR-0036 indexed in ADR INDEX.md
- ✅ ADR-0036 dated and linked to related ADRs (ADR-0028, ADR-0030, ADR-0002)

**Architecture Skills Alignment:**
- ✅ DDD patterns followed (value objects, domain events, aggregate root)
- ✅ CQRS pattern followed (commands, queries, handlers)
- ✅ Event sourcing pattern followed (events as source of truth)
- ✅ Clean architecture layering respected

### Senior QA Engineer Lens

**Test Coverage Adequacy:**
- ✅ Unit tests for `LayoutSalts` value object
- ✅ Unit tests for `LayoutSaltDeriver` (determinism, dev salts override, entropy mode)
- ✅ Unit tests for `TownLayoutGenerator` (resolver version, layout salts persistence)
- ✅ Unit tests for `GameSession.StartPrepped()` factory method
- ✅ Unit tests for `TownLayoutMapper` (LayoutSalts mapping)
- ✅ Unit tests for `GameSetupResolver` (dev salts overload)
- ✅ Unit tests for `MapGenerator` (dev salts parameter)
- ✅ Unit tests for `SetTownLayoutSaltsHandler` (ExecuteWithRetryAsync)
- ✅ Unit tests for `PrepGameSessionHandler` (creates prepped session)
- ✅ Unit tests for `StartGameSessionHandler` (starts prepped session, dev salts integration, error cases)
- ✅ Frontend tests for `TownLayoutDevPanel` component

**Test Quality:**
- ✅ Tests assert on observable behavior (not mock interactions)
- ✅ Tests use real implementations (`SeededNewGameFactory`, `InMemoryGameSessionRepository`)
- ✅ Tests cover edge cases (null dev salts, wrong session status, session not found)
- ✅ Tests verify the core feature (dev salts flow through pipeline)
- ✅ `LayoutSaltDeriverTests` verifies determinism (same inputs → same outputs)
- ✅ `StartGameSessionHandlerTests` verifies dev salts are passed to factory

**Test Kinds:**
- ✅ Unit tests for domain logic (`LayoutSaltsTests`, `GameSessionStartPreppedTests`)
- ✅ Game-content tests for pipeline (`LayoutSaltDeriverTests`, `GameSetupResolverDevSaltsTests`)
- ✅ Application tests for handlers (`SetTownLayoutSaltsHandlerTests`, `PrepGameSessionHandlerTests`)
- ✅ Frontend tests for component (`TownLayoutDevPanel.test.tsx`)

**Regression Risk:**
- ⚠️ `TownLayoutGeneratorTests` signature changes (added `resolverVersion` parameter) - existing tests updated correctly
- ⚠️ `DevEndpoints.cs` changes to `ForceSaloonOverrideAsync` (property name changes `ForcedKind` → `ForcedCategory`) - this is a breaking change to existing dev saloon functionality, should be in a separate PR

### Senior Software Engineer Lens

**Code Quality:**
- ✅ Names are accurate and descriptive (`LayoutSalts`, `LayoutSaltDeriver`, `DevLayoutSaltsForced`)
- ✅ Error handling at the right boundaries (handlers throw domain exceptions, endpoints catch and return HTTP status codes)
- ✅ Each file has one clear responsibility
- ✅ Code follows existing patterns in the codebase
- ✅ Optional parameters with default `null` values for backward compatibility
- ✅ Null argument checks (`ArgumentNullException.ThrowIfNull`)
- ✅ Documentation comments on public APIs

**File Organization:**
- ✅ Domain types in `src/WildBunch.Domain/World/`
- ✅ Events in `src/WildBunch.Domain/Events/`
- ✅ Commands in `src/WildBunch.Application/*/Commands/`
- ✅ Handlers in `src/WildBunch.Application/*/`
- ✅ DTOs in `src/WildBunch.Application/*/Models/`
- ✅ Tests mirror source structure
- ✅ INDEX.md files updated

**Naming:**
- ✅ `LayoutSalts` clearly describes the domain concept
- ✅ `LayoutSaltDeriver` clearly describes the responsibility
- ✅ `DevLayoutSaltsForced` follows existing dev event naming pattern
- ✅ `PrepGameSessionHandler` / `StartGameSessionHandler` clearly describe phase

## Conditional Lenses

**Product Owner Lens:** Not invoked - this is a dev tool feature, not a player-facing feature

**Player Lens:** Not invoked - this is a dev overlay feature, not player-facing UI

## Architecture Skills Review

**DDD:**
- ✅ Value object (`LayoutSalts`) is immutable
- ✅ Domain event (`DevLayoutSaltsForced`) follows event sourcing pattern
- ✅ Aggregate root (`GameSession`) enforces invariants
- ✅ Factory method (`GameSession.StartPrepped()`) for aggregate creation

**CQRS/Event Sourcing:**
- ✅ Command/query separation maintained
- ✅ Events as source of truth
- ✅ Projections not modified (read models unchanged)

**Clean Architecture:**
- ✅ Domain layer has no dependencies on Application/Infrastructure
- ✅ Application layer depends on Domain abstractions
- ✅ Infrastructure layer not touched

**Wild Bunch .NET Architecture:**
- ✅ GameSession as aggregate root respected
- ✅ Event-sourced command flows used
- ✅ JSON snapshot cache not modified
- ✅ Persistence boundaries respected

## Frontend Review

**Frontend Standards:**
- ✅ `TownLayoutDevPanel` uses styled-components
- ✅ No inline styles
- ✅ Dev overlay pattern followed (registered in `DevPanelRegistry`)
- ✅ Surface context "town" used for contextual display

## Unslop Application

**Backend Architecture Profile:**
- ✅ DDD patterns followed
- ✅ CQRS patterns followed
- ✅ Event sourcing patterns followed
- ✅ Clean architecture layering respected

**Dev Overlay Profile:**
- ✅ Dev overlay pattern followed
- ✅ Dev-only endpoints under `/api/dev/`
- ✅ DevRoleGuard protection
- ✅ Dev DTOs separate from player DTOs

## Agent Discovery and Durable Guidance

**Durable Agent Guidance:**
- ✅ ADR-0036 created for Dev-Enabled Action Pattern
- ✅ ADR-0036 indexed in ADR INDEX.md
- ✅ ADR-0036 linked from AGENTS.md
- ✅ Implementation plan updated with completion status
- ✅ INDEX.md files regenerated (docs/INDEX.md, docs/adr/INDEX.md, docs/superpowers/INDEX.md)
- ⚠️ Skills refresh removed 47 stale skills - this is repo hygiene, not durable guidance for future work

**Tooling Issues:**
- ⚠️ Pre-existing build errors in `DevEndpoints.cs` (references to non-existent `LockRngHandler`, `ClearRngHandler`) - not fixed in this PR, documented in plan

## Tooling Hygiene

**Workspace Cleanliness:**
- ✅ No stray files
- ✅ No uncommitted debug artifacts
- ✅ No phantom files in parent directories
- ✅ Duplicate `.agents/docs/dev-enabled-action-pattern.md` removed (replaced by ADR-0036)

## Repo Improvement Check

**Fix-While-Here Opportunities:**
- ⚠️ **P1 Finding:** `DevEndpoints.cs` changes to `ForceSaloonOverrideAsync` (property name changes `ForcedKind` → `ForcedCategory`, additional parameters) - this is a breaking change to existing dev saloon functionality. This should be in a separate PR focused on saloon dev controls, not bundled with town layout salts work.
- ⚠️ **P2 Finding:** `LockRngHandler` and `ClearRngHandler` references in `DevEndpoints.cs` are broken (handlers don't exist) - this is pre-existing tech debt, but since we're already in the file adding town layout endpoints, we could have fixed these broken references or commented them out.

**Deferred Work:**
- Tasks 5-6 (frontend API functions and three-phase flow) skipped due to pre-existing DevEndpoints.cs build errors - documented in plan, tracked in Linear issue

**Pattern Perpetuation:**
- ✅ New code uses established patterns (ExecuteWithRetryAsync, DevRoleGuard, styled-components)
- ✅ No perpetuation of legacy patterns

## Test Coverage

**Test Kinds Used:**
- ✅ Unit tests (domain logic)
- ✅ Game-content tests (pipeline logic)
- ✅ Application tests (handlers)
- ✅ Frontend tests (component)

**Test Quality:**
- ✅ Tests assert on observable behavior
- ✅ Tests use real implementations where appropriate
- ✅ Tests cover edge cases
- ✅ Tests verify core feature (dev salts integration)

**Coverage Gaps:**
- ⚠️ No integration tests for the full three-phase flow (prep → inject → start) - this would require PostgreSQL validation lane, could be added as follow-up
- ⚠️ No API tests for the new dev endpoints - could be added as follow-up

## Specific Findings

### P1 Findings (Must Fix Before Merge)

1. **Breaking change to existing dev saloon functionality:** The changes to `ForceSaloonOverrideAsync` in `DevEndpoints.cs` modify the command signature (property name `ForcedKind` → `ForcedCategory`, added parameters `FoeSpeed`, `FoeFightStrength`, `FoeMinimumBribe`, `EncounterMessage`). This is a breaking change to existing dev saloon controls and should be in a separate PR.

### P2 Findings (Should Fix)

1. **Broken handler references in DevEndpoints.cs:** `LockRngHandler` and `ClearRngHandler` are referenced in endpoint handlers but the handlers don't exist. These should be commented out or fixed while we're in the file.

2. **Endpoints not registered in DependencyInjection.cs:** The new handlers (`GetTownLayoutSaltsHandler`, `SetTownLayoutSaltsHandler`, `GenerateRandomTownLayoutSaltsHandler`, `PrepGameSessionHandler`, `StartGameSessionHandler`) are not registered in DI. This means the endpoints will fail at runtime. This should be fixed.

3. **Prep and start endpoints not registered in DevEndpoints.cs:** The core implementation added the handlers, but the endpoints (`POST /api/dev/games/prep`, `POST /api/dev/games/{id}/start`) were not registered due to pre-existing build errors. This should be fixed after the broken handler references are resolved.

### P3 Findings (Nice to Have)

1. **Integration test for three-phase flow:** Add an integration test that exercises the full flow (prep → inject dev salts → start) to verify end-to-end behavior.

2. **API tests for new dev endpoints:** Add API tests for the new town layout dev endpoints to verify HTTP layer behavior.

## Conclusions

**Overall Assessment:** The backend implementation (Tasks 1-4) is architecturally sound and well-tested. The Dev-Enabled Action Pattern is properly documented as ADR-0036. However, there are P1 and P2 findings that must be addressed before merge.

**Recommendation:** Address P1 and P2 findings before merge. The breaking change to dev saloon functionality should be reverted and moved to a separate PR. The broken handler references and DI registration should be fixed.

**Next Steps:**
1. Revert changes to `ForceSaloonOverrideAsync` in `DevEndpoints.cs`
2. Comment out or fix `LockRngHandler` and `ClearRngHandler` references
3. Register new handlers in `DependencyInjection.cs`
4. Register prep and start endpoints in `DevEndpoints.cs` (after fixing broken references)
5. Consider adding integration and API tests as follow-up
