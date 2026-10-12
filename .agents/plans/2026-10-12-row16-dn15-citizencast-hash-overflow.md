# Citizen Cast Hash Overflow Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task inline. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Keep deterministic citizen-role and feature selection from throwing when an accepted town/day/turn/visit input produces `int.MinValue` in its stable hash.

**Architecture:** Preserve `CitizenCast` as a pure deterministic selector. Map only `int.MinValue` to `int.MaxValue` before taking an absolute value, preserving the current hash and modulo outcome for every other input. Exercise both the role-hash and feature-hash branches through the public `CitizenCast.Select` API; the inputs use authored town IDs and positive `int` visit values, while remaining outside ordinary play frequency.

**Tech Stack:** C#/.NET 10, xUnit, Wild Bunch Domain, repository command bus.

**Spec:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`; row 16 in `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`; DN-15 in `.agents/investigations/stable-0.1.0/2026-10-07-domain-layer-investigation.md`.

**Execution Strategy:** `executing-plans` inline. The two deterministic collisions share one pure helper, one correction, and one focused test file; the remaining disposition, feature evidence, version, and successor retirement belong to the same bounded PR. A fresh whole-branch review will assess the complete delivery.

## Global Constraints

- Keep `Directory.Build.props` as the sole authored application-version authority and advance exactly once to `0.1.0-dev.113` in the first substantive commit.
- Preserve role and feature selection for all hash values other than `int.MinValue`; do not change inputs, candidate ordering, feature ordering, or modulo mapping.
- Keep the selector pure. Do not add session mutation, events, persistence, or developer controls to `CitizenCast`.
- Preserve event authority: the normal saloon command continues to record the selected citizen through its existing event, and replay does not reroll the selection.
- Keep the accepted `.113` plan and its parent roadmap/spec through its completing PR; retire the completed `.112` plan and record PR #297 review, exact-head CI, merge and paired worktree/ref cleanup in the first substantive commit.
- Deliver through a reviewed PR to `develop`; require hosted CI to pass on the exact reviewed head, fast-forward the primary checkout, then remove the clean worktree before deleting and verifying its local and upstream refs.

## Review Focus

- Both deterministic selector branches must tolerate a hash equal to `int.MinValue`; one test reaches role selection and a second reaches feature selection without the role hash failing first.
- The fix must preserve every non-`int.MinValue` hash result and existing selection mapping.
- The test evidence covers the public pure selector at extreme but valid integer inputs; it does not claim a normal player can accumulate those visit counts or prove a browser/deployed journey.

---

### Task 1: Commit the JIT plan before implementation

**Files:**
- Add: `.agents/plans/2026-10-12-row16-dn15-citizencast-hash-overflow.md`

- [x] Commit this plan by itself before editing source, tests, investigation, feature matrix, roadmap or version files. Keep the version at `0.1.0-dev.112` in this plan-only commit.

### Task 2: Prove both CitizenCast hash overflows and fix the shared conversion

**Files:**
- Modify: `tests/WildBunch.Domain.Tests/CitizenCastTests.cs`
- Modify: `src/WildBunch.Domain/Game/CitizenCast.cs`

**Interfaces:**
- Consumes: `CitizenCast.Select(TownId, int, int, int, IReadOnlyList<string>)`, authored town IDs from `SeedWorldFactory`, and the current deterministic stable-hash algorithm.
- Produces: Behavioral tests proving role selection and feature selection complete when their respective existing hashes equal `int.MinValue`.

- [x] Add a role-hash test using `new TownId("cinnabar")`, day `1`, turn `1`, visit `116839254`, and `SampleFeatures`. Assert that `CitizenCast.Select` returns a role in `CitizenCast.Roles` and a feature in `SampleFeatures`.
- [x] Run only the role-hash test with `dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --filter "FullyQualifiedName~CitizenCast_Select_WhenRoleHashIsIntMinValueStillReturnsEncounter"`; confirm it fails because `Math.Abs(int.MinValue)` throws `OverflowException`.
- [x] Add a feature-hash test using `new TownId("emberfall")`, day `1`, turn `1`, visit `1536863146`, and `SampleFeatures`. Assert the returned role and feature belong to their respective configured collections.
- [x] Run only the feature-hash test with `dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --filter "FullyQualifiedName~CitizenCast_Select_WhenFeatureHashIsIntMinValueStillReturnsEncounter"`; confirm it fails in the feature-selection branch because `Math.Abs(int.MinValue)` throws `OverflowException`. The role hash for this case is not `int.MinValue`.
- [x] Change `StableHash` to return `int.MaxValue` only when the unchecked hash equals `int.MinValue`; otherwise keep `Math.Abs(hash)`. Do not change input concatenation or modulo selection.
- [x] Run both focused tests and `dotnet test tests/WildBunch.Domain.Tests/WildBunch.Domain.Tests.csproj --filter "FullyQualifiedName~CitizenCastTests"`; confirm all pass.

### Task 3: Record the bounded disposition and successor delivery

**Files:**
- Modify: `.agents/investigations/stable-0.1.0/2026-10-07-domain-layer-investigation.md`
- Modify: `docs/features.md`
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `Directory.Build.props`
- Delete: `.agents/plans/2026-10-12-row16-dn15-saloon-hash-overflow.md`

**Interfaces:**
- Consumes: Task 2's two passing selector tests, PR #297's verified squash merge and exact-head hosted gate, and confirmed worktree/ref cleanup.
- Produces: A dated partial DN-15 disposition, matching PG-006 evidence with its scope limit, an accurate row-16 successor entry at `0.1.0-dev.113`, and retirement of the completed `.112` plan.

- [x] Extend the dated DN-15 disposition: the `cinnabar` role hash and `emberfall` feature hash both reproduced `int.MinValue` for positive integer inputs; the one-value fallback resolves each while preserving every other hash result. Keep the evidence limit explicit: these extreme visit values prove accepted selector inputs, not an ordinary or browser-reachable player journey.
- [x] Update PG-006 evidence to include the two direct `CitizenCast.Select` boundary tests and retain the no-browser/deployed-journey limitation. Do not change its promise, dependencies, or release disposition.
- [x] Retire the `.112` plan and update row 16 to point to this `.113` plan. Record PR #297 reviewed source `fa62710ce53964f75a8fc6ad4a97ded67505b0bc`, squash merge `b75f5d97e5aa20f33e0c0a6958ce745c2dec991b`, fresh whole-branch approval, exact-head hosted gate run `38246011126`, and that the clean worktree was removed before both branch refs and all three absences were verified. Note the branch head is not an ancestor of develop because integration was squash; verify the merge commit in develop.
- [x] Change `Directory.Build.props` exactly once from `0.1.0-dev.112` to `0.1.0-dev.113` and ensure the production web build identity still derives from this shared authority.
- [x] Re-read the relevant ADRs and the proposed diff; state that no durable decision changes because the fix preserves deterministic mapping except the previously throwing boundary.
- [ ] Run the focused tests, stage the intended successor retirement, dated dispositions, feature evidence, roadmap delivery proof and version change, then commit them together and let the check-only pre-commit hook run the canonical fail-fast gate on the staged candidate. Resolve any rejection through its owning repair command, restage deliberately, and retry the commit; reuse the successful hook result for the committed head.
