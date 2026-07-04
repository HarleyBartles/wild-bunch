# Task 1 Report — Discovery Pass

**Plan:** 1e — StartNew Cleanup
**Task:** 1 — Enumerate all remaining StartNew call sites and test failures
**Status:** DONE

## Work Performed

1. **Grepped for `GameSession.StartNew` in test files** — found 65 files containing 119 total call sites (rg with count).
2. **Ran Domain.Tests** — 525 passed, 0 failed (294 ms).
3. **Ran Application.Tests** — 204 passed, 0 failed (325 ms).
4. **Integration.Tests** — SKIPPED; `docker` is not available on this machine. Testcontainers/PostgreSQL tests cannot run. Assessed the 8 Integration files by static inspection only.
5. **Cross-referenced** the 65 StartNew files against event-assertion patterns (`UncommittedEvents.Single()`, `.Count`, index access, absolute `Version`, stored-event stream composition) to determine which files need INLINE migration vs FACTORY_DELEGATE, and which assertions break.
6. **Wrote discovery report** to `.agents/superpowers/sdd/2026-07-03-geometry-first-plan-1e-startnew-cleanup/discovery-report.md`.
7. **Committed** the report (force-added past the sdd `.gitignore` `*` rule, matching the pattern used by prior plan reports).

## Key Findings

- **65 files, 119 call sites** total. The plan brief estimated "65 call sites" but the actual count is 65 *files* containing 119 call sites.
- **0 current test failures** — the legacy `StartNew` factory still exists in `GameSession.cs` (L950-1024), so all tests pass. Failures will emerge per-file as call sites are migrated to the canonical 4-event flow.
- **4 files need INLINE migration** (high-risk, assertion updates required):
  1. `ClockTurnCorrectionTests.cs` — captures `GameStarted` via `Single()` for replay-stream construction
  2. `BountySaloonEventSourcingTests.cs` — same pattern
  3. `EventSourcingEndToEndTests.cs` (Integration) — asserts `Single()`, `Version==1`, event-stream count=3, event types/indices
  4. `EventStorePersistenceTests.cs` (Integration) — asserts stored-event count=3, `storedEvents[0]=="GameStarted"`, indices
- **59 files need FACTORY_DELEGATE migration** — replace with `TestSessionFactory.StartGameCanonical`; no assertion changes expected (most call `MarkEventsCommitted()` before asserting on action events).
- **2 files need minor comment-only updates** (`GameSessionArchiveTests.cs` L34, `EfGameSessionRepositoryTests.cs` L70) — comments reference `StartNew`/`GameStarted` but assertions use `OfType` filtering or version deltas that remain valid.
- **Important caveat:** `TestSessionFactory.StartGameCanonical` defaults `gameDifficulty` to `Easy` while legacy `StartNew` defaults to `Standard` — each call site's difficulty expectation must be verified during migration.

## Commits

- `81bf354` — `docs: Plan 1e discovery report -- enumerate remaining StartNew call sites`

## Concerns

- Integration tests (8 files, 22 call sites) could not be run — Docker is unavailable. The 2 INLINE Integration files are the highest risk and must be verified with Docker before Plan 1e closes.
- The `gameDifficulty` default mismatch (`Easy` vs `Standard`) between the factory and legacy `StartNew` could cause subtle behavior differences if not checked per call site.
