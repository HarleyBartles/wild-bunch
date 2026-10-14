# Row 17 N-52: Prove Browser Route Reachability and Search Behavior

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task. Steps use checkbox syntax for tracking.

**Goal:** Replace route-source spelling checks with tests that prove supported browser URLs render their intended player surfaces and optional arrival search does not create a false arrival state.

**Architecture:** Exercise the real `createAppRouter()` and `AppShell` through `RouterProvider`. Keep the current lazy route implementation; this plan does not add a performance test or test React import syntax. Prove direct reachability of all four town place routes, retain existing store and trail behavior coverage, and prove an unrelated raw search value does not render the arrival notice. Type checking, including current navigation calls without a search object, owns the optional search type contract. Remove the identity-only router factory assertion because a distinct object does not prove navigation-state isolation.

**Tech Stack:** React 18, TanStack Router, Testing Library, Vitest, repository command bus.

**Spec:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md), especially test design and validation; [test-quality investigation](../investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md) W-02; [web test follow-up](../investigations/stable-0.1.0/2026-10-07-web-test-followup.md) router contracts.

**Execution Strategy:** `executing-plans` (native inline). The source matcher and its replacement behavior share the existing `AppShell.test.tsx` harness; one fresh application context can cover the route and search boundary without changing production routing. Use targeted route and arrival-condition mutations to prove both new visible behaviors, then restore production byte-for-byte.

## Global Constraints

- Preserve the existing URL paths and current supported route behavior; do not reorganize the route tree or add routes.
- Preserve `/town?arrived=1` arrival behavior and ensure unrelated raw URL values do not render a false arrival notice. TanStack Router preserves unrecognized raw search parameters; the town surface recognizes only the exact supported arrival value.
- Do not change lazy route implementation, navigation behavior, game-phase synchronization, or product promises.
- Keep tests at the observable application boundary and avoid source reads, route-tree regexes, implementation identifier assertions, or isolated helper-only checks.
- Assign exactly one unique next development version, `0.1.0-dev.170`, for this merged PR, with `develop` as its target.
- Require independent whole-branch review, passing exact-head hosted CI and develop-push CI, then fast-forward the primary checkout and remove the clean worktree together with its exact local and upstream branch refs.

## Review Focus

- Direct `/town/store`, `/town/sheriff`, `/town/saloon`, and `/town/trailhead` URLs render their corresponding place surfaces instead of the hub or setup screen.
- `/town?arrived=unexpected` does not show the arrival notice; the existing valid-arrival test remains green. The test does not claim the router discards unknown raw parameters.
- The route-reachability assertion fails when the route is temporarily wired to the wrong surface, and the invalid-arrival assertion fails when any truthy value is treated as arrival.
- No assertion claims that distinct router object identity alone guarantees independent browser navigation state.

---

### Task 1: Promote N-51 and establish N-52

**Files:**
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `Directory.Build.props`
- Create: `.agents/plans/2026-10-14-row17-n52-route-reachability.md`
- Delete: `.agents/plans/2026-10-14-row17-n51-http-error-response-contract.md`

**Interfaces:**
- Consumes: N-51 PR #354, reviewed head `9a5666b5a4f05c665ccf9d4261dccb039c8a5ba4`, merge SHA `6a95d0170b2fe40da341246f3324fea92c98cab6`, identical tree `47ff5454a22d89a10d2af613b34894f5a9d19b85`, exact-head hosted run `38580786696`, and verified paired worktree/ref cleanup.
- Produces: promoted N-51 evidence, active N-52 at `0.1.0-dev.170`, and retirement of the completed N-51 plan.

- [x] **Step 1: Revalidate N-51 delivery and cleanup.** Confirm PR #354 is merged into `develop`, recorded head and merge SHA match the values above, exact-head hosted run `38580786696` passed, the merge tree equals the reviewed tree, the primary checkout is current, and the N-51 worktree plus exact local/upstream refs are absent. Record that no post-merge push run exists because the push event did not launch one and the workflow does not expose `workflow_dispatch`; do not report a develop-push gate as passed.
- [x] **Step 2: Promote N-51, activate N-52, and advance the version.** Add PR #354 to row 17; record review, merge/tree identity, exact-head gate, paired cleanup, and the missing post-merge push gate truthfully. Point the active row at this N-52 plan and `0.1.0-dev.170`. Change only the authored version in `Directory.Build.props` to `0.1.0-dev.170`.
- [x] **Step 3: Retire the completed predecessor.** Delete the N-51 plan only after confirming its full scope and delivery evidence are promoted. Preserve the baseline specification, roadmap, and investigation documents.
- [x] **Step 4: Commit successor planning and predecessor retirement first.** Run `git diff --check`, inspect the complete staged diff, and commit the plan, roadmap, version, and N-51 retirement first with message `docs: plan N-52 route reachability`.

### Task 2: Prove route reachability and search behavior

**Files:**
- Modify: `src/WildBunch.Web/src/tests/AppShell.test.tsx`
- Delete: `src/WildBunch.Web/src/tests/routingConventions.test.ts`
- Modify: `.agents/doctrine/frontend-standards.md`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-06-test-quality-investigation.md`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-07-web-test-followup.md`
- Inspect only: `src/WildBunch.Web/src/shell/router.tsx`, `AppShell`, all four town-place surfaces, existing AppShell route tests, ADR-0027/0039/0046, and PG-001/PG-003.

**Interfaces:**
- Consumes: `renderShell()` application harness, `createAppRouter()`, the existing `/town?arrived=1` interaction test, and real session/query mocks.
- Produces: observable direct-route rendering for all four town places, negative arrival rendering for an unrelated query value, and corrected W-02/documentation dispositions.

- [x] **Step 1: Write behavior tests at the application boundary.** Extend `renderShell` with an optional initial path that sets browser history before constructing the router. Add a table-driven test for `/town/store` rendering `Store`, `/town/sheriff` rendering `Sheriff Office`, `/town/saloon` rendering `Saloon`, and `/town/trailhead` rendering `Hit the trail`, using the real router and normal in-town session mocks. Add a `/town?arrived=unexpected` case that waits for the town heading and asserts the arrival status is absent. Keep the existing successful `/town?arrived=1` interaction test.
- [x] **Step 2: Prove both new behaviors are sensitive.** Temporarily point the sheriff route component at `SaloonPlace`, run only the sheriff reachability test, and confirm it fails because `Sheriff Office` is absent. Then temporarily change the arrival condition to accept any truthy value, run only the invalid-search test, and confirm the false arrival notice makes it fail. Restore both production files byte-for-byte before the remaining work.
- [x] **Step 3: Remove source-shape assertions and the identity-only check.** Delete the `routingConventions.test.ts` file. Its lazy-import regex, parent-route identifier check, arbitrary `return {};` search, and `router !== otherRouter` assertion are not user-observable proofs. Do not add a replacement assertion that merely repeats the current source shape. Keep the current lazy route code intact.
- [x] **Step 4: Reconcile the audit and guidance.** Preserve W-02 as a historical finding and append its dated disposition to the test-quality investigation. Update the web follow-up W-02 row to record direct store/sheriff/saloon/trailhead reachability and the visible invalid-arrival outcome; state that unknown raw parameters are preserved and no normalization claim is made. Retain lazy loading as current implementation without claiming a test enforces its syntax. In frontend standards, replace the statement that `routingConventions.test.ts` enforces route topology with a truthful pointer to AppShell behavior coverage and explain that app tests construct fresh routers through `createAppRouter()`.
- [x] **Step 5: Read durable truth and run focused tests.** Use the decision-record and feature-matrix playbooks to compare the diff against routing-related ADRs and feature claims. Record why no durable product or architecture decision changes. Run `npm test -- src/tests/AppShell.test.tsx` from `src/WildBunch.Web`; compare each new URL and visible result against the assertions.
- [x] **Step 6: Format, lint, and commit.** Run `py -3 tools/run.py format --check src/WildBunch.Web/src/tests/AppShell.test.tsx` and `py -3 tools/run.py lint --check src/WildBunch.Web/src/tests/AppShell.test.tsx`, review Markdown, run `git diff --check`, stage only the named test and guidance files, inspect the full staged diff, and commit as `test: prove browser route reachability`.

### Task 3: Review, publish, integrate, and retire N-52

**Files:**
- Review: full branch diff, this plan, roadmap, W-02 investigation updates, frontend standards, relevant ADRs, feature entries, and validation evidence.
- Modify: this plan and the roadmap with final delivery evidence.

**Interfaces:**
- Consumes: direct place-route rendering, invalid-arrival negative behavior, and restored production route source.
- Produces: N-52 merged into `develop` at `0.1.0-dev.170`, with independent exact-head review and passing hosted gates.

- [x] **Step 1: Verify implementation and canonical validation.** Confirm `Directory.Build.props` owns `0.1.0-dev.170`, the temporary route mutation is absent, the focused AppShell tests pass, `py -3 tools/run.py web --check` passes, and the canonical staged-candidate hook passes.
- [x] **Step 2: Complete author truth checks.** Compare the actual diff against routing ADRs and feature-matrix claims through their playbooks. Keep the plan limited to tests and guidance if the route contracts remain unchanged.
- [ ] **Step 3: Obtain independent whole-branch review and publish.** Build the exact merge-base-to-HEAD review package, dispatch a fresh-context reviewer without requesting per-slice approval, resolve any actionable findings with a new RED/GREEN proof and fresh review, publish a Draft PR targeting `develop`, verify its exact head and body, mark it ready, and require exact-head hosted CI to pass.
- [ ] **Step 4: Verify integration and clean up.** Squash-merge to `develop`, require the develop push gate to pass on the exact merge SHA, fast-forward the primary checkout, remove this clean worktree with the `finishing-a-development-branch` helper, delete the matching local and upstream refs, and verify all three are absent before creating another plan worktree.

## Acceptance Criteria

- Direct store, sheriff, saloon and trailhead URLs render their matching surfaces in the real AppShell/router composition.
- An unrelated query value does not produce an arrival notice; valid arrival interaction behavior remains covered.
- The route test fails when its matching route is temporarily wired to the wrong surface, and the arrival test fails when any truthy search value is treated as arrival; production code is restored before final checks.
- Source spelling, parent identifier, arbitrary empty-object, and object-identity checks are removed without changing production routing.
- W-02, frontend routing guidance, and the row 17 delivery ledger state exactly what behavior and validations are now covered.
- N-51 evidence is promoted before the N-51 plan is retired in the first substantive successor commit; N-52 remains through its completing PR.
- Focused tests, web lane, canonical staged-candidate gate, independent review, exact-head hosted CI, exact-merge develop gate, primary fast-forward, and paired worktree/ref cleanup are verified.

**Author truth check:** ADR-0027's routed shell and server-owned state, ADR-0039's starting-town route flow, and ADR-0046's pointer-operated map scope remain unchanged. PG-001 and PG-003 promises and dependencies do not change; these tests prove the existing town-place URLs and arrival notice behavior without adding a player capability.
