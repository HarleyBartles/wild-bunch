# AP-11 Saloon Diagnostics and Layout Metadata Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task inline. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Correct stale developer-only saloon diagnostics and make the current town-layout resolver version have one source of truth without changing generated layouts, replay, or wire output.

**Architecture:** `TownLayout` owns the persisted resolver identity as `CurrentResolverVersion`. GameContent uses that value both in layout hash input and generated layout metadata; Application DTO defaults reference the same Domain value. The existing version string remains `1.0.0`. The saloon developer explanation describes the actual eligibility predicate. A mislabeled hidden-truth test is redundant with the existing full developer-context scenario and will be removed.

**Tech Stack:** C#, xUnit, typed event snapshots, XML documentation, Markdown audit records.

**Spec:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`; source finding AP-11 in `.agents/investigations/stable-0.1.0/2026-10-07-application-layer-investigation.md` and its test disposition in `.agents/investigations/stable-0.1.0/2026-10-07-application-test-followup.md`.

**Execution Strategy:** `executing-plans` inline, with behavior-focused xUnit proof at the generated-layout output boundary.

## Global Constraints

- Preserve the current resolver value `1.0.0` and the exact hash input for every current generated layout. This is layout-algorithm metadata, not the application's SemVer release identity.
- Preserve existing persisted `TownLayout.ResolverVersion` values, event payload shape, JSON names, generated geometry, saloon candidate eligibility, and gameplay behavior.
- Do not add source-shape tests, diagnostic-copy snapshots, or tests that claim the developer DTO proves player serialization safety.
- Remove the redundant weak test that claims player serialization safety; the existing full developer-context scenario already asserts the known culprit identity and gate-aware candidate eligibility.
- No ADR or feature-matrix change is required because the code centralizes existing metadata and corrects narration without changing a durable contract or product promise.
- Advance `Directory.Build.props` exactly once to `0.1.0-dev.105` for this PR.
- In the first substantive commit, record PR #289's merge, exact-head hosted gate, and completed cleanup; retire the `.104` plan and route this `.105` successor from the roadmap.
- Deliver by PR to `develop`; require fresh whole-branch review and exact-head hosted CI success before merge. After merge, fast-forward the primary checkout, remove this clean worktree, delete its matching local and upstream branch refs, and verify all three are absent before starting another plan.

## Review Focus

- Layout generation reads one current resolver identity for both its deterministic hash and the persisted metadata while preserving today's byte-for-byte string input.
- The code correction does not add new retry, replay, compatibility, or migration behavior.
- The saloon explanation agrees with the actual eligibility rule: the culprit is excluded before release and eligible after the release gate opens.
- The surviving full developer-context test proves developer-only hidden-truth and eligibility output; no comment or test claims it establishes player DTO serialization safety.

---

### Task 1: Commit the JIT plan before execution

**Files:**
- Add: `.agents/plans/2026-10-12-row16-ap11-diagnostic-metadata.md`

- [x] Commit this plan by itself before changing source, tests, version, or successor bookkeeping.

### Task 2: Add generated-layout resolver identity behavior proof

**Files:**
- Modify: `tests/WildBunch.GameContent.Tests/NewGame/MapGeneratorLayoutSaltsTests.cs`

- [ ] Add a focused test that generates a normal world from the existing deterministic seed fixture and asserts every generated town layout reports the current resolver value `1.0.0`.
- [ ] Before the production change, temporarily make the current `MapGenerator` layout version disagree with the canonical value, run the focused test, and verify it fails on the output assertion; restore the source before continuing.
- [ ] Before centralizing the value, update the existing TownLayoutGenerator output test to exercise its default version and verify that test fails when the default is temporarily changed to a different value; restore the source before continuing.
- [ ] Keep existing same-seed layout and salt behavior checks; do not assert source strings, call counts, or internal implementation shape.

### Task 3: Correct AP-11 diagnostic and metadata ownership

**Files:**
- Modify: `src/WildBunch.Domain/World/TownLayout.cs`
- Modify: `src/WildBunch.Application/Games/Models/TownLayoutDto.cs`
- Modify: `src/WildBunch.Application/Dev/Mapping/SaloonDevContextMapper.cs`
- Modify: `src/WildBunch.GameContent/NewGame/LayoutDeterministicSource.cs`
- Modify: `src/WildBunch.GameContent/NewGame/MapGenerator.cs`
- Modify: `src/WildBunch.GameContent/NewGame/TownLayoutGenerator.cs`
- Modify: `tests/WildBunch.GameContent.Tests/NewGame/MapGeneratorLayoutSaltsTests.cs`
- Modify: `tests/WildBunch.GameContent.Tests/NewGame/TownLayoutGeneratorTests.cs`
- Modify: `tests/WildBunch.Application.Tests/Dev/GetSaloonDevContextHandlerTests.cs`
- Modify: `tests/WildBunch.Integration.Tests/EfGameSessionRepositoryTests.cs`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-07-application-layer-investigation.md`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-07-application-test-followup.md`
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `Directory.Build.props`
- Delete: `.agents/plans/2026-10-12-row16-application-command-pipeline-comments.md`

- [ ] Add `TownLayout.CurrentResolverVersion` with the existing value and use it for the Domain and Application fallback defaults.
- [ ] Replace duplicated production layout-version literals in `MapGenerator` with the canonical value for both `LayoutDeterministicSource` and `TownLayoutGenerator`.
- [ ] Have `TownLayoutGenerator` default to the canonical current value while retaining explicit historical/custom resolver input where its existing contract supports it; preserve all current hash inputs.
- [ ] Correct the `LayoutDeterministicSource` summary so it describes concern-scoped layout salts without claiming the retired dev override flow is active.
- [ ] Correct saloon candidate prose to include the culprit once the killer-release gate opens.
- [ ] Remove `HandleAsync_HiddenTruthDoesNotLeakIntoPlayerDtoSerialization`: it neither constructs nor serializes a player DTO, and `HandleAsync_ReturnsSaloonContext_WithHiddenTruthAndSuspects` already asserts the exact developer culprit, release state, and candidate eligibility.
- [ ] Update only existing expected resolver metadata assertions and fixture values needed by the canonical owner; keep independent historical/test values where they describe a distinct serialized scenario.
- [ ] Add dated partial AP-11 dispositions to the source and test follow-up records, preserving the original audit-time finding and recording any remaining subpart accurately.
- [ ] Record PR #289 source `4781bca66bafbac351fc1082192c8d90ac3b86e2`, merge `580d9935300316be72e116953f8dc8e10d9976f2`, and exact-head hosted gate run `38209642264`; state that the `.104` plan is retired here and this `.105` plan succeeds it.
- [ ] Advance the application version once, review `git diff --check`, run focused tests, then use the normal check-only commit hook. Run the complete canonical gate before whole-branch review and publication.

**Validation:** Clean develop baseline passed `py -3 tools/run.py ci --check` after ensuring PostgreSQL. Focused GameContent, Application and Integration tests must pass after implementation. The completing commit must pass the canonical staged-snapshot gate; fresh whole-branch review and hosted canonical CI must pass on the exact PR head before merge.
