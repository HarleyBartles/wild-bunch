# N-68: Separate Player and Playtest Web Composition

> **For agentic workers:** REQUIRED SUB-SKILL: Use `subagent-driven-development` (recommended) or `executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make ordinary production builds compose player UI without developer controls while preserving the contextual developer overlay in local development and an explicitly selected playtest build.

**Architecture:** The settled boundary in ADR-0041 and the baseline requires public player-only UI and developer tools only in a developer-enabled environment. Choose the built-in Vite mode as the explicit frontend composition selector: normal production builds use the player shell; local Vite development and an explicit playtest build use the playtest shell. Keep backend DevRoleGuard as the authority for developer API access; frontend composition is not authentication or an access-control boundary.

**Tech Stack:** React, TanStack Router, Vite, Vitest, ASP.NET Core integration tests, Git, the repository command bus.

**Spec:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md); [row 17 roadmap](../roadmaps/2026-10-07-stable-0.1.0-cleanup.md); [web layer investigation](../investigations/stable-0.1.0/2026-10-07-web-layer-investigation.md); [web test follow-up](../investigations/stable-0.1.0/2026-10-07-web-test-followup.md); [ADR-0030](../../docs/decisions/ADR-0030-dev-overlay-and-dev-endpoint-namespace.md); [ADR-0041](../../docs/decisions/ADR-0041-public-and-preprod-developer-capability-boundary.md).

**Execution Strategy:** `executing-plans` (Native inline). The successor promotion, route-root selection, shell composition and the existing backend-denial proof are coupled by the public/playtest boundary; one inline implementation context keeps the test and application contracts aligned, followed by a fresh independent whole-branch review.

## Global Constraints

- Start from refreshed `origin/develop` in this fresh canonical worktree and target the PR to `develop`.
- Promote PR #370, record its reviewed source `ca8ed08b4f9db8cbcee006d54ac98516f0850273`, merge `d2149dba2d10987cb11acee49c06863df9d5bf7b`, exact-head gate `38664283255`, develop-push gate `38664767254`, clean review and paired cleanup; retire N-67 and advance only `Directory.Build.props` from `0.1.0-dev.185` to `0.1.0-dev.186`.
- The normal production build is player-only; local Vite development and the explicit playtest build retain contextual developer controls. No runtime query, user-supplied URL or local-storage flag enables developer composition.
- Developer API authorization remains server-owned by DevRoleGuard. Prove its production denial using a real created session; do not treat frontend hiding or Vite mode as security.
- Do not implement Google authentication, owner admission, deployment, a new API authorization scheme, developer feature work, or public API removal. ADR-0041's environment admission and separate credentials remain cloud/deployment work.
- Keep direct player-route rendering, arrival handling, saved-session recovery, and playtest overlay behavior meaningfully covered. Do not test component source spelling, imports, mode strings alone, or arbitrary property absence.
- Do not change any player capability or dependency; no feature-matrix change is expected. No ADR change is expected because this implements the existing ADR-0030/0041 boundary.
- Keep the current plan through its completing PR; retire eligible predecessors in this successor's first substantive commit. Use the normal staged-candidate hook and the repository's fail-fast canonical CI.

## Review Focus

- A production build with a hydrated saved game must not expose a Dev control or developer overlay, while direct player routes and arrival behavior continue to work.
- Local development and an explicit playtest build must retain the developer overlay and contextual surface synchronization.
- A direct dev API request for an existing game in Production must still return 403, proving the guard rather than an unknown-session path.
- The updated investigation row must accurately distinguish player-shell and playtest-shell coverage and preserve the user's direct sheriff/saloon route disposition.

---

### Task 1: Promote N-67 and prepare N-68 delivery

**Files:**
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `Directory.Build.props`
- Delete: `.agents/plans/2026-10-15-row17-n67-map-accessibility-inventory.md`

**Interfaces:**
- Consumes: PR #370 merged to `develop` at `d2149dba2d10987cb11acee49c06863df9d5bf7b`, reviewed source `ca8ed08b4f9db8cbcee006d54ac98516f0850273`, passing hosted gates `38664283255` and `38664767254`, clean review, and verified N-67 worktree/ref removal.
- Produces: N-67 delivery evidence and retirement, active N-68 roadmap link, and the next version identity `0.1.0-dev.186`.

- [x] Verify PR #370's merged state, head and merge identities, both hosted gate conclusions, current `origin/develop`, and absent N-67 worktree/local/upstream refs.
- [x] Add PR #370 and both gate links to row 17, record N-67's test-inventory correction and paired cleanup, set N-68 as the active plan, and delete the completed N-67 plan.
- [x] Advance only `Directory.Build.props` to `0.1.0-dev.186`; inspect and commit this successor retirement, roadmap promotion and version change together before implementation changes.

### Task 2: Separate the player and playtest route roots

**Files:**
- Modify: `src/WildBunch.Web/src/shell/router.tsx`
- Modify: `src/WildBunch.Web/src/shell/AppShell.tsx`
- Create: `src/WildBunch.Web/src/shell/PlaytestAppShell.tsx`
- Test: `src/WildBunch.Web/src/tests/AppShell.test.tsx`

**Interfaces:**
- Consumes: existing route tree, AppShell, DevSurfaceProvider, useDevSurfaceSync, and current AppShell behavior tests.
- Produces: `createAppRouter(profile)` with an explicit `public | playtest` profile and the matching root shell; default profile is public. `AppShell` owns player chrome; `PlaytestAppShell` composes the developer provider, synchronization, toggle and overlay around the shared player shell.

Use the profile at the TanStack root-route boundary, not as a shell query parameter:

    createAppRouter(profile: "public" | "playtest" = "public")

The AppShell test helper accepts the same profile; ordinary hydration and direct-route cases stay public, while tests that open or inspect the dev overlay explicitly use playtest.

- [x] Add a public-composition behavior test with a saved game that expects the HUD and player route while no Dev toggle or developer overlay is available. Run it first and confirm it fails against the current unconditional AppShell.
- [x] Make route-root composition explicit: public uses player chrome only; playtest composes DevSurfaceProvider, useDevSurfaceSync, contextual toggle and DevOverlay. Keep shared HUD, global overlays, recovery, phase routing and player routes under one reusable player-shell owner.
- [x] Route existing dev-open, dismissal, HUD visibility and contextual-surface tests through the explicit playtest profile. Keep direct store, sheriff, saloon and trailhead URL tests and arrival behavior under the public profile.
- [x] Run the focused AppShell suite. The public-isolation test failed against the original unconditional AppShell, then the final split passed the suite.

### Task 3: Select the playtest build explicitly and strengthen server denial proof

**Files:**
- Modify: `src/WildBunch.Web/src/App.tsx`
- Create: `src/WildBunch.Web/src/shell/appProfile.ts`
- Modify: `src/WildBunch.Web/package.json`
- Test: `src/WildBunch.Web/src/tests/appProfile.test.ts`
- Modify: `tests/WildBunch.Integration.Tests/Dev/DevEndpointTests.cs`
- Review: `src/WildBunch.Api/Dev/DevRoleGuard.cs` and `tests/WildBunch.Integration.Tests/Dev/NonDevApiFactory.cs`

**Interfaces:**
- Consumes: the profile-aware router from Task 2 and the existing Production-host integration fixture.
- Produces: `resolveAppProfile` selecting playtest only for Vite development or mode `playtest`; local Vite development and `build --mode playtest` use playtest composition; ordinary `vite build` uses public composition; the Production denial test uses a valid started game.

The profile resolver contract is intentionally small and accepts only Vite's mode/development inputs:

    resolveAppProfile({ dev: true, mode: "development" }) -> "playtest"
    resolveAppProfile({ dev: false, mode: "playtest" }) -> "playtest"
    resolveAppProfile({ dev: false, mode: "production" }) -> "public"

- [x] Add `resolveAppProfile` and table-driven tests proving development selects playtest, explicit `playtest` mode selects playtest, and production/default mode selects public. The resolver accepts only Vite mode/development values; a production URL or browser storage value cannot opt a user into playtest.
- [x] Keep `vite build` as the player-only production artifact and add a named `build:playtest` command using Vite mode `playtest`; local Vite development remains playtest for existing developer workflow.
- [x] Update `GetSessionAudit_Returns403_InNonDevEnvironment` to create a real started game through the public API in `NonDevApiFactory`, request that game's dev audit route, and assert 403. Bypassing the guard made the test fail with 200; the guard was restored and the test passed.

The denial test must use the valid created ID so the guard, rather than a missing session, determines the response:

    var created = await client.CreateStartedGameAsync(BoringScenarioBuilder.StartingTownReady(), "Ranger Vale");
    var response = await client.GetAsync($"/api/dev/sessions/{created.Id}/audit");
    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
- [x] Ensure PostgreSQL with `tools/postgres-dev.ps1 ensure`, then run `npm --prefix src/WildBunch.Web test -- src/tests/AppShell.test.tsx src/tests/appProfile.test.ts`, the targeted Production API integration test with `py -3 tools/run.py dotnet-test --check -- --filter FullyQualifiedName~GetSessionAudit_Returns403_InNonDevEnvironment`, `npm --prefix src/WildBunch.Web run build`, and `npm --prefix src/WildBunch.Web run build:playtest`. Confirm both profiles compile and their profile-specific UI behavior tests pass.

### Task 4: Reconcile evidence, validate and deliver N-68

**Files:**
- Modify: `.agents/investigations/stable-0.1.0/2026-10-07-web-test-followup.md`
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Review: ADR-0030, ADR-0041 and the feature-matrix entries relevant to this scope.

**Interfaces:**
- Consumes: tested public/playtest composition, valid-session Production denial, and PR #370 successor evidence.
- Produces: truthful AppShell test inventory, N-68 delivery evidence and a reviewable PR.

- [ ] Update the AppShell inventory to retain real hydration/recovery, direct sheriff/saloon routes, navigation and arrival; distinguish player-composition tests from playtest-only developer wiring/dismissal; remove any claim that the current suite still contains the already-absent safety-copy oracle.
- [ ] Add a dated N-68 disposition explaining the public/playtest behavioral coverage and valid-session backend denial. Preserve the user's direct-route coverage intent. State why ADR-0030/0041 and the feature matrix remain unchanged.
- [ ] Before publication, compare the full diff against ADR-0030/0041 and `docs/features.md`; require no new decision, admission/auth promise, deployment claim or player capability.
- [ ] Run `git diff --check`, explicitly stage the intended candidate, inspect all staged changes and commit through the staged-candidate hook. Treat the passing hook as the canonical local gate; do not immediately repeat full CI.
- [ ] Prepare a whole-branch review package and dispatch a fresh independent reviewer. Resolve actionable findings with focused proof and a fresh review of the corrected head.
- [ ] Push and create a Draft PR to `develop`; verify body, base, head SHA and Draft state; mark ready after review and require the exact-head hosted canonical gate to pass.
- [ ] Squash-merge under the active goal authorization and require the develop-push gate to pass on the merge SHA.
- [ ] Fast-forward the primary checkout and preserve or integrate its pre-existing AppShell inventory note without losing its intent; verify the merged tree, then remove this worktree with the worktree-removal helper, delete only its paired local/upstream refs and verify all three absences.

## Acceptance

The ordinary production build composes player UI without developer controls. Local development and an explicit playtest build retain the contextual developer surface. A valid game ID cannot make the Production dev API return anything but 403. Player routes and arrival behavior remain covered. The N-67 artifact is retired only after its merged source, hosted gates and paired cleanup are verified; N-68 merges as `0.1.0-dev.186` with exact-head and develop-push hosted gates green.
