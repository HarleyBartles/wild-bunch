# Phase 5 Report: Event Round-Trip Tests

## What I Implemented

Created `tests/WildBunch.Domain.Tests/CaseFileGeneratedEventTests.cs` with three tests verifying that the `CaseFileGenerated` event's `CaseFileSnapshot` payload round-trips correctly through `FromDomain` → `ToDomain`:

1. **`CaseFileGenerated_CarriesCaseFileSnapshotThatReconstructsToIdenticalCaseFile`** — Builds a baseline caseFile with two suspects (one carrying a profile with an alias and an identity fact, plus trait tags), a true culprit, an opening lead, and a known clue (with linked suspect ids, source kind, source, and context). Snapshots via `CaseFileSnapshot.FromDomain`, reconstructs via `ToDomain`, and verifies suspects (id, name, status, profile aliases/facts, trait count) and known clues (id, kind, description, target kind, source kind, source, context, linked suspect count) all match.

2. **`CaseFileGenerated_PreservesTrueCulpritId`** — Verifies the true culprit id survives the round-trip.

3. **`CaseFileGenerated_PreservesOpeningLead`** — Verifies the opening lead survives the round-trip.

### Approach

Followed the established `WorldGeneratedEventTests` pattern: build the domain object directly, wrap it in the event, reconstruct from the event's payload, and assert on individual properties. The brief suggested using `TestSessionFactory`, but the factory's caseFiles ship with empty `knownClues`, which would leave the knownClues round-trip unexercised. To cover all four properties named in the brief (suspects/trueCulpritId/openingLead/knownClues), I built a baseline caseFile directly via a private `CreateBaselineCaseFile()` helper that mirrors the factory's suspect/lead shapes while populating knownClues. The helper reuses the same `Suspect`/`CaseOpeningLead`/`Clue` construction patterns found in `TestSessionFactory`.

Records with `IReadOnlyList` members use reference equality for those members, so I asserted on individual scalar properties rather than relying on whole-object equality — consistent with how `WorldGeneratedEventTests` asserts on town properties.

## What I Tested and Test Results

- **Build:** `dotnet build tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj` — 0 warnings, 0 errors.
- **Focused tests:** `dotnet test --filter "FullyQualifiedName~CaseFileGeneratedEventTests"` — 3/3 passed, 0 failed, duration 67 ms.
- **Full Domain.Tests suite:** `dotnet test tests/WildBunch.Domain.Tests` — 519/519 passed, 0 failed, 0 skipped, duration 219 ms. Output pristine (no warnings/noise).

## Files Changed

- **Created:** `tests/WildBunch.Domain.Tests/CaseFileGeneratedEventTests.cs`
- **Regenerated:** `tests/WildBunch.Domain.Tests/INDEX.md` (auto-generated mesh now lists the new test file)
- **Created:** `.agents/superpowers/sdd/2026-07-03-geometry-first-plan-1c-casefile-event-boundary/phase-5-report.md`

## Self-Review Findings

- **Completeness:** All three tests from the brief are implemented and exercise the full snapshot surface (suspects with profiles/aliases/identity facts, true culprit, opening lead, known clue with anchors via linked suspect ids).
- **Quality:** Names match the brief exactly. Assertions are explicit and behavior-focused.
- **Discipline:** No overbuilding — only the three specified tests, one private helper for the baseline caseFile.
- **Testing:** Tests verify real round-trip behavior (FromDomain → ToDomain), not mocked behavior. Output is pristine.
- **Note:** The index-mesh regeneration also surfaced pre-existing drift in other INDEX.md files (e.g. `CaseFileGenerated.cs`, `StartingTownSelected.cs` entries from prior phases). I left those out of this commit to keep it scoped to Phase 5; only the tests INDEX.md (containing the new test file entry) is included.

## Issues or Concerns

None.
