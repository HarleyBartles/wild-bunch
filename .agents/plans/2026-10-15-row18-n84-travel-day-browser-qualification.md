# Row 18 N-84 Travel-Day Browser Qualification Implementation Plan

> **For agentic workers:** Use `executing-plans` to implement this plan task-by-task. Steps use checkbox syntax for tracking.

**Goal:** Prove that one legal travel day advances through the public browser, persists the server-owned journey and journal state, and restores the correct next action after page reopen.

**Architecture:** Extend the existing production-browser path over the real API and an isolated PostgreSQL database. Keep the event stream and server session authoritative; assert independent day, journey-progress, diary, and rendered-action behavior after the command and a fresh page load.

**Tech Stack:** .NET 10, React, TypeScript, Playwright, PostgreSQL, GitHub Actions.

**Spec:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`, especially PG-007 and the travel event/replay, journal, and arrival contracts.

**Execution Strategy:** `executing-plans` - the checkpoint, browser behavior, validation, review, and publication are sequential and share one worktree.

## Global Constraints

- Work in a fresh worktree based on `origin/develop`; PR targets `develop`.
- Advance the authored application version once to `0.1.0-dev.202` in the planning checkpoint.
- Keep one version authority, check-only pre-commit/CI behavior, and cheapest-first fail-fast validation.
- Use the disposable PostgreSQL browser-test database; do not alter the shared development database.
- Keep the public UI, event stream, and server session as the only player/game truth; do not add identity, SignalR, cross-tab sync, or deployment behavior.
- Do not claim encounter resolution, completed arrival, full PG-007, full release qualification, or cloud playability.
- Record truthful PG-007 evidence and no ADR change unless inspection shows an actual durable-decision change.
- Retire the N-83 plan, matching worktree, and local/upstream branch together after verified merge and hosted develop gate.

## Review Focus

- A legal advance records exactly one day and reduces the chosen multi-day journey distance; assert the real command result and freshly loaded state, so a no-op day command fails.
- A one-day advance on a route with more than one expected day does not present arrival or permit premature arrival acknowledgement; verify remaining distance and the actual next legal browser action.
- If the deterministic day produces a pending encounter, restore its server-owned choice and keep day advancement unavailable; if it does not, restore the active journey's advance action.

---

### Task 1: Record N-83 delivery and commit the N-84 checkpoint

**Files:**
- Create: `.agents/plans/2026-10-15-row18-n84-travel-day-browser-qualification.md`
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `Directory.Build.props`
- Delete: `.agents/plans/2026-10-15-row18-n83-travel-browser-qualification.md`

**Interfaces:**
- Consumes: N-83 PR #386 reviewed source `72fc50f0053742abb7d0e35cab34eaff3f8767d2`, squash merge `cbf6b38bbabf11128a22660c7450669c5e40e20d`, exact-head hosted gate `38786311635`, develop-push gate `38787308937`, clean whole-branch review, and verified paired worktree/local/upstream branch removal.
- Produces: dated N-83 delivery evidence, N-84 as row 18's active plan, retired N-83 plan, and version `0.1.0-dev.202`.

- [x] **Step 1: Verify the merged N-83 source and delivery evidence**

Confirm PR #386 is merged to `develop` with recorded source `72fc50f0053742abb7d0e35cab34eaff3f8767d2` and merge commit `cbf6b38bbabf11128a22660c7450669c5e40e20d`. Confirm exact-head and develop-push gates passed on their exact SHAs, the independent whole-branch review reported no actionable findings, `origin/develop` contains the merge commit with the same tree as the source, and the N-83 worktree plus local/upstream refs are absent. Preserve the unrelated investigation edit in the main checkout.

- [x] **Step 2: Update the roadmap, version, and successor plan**

Add a dated N-83 delivery record stating its one-route travel departure/resume evidence, PR/source/merge identities, both passing hosted gates, clean review, and paired worktree/branch cleanup. Set row 18's active plan to N-84, state that N-83 proves active journey restoration only, and retain full travel-day, encounter, arrival, other retained-path, release-candidate and hosted-release qualification as open. Remove N-83's plan after classifying its complete scope as shipped. Set `Directory.Build.props` to `0.1.0-dev.202`.

- [x] **Step 3: Commit the plan checkpoint**

Run `git diff --check`, stage only the N-84 plan, roadmap, version file, and N-83 plan retirement, then commit through the check-only hook. Do not run the full canonical gate before this commit; the hook validates the exact staged candidate.

### Task 2: Qualify one travel-day advance and page-reopen recovery

**Files:**
- Modify: `src/WildBunch.Web/browser-tests/player-startup.spec.ts`
- Inspect: `src/WildBunch.Api/Games/TravelEndpoints.cs`
- Inspect: `src/WildBunch.Application/Games/Commands/AdvanceTravelDayHandler.cs`
- Inspect: `src/WildBunch.Domain/Game/GameSession.cs` and `src/WildBunch.Domain/Game/JourneyLoop.cs`
- Inspect: `.agents/doctrine/frontend-standards.md`, `.agents/doctrine/validation-policy.md`, and `.agents/playbooks/ui-browser-check.md`

**Interfaces:**
- Consumes: the production web preview at `http://127.0.0.1:4174`, disposable API at `http://127.0.0.1:5275`, `seedWorldSeedCode()`, the existing `clickTownOnWorldMap(page, map, town)` helper, the real world map and travel-preview queries, `POST /api/games/{id}/travel`, and `POST /api/games/{id}/travel/advance`.
- Produces: one browser scenario that starts a route with `expectedDays > 1`, advances one day from the restored active journey, and verifies persisted day/progress/diary state plus the legal post-day action after reopening the same browser context.

- [x] **Step 1: Reuse the existing public-browser journey setup without changing its behavior**

Extract the startup, map choice, real travel preview, and travel departure sequence in the current N-83 test into a named helper in `player-startup.spec.ts`, returning the game ID, selected origin and destination, departure clock day, and initial remaining distance. Choose an endpoint and the other endpoint of the longest connected trail returned by the real map so the route has sufficient distance; start the game at that endpoint and assert the real preview reports more than one expected travel day. Run the existing N-83 test and confirm direct departure/resume behavior remains intact before adding the new day scenario.

- [x] **Step 2: Add the browser behavior for one real travel-day advance and restore**

Use the helper to start a multi-day route. Close the page and reopen it in the same context, wait for the real session GET, and verify the active journey is present before acting. Click the visible `Advance travel day` control, wait for the real advance POST, and assert success, one additional absolute game-clock day, `daysTravelled === 1`, decreased positive remaining distance, and one recorded travel-diary day numbered `1` relative to this journey. Assert the journey is not completed and `Step into town` is absent. Close and reopen the page again; verify the GET restores those independent facts and the diary's visible `Day 1` entry. If the authoritative result has a pending encounter, verify the encounter message and at least one legal choice are visible and `Advance travel day` is unavailable; otherwise verify the active journey exposes `Advance travel day`. Assert page restoration caused no second advance POST.

- [x] **Step 3: Prove the new browser assertion detects a broken travel-day application**

After the test passes, temporarily omit the `_journey.AdvanceOneDay()` call in `JourneyLoop.PrepareTravelDayAdvance` and run only the new browser test. Confirm it fails because the returned server session still has `daysTravelled === 0`. Restore the call, rebuild the required API/web outputs, and rerun the new and existing travel-resume browser tests. Do not commit the mutation.

- [x] **Step 4: Reconcile feature evidence and run the canonical gate**

Add dated PG-007 evidence for one production-browser travel-day advance and page-reopen restoration, including the observed journal day and legal next action; explicitly leave encounter resolution, arrival, full PG-007 and release qualification open. Run `npm run build` and `npm run test:browser` in `src/WildBunch.Web`, then run `py -3 tools/run.py ci --check` after the source and evidence are complete. Commit the test and feature evidence through the normal check-only hook. Keep all disposable API, database and browser artifacts outside the repository.

### Task 3: Review, publish, merge, and retire N-84

**Files:**
- Update: `docs/decisions/ADR-0047-add-playwright-for-native-browser-contracts.md` with the dated scope clarification justified by this retained player-flow browser proof.
- Review: the committed N-84 branch diff, this plan, the stable baseline spec, applicable ADRs, `docs/features.md`, and the row-18 roadmap.
- Update: `.agents/plans/2026-10-15-row18-n84-travel-day-browser-qualification.md` with completed execution evidence.

**Interfaces:**
- Consumes: a committed browser qualification and passing canonical local gate.
- Produces: a fresh whole-branch review, reviewable PR to `develop`, passing exact-head hosted CI, merge commit and passing develop-push CI, fast-forwarded main checkout, and verified paired cleanup for the N-84 worktree and branch.

- [ ] **Step 1: Complete readiness and independent review**

Use completion-readiness, prepare the whole-branch review package from `origin/develop` to `HEAD`, and request a fresh-context reviewer. Supply this plan, the spec, the review focus, and any execution rulings. Resolve Critical and Important findings with witnessed RED/GREEN and rerun the canonical gate; defer only Minor findings with explicit rationale.

- [ ] **Step 2: Publish and verify the PR**

Push the plan branch and create a Draft PR targeting `develop`. Read the PR back and verify the exact head SHA, source diff, decision-record comparison, feature-matrix comparison, validation claims, and draft state. Mark it ready for review, then verify the hosted canonical gate passes on that exact head before merging.

- [ ] **Step 3: Merge and verify integration**

Squash-merge the reviewed PR into `develop`, verify the merge commit contains the reviewed tree, and wait for the develop-push canonical gate to pass on that merge commit. Do not continue if the merge tree or hosted gate differs or fails.

- [ ] **Step 4: Refresh and clean the exact execution unit**

Fast-forward the main checkout to the verified `develop` merge without disturbing its unrelated investigation edit. Use `finishing-a-development-branch` and its remove-worktree script with `--check` then `--apply`; remove only the N-84 worktree, then remove the matching local and upstream branch refs together. Verify the PR source/merge identities, both hosted gates, main `HEAD`, preserved user edit, and all three cleanup absences. Leave the N-84 plan and roadmap delivery record for N-85's successor-artifact retirement.

N-84 proves one normal travel-day advance and restored post-day state. It also clarifies that Playwright covers selected retained player flows requiring the real browser-to-API recovery boundary, in addition to native browser contracts; manual browser observations remain a separate evidence lane. It does not prove any encounter resolution, journey completion or arrival acknowledgement, the remaining retained browser paths, deployed behavior, overall `0.1.0` qualification, cloud deployment, or user ownership.

### Execution rulings

- **Ruling, 2026-10-15:** The absolute game clock and journey diary use different day coordinates. `TravelDayAdvanced.Day` advances the world clock; `TravelDiaryDayFactory.Create` copies `TravelJourneySnapshot.DaysTravelled` into the journey-relative diary `DayNumber`. The first day therefore advances game clock day 1 to day 2 while appearing as diary `Day 1`; assert these facts independently. Evidence: `src/WildBunch.Domain/Travel/TravelDiaryDayFactory.cs` and `tests/WildBunch.Domain.Tests/Characterization/TravelDiaryCharacterizationTests.cs`; the first real browser run observed clock day 2 and diary day 1.
