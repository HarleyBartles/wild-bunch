# Town Hub Value Bounds Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use executing-plans to implement this plan task-by-task inline. Steps use checkbox syntax for tracking.

**Goal:** Enforce the existing town-hub surface coordinate contract at the Domain value boundary, require positive rendered building dimensions, and reconcile DN-15 with all already-delivered fixes.

**Architecture:** BuildingPlacement owns placement-center and display-size validity; TownLayout owns player-spawn coordinate validity. Preserve positional record construction, deconstruction, equality, init updates, and JSON-visible properties. This changes no aggregate events, application commands, persistence schema, or player capability.

**Tech Stack:** C#/.NET 10, xUnit, Vitest, Wild Bunch Domain, GameContent, application mapping, and repository command bus.

**Spec:** .agents/specs/2026-10-07-stable-0.1.0-baseline.md; DN-15 in .agents/investigations/stable-0.1.0/2026-10-07-domain-layer-investigation.md; row 16 in .agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md.

**Implementation Guide:** .agents/runbooks/implementing.md; execute inline with executing-plans.

**Execution Strategy:** executing-plans inline. The two Domain values represent one town-hub geometry contract and their tests are coupled; editing their producers or consumers in parallel would increase context and review cost without yielding independent behavior.

## Global Constraints

- Start from refreshed origin/develop in this dedicated worktree and use a unique codex/row16-dn15-building-inputs branch.
- Keep Directory.Build.props as the only authored application version and advance exactly once to 0.1.0-dev.116 in the first substantive implementation commit, not the plan-only commit.
- BuildingPlacement X and Y are center coordinates and must be within inclusive 0-100; Width and Height must be positive integers because the scene renderer uses them as display sizes.
- TownLayout PlayerSpawnX and PlayerSpawnY must be within inclusive 0-100, matching the documented logical surface.
- Do not require a building footprint to remain inside the surface. Width and height describe the rendered footprint around a center; surface clipping is a consumer concern and no stronger Domain rule is established.
- Preserve all positional constructors, generated deconstruction/equality, and serialized property names and values. Validate direct construction and record with updates through the properties.
- Do not change TownLayout collection or TileGrid shape, mutability, nullability, or content rules; DN-09 and the remaining conditional DN-15 grid observation stay open.
- Do not change DTOs, API behavior, event payloads, JSON schema, or database schema. Existing generated values and historical event shapes remain supported.
- Update the DN-15 narrative and per-file inventory to reflect the delivered BountyLoop/CitizenCast hash guards, nonnegative money contracts, and PathSegment bounds. Keep only unresolved TownLayout collection/grid and separately evidenced findings open.
- Correct the PhaserTownHubHost test fixture that currently uses spawn coordinates 120 and 130; the test proves object forwarding, so use valid values while preserving that assertion.
- No ADR or feature-matrix change is required because existing Domain comments define these surface coordinates and no product promise changes.
- In the first substantive implementation commit, record PR #300 source head 1999ec5749a122167d39d91cea7240a63ea87f80, squash merge 385a786b7181eb84b9697432b024fe0818263d3b, exact-head gate run 38263270305, develop-push gate run 38264164966, review disposition, and completed paired worktree/ref cleanup. Retire the completed .115 plan and make this .116 plan the active row-16 plan.
- Keep the active spec and roadmap, and this current plan, through their completing PR.

## Review Focus

- Direct construction and with updates reject each documented out-of-range coordinate and each nonpositive display dimension; both coordinate endpoints remain valid.
- TownLayoutGenerator continues to produce all supported layouts, including its standard trailheads and their edge-aligned footprints.
- Existing mapper and persisted-world paths preserve the same positional and JSON shape.
- The frontend fixture no longer relies on invalid server data to prove that the host forwards the current layout object.
- DN-15 no longer presents findings already fixed in PRs #297-300 as current unresolved defects.

---

### Task 1: Enforce town-hub coordinate and size contracts

**Files:**
- Modify: tests/WildBunch.Domain.Tests/World/TownLayoutTests.cs
- Modify: tests/WildBunch.GameContent.Tests/NewGame/TownLayoutGeneratorTests.cs
- Modify: src/WildBunch.Domain/World/BuildingPlacement.cs
- Modify: src/WildBunch.Domain/World/TownLayout.cs
- Modify: src/WildBunch.Web/src/tests/PhaserTownHubHost.test.tsx
- Modify: .agents/investigations/stable-0.1.0/2026-10-07-domain-layer-investigation.md
- Modify: .agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md
- Modify: Directory.Build.props
- Delete: .agents/plans/2026-10-12-row16-dn15-pathsegment-bounds.md

**Interfaces:**
- Consumes: BuildingPlacement positional values X, Y, View, Width, Height; TownLayout positional values PlayerSpawnX and PlayerSpawnY.
- Produces: the same public positional records, with invalid coordinates and nonpositive building dimensions rejected at construction and on init assignment.

- [x] Add behavior tests proving BuildingPlacement direct construction rejects X/Y values -1 and 101, Width/Height values -1 and 0, and with updates cannot bypass any of those checks.
- [x] Add behavior tests proving TownLayout direct construction and with updates reject PlayerSpawnX/PlayerSpawnY values -1 and 101; preserve valid endpoint construction for 0 and 100.
- [x] Run each new test against the unmodified values and confirm it fails because the positional constructors or generated init properties accept the invalid values.
- [x] Add validated init properties with backing fields to BuildingPlacement and TownLayout, initializing each field from its matching primary-constructor parameter and throwing ArgumentOutOfRangeException for an invalid value.
- [x] Add a focused positional API behavior test proving BuildingPlacement and TownLayout preserve record equality and deconstruction with the validated positional properties.
- [x] Run the focused TownLayoutTests suite and confirm construction, with updates, defaults, custom dimensions, positional equality/deconstruction, and boundary cases pass.
- [x] Extend the existing core-service generator behavior test to run every supported non-reserved BuildingLayoutPalette and verify each layout retains Store, Sheriff, Saloon, Telegraph, and Trailhead. Value constructors enforce geometry boundaries, so do not repeat their coordinate and dimension assertions here.
- [x] Run the complete TownLayoutGeneratorTests suite to verify every supported generated layout still constructs and remains reachable under the new invariants.
- [x] Update the PhaserTownHubHost forwarding test to use a valid layout payload and retain its identity assertion; run the focused Vitest file.
- [x] Run focused TownLayoutMapperTests and the existing PostgreSQL-backed world-cache recovery test to confirm application mapping and persisted-world JSON reconstruction retain their existing shape.
- [x] Correct DN-15 current prose and its per-file index: mark the delivered hash and money defects resolved, record the delivered BuildingPlacement and TownLayout spawn contracts, remove stale PathSegment/factory-bypass language, and leave collection/grid shape conditional where the contract is not settled.
- [x] Update row 16 with PR #300 delivery evidence, replace its active plan link with this .116 plan, and retire the completed .115 PathSegment plan in this successor slice.
- [x] Change Directory.Build.props exactly once from 0.1.0-dev.115 to 0.1.0-dev.116 in the first substantive implementation commit.
- [x] Inspect and stage the implementation, behavioral tests, investigation/roadmap update, .116 version, and .115 plan retirement together. Commit through the normal check-only hook; do not bypass it or run the complete gate redundantly before the hooked commit.

**Acceptance:** Both Domain types enforce only their documented coordinate/size boundaries at every public construction and init-update path. Valid generated layouts, the positional and JSON contracts, and historical persisted-world reconstruction remain intact. DN-15 and row 16 describe the current state truthfully, PR #300 is recorded with its exact hosted evidence, and its completed plan is retired in this successor.

**Validation:** Prove the new tests red before implementation and green afterward. Run `py -3 tools/run.py dotnet-test --check -- --filter FullyQualifiedName~TownLayoutTests`, `py -3 tools/run.py dotnet-test --check -- --filter FullyQualifiedName~TownLayoutGeneratorTests`, `py -3 tools/run.py dotnet-test --check -- --filter FullyQualifiedName~TownLayoutMapperTests`, and `npm --prefix src/WildBunch.Web test -- src/tests/PhaserTownHubHost.test.tsx`. Start PostgreSQL with `.\tools\postgres-dev.ps1 ensure`, then run `py -3 tools/run.py dotnet-test --check -- --filter FullyQualifiedName~WorldCacheRecovery_MissingRecordedTrailRebuildsWithoutWritebackAndRepairsOnSave`. The normal staged-snapshot hook must pass the fail-fast canonical gate on the implementation commit. Obtain a fresh whole-branch review, then require the exact-head hosted canonical gate and develop-push gate to pass before merge.

**Closeout:** Verify the PR head is unchanged, exact-head hosted validation passed, and the squash merge result is contained in develop. Fast-forward the primary checkout, remove the clean worktree first, then delete and verify the exact local and upstream branch refs. Confirm the worktree and both refs are absent before beginning the next plan.
