# Use one world map for selection and travel

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task.

**Goal:** Make new-game town selection and later travel consume one seeded world-map read contract, with no parallel static starting-town list or duplicate map route.

**Architecture:** The generated session world is the source for both map views. Application owns one map query and DTO, API exposes one session world-map route, and the browser uses that route for both the starting-town step and travel selection. The start command still validates the selected town and records the one free first arrival.

**Execution Strategy:** Native inline, sequential execution. The application/API contract, browser consumers, retired catalog and successor artifacts are tightly coupled to the same one-map decision; one implementation context avoids repeated handoffs, followed by one fresh whole-branch review.

**Spec:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`, especially PG-003 and row 08: one world map serves opening selection and later travel under their respective legal interaction rules; no duplicate map authority or setup-owned travel dependency.

**Roadmap:** Row 08, `Restore strict read ownership and honest projections`.

**Scope:** Replace the duplicate `starting-town-map` and `world-map` aliases with one typed world-map query, DTO, endpoint and browser client path; remove the unused static `starting-towns` endpoint/catalog after confirming no runtime consumer; preserve access to the generated session map before and after the first-town selection; keep start selection and travel interaction rules distinct.

**Out of scope:** Changing setup/prologue/first-arrival semantics, town eligibility, map generation or geometry, travel legality, rendered map behavior, account ownership, gameplay features, or compatibility for pre-alpha API clients.

**Review Focus:** Prove the setup-phase map comes from the current session world and can supply the town used by the existing first-arrival flow; verify missing-session behavior and hidden-truth safety; confirm both browser flows use the same world-map client; establish that removed static/alias paths have no live runtime consumers.

## Global Constraints

- Follow `.agents/playbooks/unslop.md` and select the backend architecture, play-surface UI and code-review profiles for the changed concerns.
- Follow `.agents/playbooks/decision-records.md`; ADR-0039 already decides that one world map supports initial town selection and later travel, so preserve it and add no implementation inventory or new decision.
- Follow `.agents/playbooks/feature-matrix.md`; PG-003 remains the same product promise, so update `docs/features.md` only if source inspection shows current assessed behavior or disposition changes.
- Keep gameplay authority in the generated `GameSession.World`; the map is a safe projection and cannot expose culprit or hidden case facts.
- Preserve map availability during setup and after `GameStarted`; choosing a town remains one command that places the player there without journey time.
- Do not add a negative test whose only purpose is to assert an obsolete route returns 404; remove duplicate/static endpoint tests and retain behavior tests for the supported map and selection flow.
- Demonstrate any new behavior test fails for its intended missing behavior, restore the implementation and verify green.
- Use the refreshed `develop` base and version `0.1.0-dev.60`. PR #244 delivered `.59` from `ec0df848522e112bd99115801c15267fd55d11b0`, merged as `f38276e0d58d8169ae2f477bd376cccc0152ec6b`; exact-head hosted gate `38038222347` and develop gate `38038568890` passed.
- Retire `.agents/plans/2026-10-10-setup-projection-unknown-state.md` in this successor after checking its entire setup-response/HUD scope against PR #244 and hosted evidence; retain the baseline spec and roadmap.

## Task 1: Commit the successor handoff and version identity

**Files:** `.agents/plans/2026-10-10-setup-projection-unknown-state.md`, `.agents/plans/2026-10-10-unify-world-map-read-surface.md`, `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`, and `Directory.Build.props`.

- [x] Verify PR #244 and its merge/develop gate against live GitHub evidence, confirm its full plan scope is present in `develop`, then retire the completed plan in this first substantive successor commit.
- [x] Update row 08 with PR #244 source/merge SHAs and exact-head/develop gate run IDs; point the active plan link at this plan and name this slice as the `.60` target.
- [x] Set the sole authored application version to `0.1.0-dev.60`; do not add duplicate web/package versions or commit generated identity output.
- [x] Commit the handoff before implementation so the execution source is durable and the completed predecessor is preserved in Git history.

## Task 2: Expose one Application/API world-map contract

**Files:** Application map queries and DTOs, API game endpoints and registrations, seeded-world map generation and static catalog, web map types and consumers, focused application/integration/browser tests, and `LICENSE-ASSETS.md`.

- [x] Rename the Application query, handler and response types to describe the session world map, then retain exactly one `GET /api/games/{id}/world-map` route backed by that use case.
- [x] Remove `GET /starting-towns` and `GET /{id}/starting-town-map` plus their handler registrations; `rg` confirms the static list has no runtime caller and the second route is an alias to the same handler.
- [x] Retire `StartingTownCatalog` and the parameterless map-town projection; retain the canonical-world fixture used by starting-town policy tests and correct comments that described the obsolete start-screen world.
- [x] Retain the real missing-session and hidden-truth assertions on the supported world-map route; remove fixed map counts, duplicate-alias equality, and catalog-comparison tests that only restate the implementation. Remove the retired catalog from the creative-content inventory.
- [x] Strengthen the Application handler test to compare the returned map towns/trails with the independently loaded setup session's generated `World`, including identities and coordinates. The test failed when projection was mutated to use canonical coordinates, then passed after restoring the session-world projection.
- [x] Update the existing PostgreSQL setup-to-start integration flow to get its selected town from `GET /world-map` before sending the existing start command; assert the session map remains the same after start. The test failed when the map route was temporarily renamed, then passed after restoration.
- [x] Run the focused Application and PostgreSQL integration lanes; no route-absence or duplicate first-arrival test was added.

## Task 3: Make both browser flows consume the shared map

**Files:** `src/WildBunch.Web/src/api/types.ts`, `src/WildBunch.Web/src/api/wildBunchApi.ts`, `src/WildBunch.Web/src/components/start-flow/StartingTownStep.tsx`, `src/WildBunch.Web/src/components/start-flow/PhaserMapHost.tsx`, `src/WildBunch.Web/src/flow/TravelPrepSurface.tsx`, `src/WildBunch.Web/src/tests/StartingTownStep.test.tsx`, `src/WildBunch.Web/src/tests/PhaserMapHost.test.tsx`, `src/WildBunch.Web/src/tests/TravelPrepSurface.test.tsx`, `src/WildBunch.Web/src/tests/StartFlow.test.tsx`, and any test mocks that still name the retired client functions.

- [x] Use the single `WorldMapDto`/world-map client for starting-town selection and travel; both React Query reads share the `world-map` key scoped by session ID.
- [x] Rename the shared map scene/type to `WorldMap` where the current `StartingTownMap` name claims the wrong lifecycle; preserve existing Phaser rendering and phase-specific selectable-town rules.
- [x] Remove `getStartingTownMap`, `getStartingTowns`, and test-only mocks/imports that have no remaining runtime consumer.
- [x] Preserve player-visible setup selection and travel destination behavior with existing component tests against the shared map DTO; no source-name or query-key assertion substitutes for behavior.
- [x] Run the web format/lint/type-check/test/build lane: 42 files and 302 tests passed; production build succeeded. No setup component requests the retired catalog or duplicate route.

## Task 4: Review and publish the slice

- [x] Run `py -3 tools/run.py ci --check` on the intended staged/committed candidate; the focused Application, Integration and Web lanes passed and PostgreSQL was healthy.
- [x] Compare the final diff with ADR-0039, PG-003 and the row 08 specification; this slice leaves their durable decision and feature promise unchanged, so neither ADR nor feature matrix requires an update.
- [x] Perform a fresh whole-branch review against the final plan, applicable backend/web/code-review profiles, actual ADRs, and feature matrix; review found no actionable issues. The documented keyboard-selection gap is unchanged and outside this slice.
- [ ] Publish a Draft PR to `develop`, verify its exact head and scope, set it ready for hosted checks, and wait for the exact-head canonical gate to pass.
- [ ] Merge to `develop` only after the required review and exact-head gate pass; verify the develop push gate on the merge commit, then update the roadmap with the source SHA, merge SHA, both run IDs and the next row 08 target in the next substantive successor slice.
