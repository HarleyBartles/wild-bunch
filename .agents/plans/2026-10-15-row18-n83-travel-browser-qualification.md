# Row 18 N-83 Travel Resume Browser Qualification Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Qualify a public-browser journey from the player's starting town into an active trip, then prove the same server-owned destination and legal travel action return after the page is reopened.

**Architecture:** Extend the production-browser test using the existing local API, disposable PostgreSQL database, and real public UI. Select a connected destination from the returned world map, start travel through the rendered map and preview, and assert the event-derived active journey after reload; existing Domain and PostgreSQL tests remain owners of day-resolution, encounter, arrival, and replay rules.

**Tech Stack:** Playwright Chromium, production-mode Vite build, ASP.NET API, PostgreSQL 16, Python command bus, and pytest.

**Spec:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md), especially PG-003, PG-007, PG-008, PLAT-001, and release exit; [feature matrix](../../docs/features.md), especially PG-007; [ADR-0028](../../docs/decisions/ADR-0028-onion-ddd-cqrs-event-sourcing-and-projections-posture.md), [ADR-0046](../../docs/decisions/ADR-0046-pointer-operated-town-selection-for-0.1.0.md), and [ADR-0048](../../docs/decisions/ADR-0048-gitflow-and-semver-release-lifecycle.md).

**Execution Strategy:** `executing-plans` - setup, town choice, departure, and restored in-progress travel form one sequential public-player journey with shared browser storage and a real persisted session; inline execution preserves the connected state and needs one whole-branch review.

## Global Constraints

- Advance the sole authored product version in `Directory.Build.props` exactly once to `0.1.0-dev.201` for this one PR; reconcile it with the latest merged develop version before publication.
- Keep the shared map's accessible name truthful in both start and travel contexts; this does not add keyboard town selection.
- Use the public production web build, actual player endpoints, and the isolated PostgreSQL-backed API; do not use dev endpoints, overrides, hidden truth, mocked game responses, or test-only application routes.
- Select an actual connected destination returned by the world map and use the rendered public travel controls to preview and depart.
- Keep the server and event stream authoritative. The browser may retain only the existing selected game pointer and must restore current journey state from the API.
- Limit production fixes to a defect reproduced against the settled PG-007 contract; preserve CQRS, event authority, read/write separation, and replay.
- Do not expand this route into encounter, arrival, resource-balance, store, or casebook feature work; those require their own current-scope assessment and JIT plan.
- Do not create or change an ADR. The route qualifies an existing player promise and makes no durable architecture decision; compare the actual diff with the named ADRs before review.

## Review Focus

- The map selection must be a destination connected to the player's actual current town, and the real preview and departure must agree on that destination; assert both in the browser test.
- Reopening must restore the server's active journey and destination and expose the normal trail action without issuing a second travel command; assert the restored GET payload, visible surface, and request count.
- The journey state must come from the real session projection. Mutation proof temporarily removes the journey mapping from the read-model DTO, rebuilds the API, and confirms the browser test fails on the missing restored travel state.

---

### Task 1: Commit the successor checkpoint and retire N-82

**Files:**
- Create: `.agents/plans/2026-10-15-row18-n83-travel-browser-qualification.md`
- Delete: `.agents/plans/2026-10-15-row18-n82-saloon-take-in-browser-qualification.md`
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `Directory.Build.props`

**Interfaces:**
- Consumes: N-82 PR #385 reviewed source `5889378becb6426db94da2ed8cc9d36d275abfb4`, squash merge `73054ca0e5a8c7613b5d8a754a7e4eaad6558392`, exact-head hosted gate `38776773784`, develop-push gate `38777648999`, fresh whole-branch review with no actionable findings, and verified paired worktree/local/upstream branch cleanup.
- Produces: the N-82 dated delivery record and plan retirement; N-83 as row 18's active plan; version `0.1.0-dev.201`.

- [x] **Step 1: Reconcile live branch and delivery evidence**

Verify `HEAD` and `origin/develop` are `73054ca0e5a8c7613b5d8a754a7e4eaad6558392`; verify PR #385 is merged at that commit, its recorded head is `5889378becb6426db94da2ed8cc9d36d275abfb4`, both hosted runs passed on their respective SHAs, and the N-82 worktree and matching branches are absent. Preserve the unrelated user edit in the main checkout.

- [x] **Step 2: Reconcile successor artifacts and version**

Record N-82's tested route and exact delivery identities in the dated roadmap history. Set row 18's active plan to N-83 and clarify that active travel restoration is one bounded route while full journey, remaining retained-path, full-candidate, and hosted-release qualifications remain open. Remove N-82's plan only after confirming the entire plan scope shipped. Set `Directory.Build.props` to `0.1.0-dev.201`.

- [x] **Step 3: Commit the plan checkpoint**

Run `git diff --check`, stage only the plan, roadmap, `Directory.Build.props`, and N-82 plan retirement, then commit through the normal check-only hook. Do not run the full canonical gate immediately before this commit; the hook validates the exact staged candidate.

### Task 2: Prove public travel departure and restoration

**Files:**
- Modify: `src/WildBunch.Web/browser-tests/player-startup.spec.ts`
- Modify: `src/WildBunch.Web/src/components/start-flow/PhaserMapHost.tsx`
- Inspect: `src/WildBunch.Application/Games/Mapping/GameSessionMapper.cs`
- Inspect: `.agents/doctrine/frontend-standards.md`, `.agents/doctrine/validation-policy.md`, and `.agents/playbooks/ui-browser-check.md`

**Interfaces:**
- Consumes: production preview at `http://127.0.0.1:4174`, isolated API at `http://127.0.0.1:5275`, `seedWorldSeedCode()`, `GET /api/games/{id}/world-map`, `GET /api/games/{id}/travel/preview/{destinationTownId}`, `POST /api/games/{id}/travel`, and the existing public React/Phaser start and trail surfaces.
- The existing browser test's world-map response has towns `{ id, name, x, y }` and will gain trails `{ id, fromTownId, toTownId, rideDayDistance }`; extract its existing map-to-canvas coordinate calculation into `clickTownOnWorldMap(page, map, town)` and reuse it for the starting choice and travel destination.
- The travel preview response is `{ success, message, preview }`; the active journey response exposes `originTownId`, `destinationTownId`, and `status`.
- Produces: one browser test that proves a map-connected trip is started through the player UI and restored from the real API after closing and reopening the page, plus one shared test helper for pointer selection on the shared world map.

- [x] **Step 1: Add the travel-resume browser behavior**

Change the shared Phaser map accessible name to `World map`, since the same component displays both initial town choices and later travel destinations. Create a fresh browser context and start the production web preview with a unique player name, Boring entropy, and the existing deterministic `SeedWorld` helper. Acknowledge the prologue, obtain the world map from the real response, select the first displayed town through the `World map` visual using Phaser pointer input, and wait for the normal town surface. Use the visible `Hit the trail` town control. From the current town and `world-map.trails`, derive a connected destination; select its returned map coordinates through the rendered `World map`. Wait for the real preview request and assert the preview names that destination before clicking `Start the ride`.

Wait for the real `POST /api/games/{id}/travel`, assert success, and verify its returned session has an active journey with the chosen destination and the selected town as origin. Close the page but keep the same browser context. Reopen the production URL, await `GET /api/games/{id}`, and verify the response still reports that active journey and destination, the normal trail surface shows `Advance travel day`, and no second travel POST occurred. Close the context in `finally`.

- [x] **Step 2: Witness a behavior failure with a controlled mutation**

After the new browser test passes, temporarily change the `GameSessionMapper.ToDto(DomainGameSessionReadModel)` mapping to return no journey. Rebuild the API and production web output, run only the new browser test, and confirm it fails because the restored server response no longer reaches the active travel surface. Restore the mapper, rebuild, and rerun the focused browser test. Do not commit the mutation.

- [x] **Step 3: Fix only a reproduced contract defect**

If the browser journey exposes a production defect, first add or refine the narrowest behavior test that fails for that defect, then repair the owning production seam and rerun the new route plus its focused owner tests. If the observed issue requires new product behavior or changes an ADR, stop that dependent change and return to the user; do not broaden this qualification slice.

### Task 3: Reconcile evidence and deliver N-83

**Files:**
- Modify: `docs/features.md`
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Inspect: applicable ADRs, the feature-matrix playbook, UI browser-check playbook, PR runbook, and release runbook.

**Interfaces:**
- Consumes: the committed N-83 browser behavior, its witnessed mutation failure and restored passing state, and the current develop version/PR workflow.
- Produces: dated PG-007 browser evidence, an accurate row-18 successor scope, reviewed PR to `develop`, passing exact-head hosted CI and develop-push CI, and verified paired worktree/branch cleanup.

- [x] **Step 1: Update current feature and roadmap truth**

Add dated PG-007 evidence that states exactly that one production-browser journey starts travel to a map-connected destination and restores the active destination and legal trail action after page reopen. Do not claim travel-day advancement, encounter resolution, arrival, all PG-007 behavior, deployed resume, or overall release qualification. Record N-83 delivery identities, checks, review and cleanup in the roadmap; select the next remaining row-18 scope from then-current evidence.

- [x] **Step 2: Run an interactive browser playtest of the same route**

Use this worktree's production web build and PostgreSQL-backed API, confirm the browser uses the matching worktree API, then play the same setup/map/departure route through the visible controls. At desktop width, inspect the world map and active trail page for readable layout and browser-console or game-API failures; capture a screenshot of the active trail state under this branch's scratch directory. Keep this visual/manual result distinct from the automated assertion result. Stop only servers this worktree started; leave shared PostgreSQL running. Completed with the production preview at `http://127.0.0.1:4174`, worktree API at `http://127.0.0.1:5276`, and disposable PostgreSQL database `wildbunch_n83_manual_20261015`. The route selected Thunder Butte to Barrel Cactus, resumed after closing/reopening, showed `Advance travel day`, made no repeated departure POST, and had no console errors on the reopened page. Screenshot: `Z:\_agent-scratch\wild-bunch\codex\row18-n83-travel-browser-qualification\travel-resume.png`. The earlier attempt against shared dev data returned the expected 409 because that database already had an active game; it was not reset or changed.

- [x] **Step 3: Run focused proof and the canonical gate**

Run `npm run build` and `npm run test:browser` in `src/WildBunch.Web` after PostgreSQL is ensured by `./tools/postgres-dev.ps1 ensure`. Run `py -3 tools/run.py ci --check` after the source and evidence are complete, and commit through the normal check-only hook. The canonical gate is fail-fast and cheapest-first. Focused browser suite passed 7/7, including N-83; the focused `TravelPrepSurface` suite passed 11/11 after two stale `/trail map/i` expectations were updated to `/world map/i`. The complete gate passed: 8 Python tool tests; .NET build with zero warnings/errors; Domain 534 passed with one pre-existing skip; API 1, Application 163, GameContent 159, Integration 332; Vitest 377; and browser 7. The first gate run stopped at web tests because those two component assertions retained the retired label; the focused suite then passed and the full gate was rerun successfully.

- [ ] **Step 4: Review, publish, merge, and retire the execution unit**

Run completion-readiness, create the review package, and request a fresh whole-branch review of the committed diff with the plan, spec, review focus, and any rulings. Resolve Critical and Important findings with witnessed RED/GREEN and rerun the canonical gate. Publish or update a Draft PR to `develop`, verify its head and PR body, then make it reviewable. Confirm exact-head hosted CI passes before merge; verify develop-push CI passes on the merge. Refresh the main checkout without disturbing the unrelated user edit, use `finishing-a-development-branch` and its remove-worktree `--check` then `--apply` script, remove only this plan's matching local/upstream branch refs, and verify all three are absent.

This slice establishes travel departure and restoration only. It does not claim a completed trip, travel-day/encounter/arrival browser coverage, store/casebook browser coverage, full 0.1.0 qualification, hosted release workflow, deployment, user ownership, or cloud playability.
