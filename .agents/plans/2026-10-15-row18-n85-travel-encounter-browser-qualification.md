# Row 18 N-85 Travel-Encounter Browser Qualification Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `subagent-driven-development` (recommended) or `executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Prove that a player can resolve one real hostile trail encounter through the public browser, persist its resolution, and restore the resulting server-owned journey and journal state after reopening.

**Architecture:** Extend the production-browser route over the real API and isolated PostgreSQL test database. Keep encounter generation, legality, resolution, events, and state server-owned; the browser only chooses an offered action and renders the returned state.

**Tech Stack:** .NET 10, React, TypeScript, Playwright, PostgreSQL, GitHub Actions.

**Spec:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`, especially PG-007 and its event/replay, journal, and legal-action contracts.

**Execution Strategy:** `executing-plans` inline because encounter discovery, resolution, persistence, and reopen assertions share one coupled browser flow, while the checkpoint and publication tasks depend on that same branch state.

## Global Constraints

- Use this fresh worktree from current `origin/develop`; target the PR to `develop`.
- Advance the single authored application version once to `0.1.0-dev.203` in the planning checkpoint.
- Preserve check-only pre-commit and cheapest-first fail-fast CI behavior.
- Use the isolated PostgreSQL browser-test database. Development database playthroughs are disposable prior runs by this agent and may be restarted or dropped if needed; no other agent or user playthrough is present.
- Use only the public player path; do not add dev controls, test-only production endpoints, or direct state mutation to make an encounter occur.
- Assert a real pending hostile encounter and its successful resolution; do not conditionally skip the named behavior or accept a failed/repeated decision as proof of completion.
- Keep the server session and persisted event history authoritative. Verify the resolved encounter appears in the player journal and the returned/reloaded journey exposes the legal next state without duplicating the encounter command.
- Do not claim arrival, all encounter variants, complete PG-007, hosted play, overall release qualification, or cloud playability.
- Update durable decision records only if code inspection proves this change makes an existing decision untrue or introduces a new durable decision.
- Retire the N-84 plan, its matching worktree, and its exact local/upstream branch as one successor-delivery closeout after verified merge and gates.

## Review Focus

- The scenario must fail if the real day-advance path stops creating the deterministic hostile encounter.
- The scenario must fail if the browser choice does not reach the real resolve endpoint, the server does not record the resolution, or the resolved day/journal are not restored after reopen.
- The encounter must be legal in the actual returned session; the test must not forge DTOs or select an unavailable choice.
- Keep the assertion bounded to one deterministic hostile encounter path and its next legal travel state; domain and integration tests remain owners of encounter rules, negative cases, and replay variants.

---

### Task 1: Record N-84 delivery and commit the N-85 checkpoint

**Files:**
- Create: `.agents/plans/2026-10-15-row18-n85-travel-encounter-browser-qualification.md`
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `Directory.Build.props`
- Delete: `.agents/plans/2026-10-15-row18-n84-travel-day-browser-qualification.md`

**Interfaces:**
- Consumes: N-84 PR #387 reviewed source `ac38f3fa611be62ac5fe880e03d93044ccc98f7c`, squash merge `1ab3f04d1766a27e9581913cfa2f8ffe6c299840`, exact-head gate `38795131614`, develop-push gate `38796072858`, whole-branch review, and paired worktree/ref cleanup evidence.
- Produces: dated N-84 delivery evidence, N-85 as row 18 active plan, retired N-84 plan, and version `0.1.0-dev.203`.

- [x] **Step 1: Verify the merged N-84 source and delivery evidence**

Confirm PR #387 is merged to `develop` at `1ab3f04d1766a27e9581913cfa2f8ffe6c299840` with reviewed source `ac38f3fa611be62ac5fe880e03d93044ccc98f7c` and version `0.1.0-dev.202`. Confirm exact-head gate `38795131614` and develop-push gate `38796072858` passed on their exact commits, independent whole-branch review found no actionable findings, and the merge tree matches the reviewed source tree. Preserve the unrelated investigation edit in the main checkout.

- [x] **Step 2: Update the roadmap, version, and successor plan**

Add a dated N-84 delivery record for its one-day public-browser progress and page-reopen restoration, PR/source/merge identities, both passing hosted gates, review, and paired worktree/branch cleanup. Set row 18's active plan to N-85 and state that N-84 proves one normal travel-day advance and restored journey/journal state only. Keep encounter resolution, completed trip and arrival qualification, other retained paths, and full release qualification open. Remove N-84's completed plan and set `Directory.Build.props` to `0.1.0-dev.203`.

- [x] **Step 3: Commit the plan checkpoint**

Run `git diff --check`, stage only this plan, roadmap, version file, and N-84 plan retirement, then commit through the check-only hook. Do not run the full canonical gate before this checkpoint; the hook validates the exact staged candidate.

### Task 2: Qualify one real hostile trail encounter and its restored resolution

**Files:**
- Modify: `src/WildBunch.Web/browser-tests/player-startup.spec.ts`
- Inspect: `src/WildBunch.Api/Games/TravelEndpoints.cs`
- Inspect: `src/WildBunch.Application/Games/Commands/AdvanceTravelDayHandler.cs` and `ResolveJourneyEncounterHandler.cs`
- Inspect: `src/WildBunch.Domain/Game/JourneyLoop.cs` and `src/WildBunch.Domain/Travel/TravelDayPlanGenerator.Context.cs`
- Inspect: `.agents/doctrine/frontend-standards.md`, `.agents/doctrine/validation-policy.md`, and `.agents/playbooks/ui-browser-check.md`

**Interfaces:**
- Consumes: `startMultiDayJourney(page)`, real world-map and travel-preview queries, `POST /api/games/{id}/travel/advance`, `POST /api/games/{id}/travel/encounter/resolve`, and the real server session returned by `GET /api/games/{id}`.
- Produces: a deterministic public-browser scenario that advances a real trip until the selected fixed setup produces one hostile encounter, chooses an offered successful resolution through the visible UI, and verifies the persisted journal/result and legal post-resolution action after page reopen.

- [x] **Step 1: Establish a deterministic public-path hostile encounter fixture**

Use the existing canonical seed helper, Brutal difficulty, and Boring entropy with the existing real longest map-connected multi-day route. The production-browser probe established that this setup reaches a `foe` encounter on journey day 3. Advance through legal travel-day actions until the fixed scenario yields that hostile choice; make absence of a hostile encounter before route completion fail explicitly rather than silently skip it.

- [x] **Step 2: Add the browser resolution and recovery behavior**

Start the deterministic route in the public UI and advance through real visible controls until the server returns a pending hostile encounter. Close and reopen the page before resolution; verify the encounter message and legal offered choice return from the session GET and that day advancement is unavailable. Choose the UI's offered `Fight` action using a deterministic, legal bullet amount that produces a successful resolution. Assert the real resolve POST succeeds, the response clears the pending encounter, the relevant travel-diary day contains an encounter resolution and resolution narrative, and the journey exposes its actual next legal state. Close and reopen again; verify the server response retains that resolution and the visible diary, the UI shows the returned next legal action, and page restoration did not issue a second resolution command.

- [x] **Step 3: Witness that the browser test detects a broken resolution path**

After the scenario passes, temporarily prevent the real resolution handler from applying its result or suppress the relevant browser command path, one mutation at a time. Run only the new browser scenario and confirm it fails on the expected user-visible or persisted resolution assertion. Restore source byte-for-byte before final validation. Do not commit mutations or introduce a test-only production route.

- [ ] **Step 4: Reconcile PG-007 evidence and run the canonical gate**

Add dated PG-007 evidence for this one hostile encounter's browser resolution and page-reopen recovery, naming only the verified action and observed result. State that trip completion/arrival, other encounter outcomes, deployed behavior, and overall PG-007 remain unqualified. Run `npm run build` and `npm run test:browser` in `src/WildBunch.Web`. Commit through the normal check-only hook, which runs `py -3 tools/run.py ci --check` on the staged candidate; use that hook result as the canonical gate instead of duplicating the full gate immediately before commit. Keep disposable API, database, and browser artifacts outside the repository.

### Task 3: Review, publish, merge, and retire N-85

**Files:**
- Review: the committed N-85 branch diff, this plan, the baseline spec, applicable ADRs, `docs/features.md`, and row-18 roadmap.
- Update: `.agents/plans/2026-10-15-row18-n85-travel-encounter-browser-qualification.md` with completed execution evidence.

**Interfaces:**
- Consumes: committed browser qualification and passing canonical local gate.
- Produces: a fresh whole-branch review, reviewable PR to `develop`, passing exact-head hosted CI, merge commit and passing develop-push CI, fast-forwarded primary checkout, and paired cleanup of N-85 worktree and branch.

- [ ] **Step 1: Complete readiness and independent review**

Use completion-readiness; prepare a whole-branch review package from `origin/develop` to `HEAD` containing this plan, the spec, the stated review focus, and any execution rulings. Request fresh-context review under the standing review authorization. Resolve Critical and Important findings with witnessed RED/GREEN and rerun the canonical gate; defer only Minor findings with an explicit reason.

- [ ] **Step 2: Publish and verify the PR**

Push the plan branch and create a Draft PR targeting `develop`. Read the PR back and verify the exact head SHA, source diff, ADR and feature-matrix comparison, validation claims, and draft state. Mark ready for review, then verify the hosted canonical gate passes on that exact head before merging.

- [ ] **Step 3: Merge and verify integration**

Squash-merge the reviewed PR into `develop`, verify the merge commit contains the reviewed tree, and wait for the develop-push canonical gate to pass on that merge commit. Do not continue if the merge tree or hosted gate differs or fails.

- [ ] **Step 4: Refresh and clean the exact execution unit**

Fast-forward the primary checkout to the verified `develop` merge without disturbing its unrelated investigation edit. Use `finishing-a-development-branch` and its remove-worktree script with `--check` then `--apply`; remove only the N-85 worktree, then delete the matching local and upstream branch refs together. Verify PR source/merge identities, both hosted gates, primary `HEAD`, preserved user edit, and absence of the worktree and both refs. Leave this N-85 plan and roadmap evidence for N-86's successor-artifact retirement.

N-85 proves one deterministic hostile encounter resolution through the public browser, including restoration of its server-recorded journal result and next legal travel state. It does not prove completion/arrival, all encounter variants, remaining retained browser paths, deployed behavior, overall `0.1.0` qualification, or cloud deployment.

### Execution Rulings

- **Ruling, 2026-10-15:** The initial Standard/Boring canonical-seed probe completed the selected longest route without a hostile encounter. Use the player-selectable Brutal difficulty with Boring entropy for the deterministic hostile-encounter fixture; this changes only scenario setup, not production logic or dev controls.
- **Ruling, 2026-10-15:** Advancing into a pending encounter returns `success: false` with an interrupted server snapshot and the legal choice; that is the designed pause, not a failed day mutation. The browser test treats this response as expected only when the returned journey contains the pending encounter.
- **Ruling, 2026-10-15:** Replacing the server handler's submitted encounter choice with an invalid choice made the new browser scenario fail at the successful resolution assertion with `That is not a lawful way to answer this encounter.` Restoring the handler byte-for-byte made the focused scenario pass again; the mutation was not committed.
