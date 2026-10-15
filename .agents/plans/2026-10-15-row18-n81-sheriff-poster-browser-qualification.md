# Row 18 N-81 Sheriff Office Poster Browser Qualification Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task by task.

**Goal:** Qualify the retained Sheriff Office wanted-poster path through the production-mode browser, real API and PostgreSQL event store, including restoration of the learned poster after closing and reopening the page.

**Architecture:** Extend the existing `player-startup.spec.ts` journey after its real name-only start, prologue, map choice, arrival and resume. Use the existing town-building control to enter Sheriff Office, read one poster through the player command, assert the returned public poster, then close and reopen the browser and prove the same poster is served from the restored game and rendered by the existing Sheriff Office surface. Do not add application behavior or a test-only route; fix only a reproduced defect against the settled PG-005 contract.

**Tech Stack:** Playwright Chromium, production-mode Vite build, ASP.NET API, PostgreSQL 16, Python command bus and pytest.

**Implementation Guide:** [Implementing runbook](../runbooks/implementing.md), [testing playbook](../playbooks/testing.md), [frontend standards](../doctrine/frontend-standards.md), [code-review runbook](../runbooks/code-review.md), and [pull-request runbook](../runbooks/pr.md).

**Spec:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md), especially Knowledge PG-005/PG-008 and workstream G; [feature matrix](../../docs/features.md), especially PG-005 and PG-003; [ADR-0028](../../docs/decisions/ADR-0028-onion-ddd-cqrs-event-sourcing-and-projections-posture.md), [ADR-0046](../../docs/decisions/ADR-0046-pointer-operated-town-selection-for-0.1.0.md), and [ADR-0048](../../docs/decisions/ADR-0048-gitflow-and-semver-release-lifecycle.md).

**Execution Strategy:** `executing-plans` - the route is one sequential browser scenario across restored session state, an existing town control, the poster command and the server-backed read model; inline execution keeps the stateful real-API journey and its mutation proof in one context. The nearest alternative, subagent-driven-development, would split a single test and its evidence across handoffs without reducing implementation or review risk.

## Global Constraints

- Advance the sole authored product version in `Directory.Build.props` exactly once, from `0.1.0-dev.198` to `0.1.0-dev.199`.
- Keep this change scoped to the existing public Sheriff Office poster journey and evidence for PG-005; do not qualify saloon take-in, purchasing, travel, owned-resource arrival, confirmed start over, full candidate scope, or deployment here.
- Use the existing public web production build, actual HTTP API and uniquely named disposable PostgreSQL database established by the browser support. Never mock the route under test, use the shared `wildbunch_dev` database, or add a test-only application path.
- Reach Sheriff Office through the existing visible `Sheriff Office` town-building control. Do not add town navigation, change world-map input, or add product controls for this test.
- Let the API return the public poster and its target name; assert one poster is revealed on the first read and that the same public poster is restored and rendered after a new page loads. Do not hard-code seed-dependent suspect identity or expose hidden culprit state.
- Preserve event history as source of reconstruction, valid caches as ordinary read state, strict CQRS, and the PG-005 rule that poster reading reveals one clue per read. Existing integration coverage owns hidden-truth safety and event persistence; this browser proof owns the complete player route and page-reopen rendering.
- Prove the new browser assertion can fail by temporarily removing the existing `refreshGameResources(queryClient, result.currentJournal.id)` call from `SheriffPlace`'s successful poster-read path, confirm the browser journey fails because the learned poster never appears, restore the call, and rerun the browser proof. Do not commit the mutation.
- Do not create a successor ADR: the route preserves ADR-0028 and ADR-0046 and makes no new durable choice. Compare the completed diff against those records and ADR-0048 before review; update the matrix because runtime/browser evidence changes, not because the product promise changes.
- Keep the user's pre-existing `.agents/investigations/stable-0.1.0/2026-10-07-web-test-followup.md` edit in the main checkout untouched; all N-81 work stays in this fresh canonical worktree.

## Review Focus

- The poster read must travel through the real Sheriff Office UI action and API endpoint, not a direct helper call; the existing `readWantedPosters` handler must return one newly learned public poster.
- After the browser page closes, the `GET /api/games/{id}` response must contain that same poster and reopening Sheriff Office must render it without re-reading or revealing a second clue.
- The browser proof must not imply that every PG-005 source, town visit, all retained gameplay, cache invalidation recovery, hosted/deployed resume or candidate qualification is complete.
- A failed or rejected read must not be mistaken for poster acquisition; the existing API acceptance test remains responsible for hidden-truth exclusion and persistence semantics.

---

### Task 1: Commit the successor plan checkpoint and retire N-80

**Files:**
- Create: `.agents/plans/2026-10-15-row18-n81-sheriff-poster-browser-qualification.md`
- Delete: `.agents/plans/2026-10-15-row18-n80-retained-startup-browser-qualification.md`
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `Directory.Build.props`

**Interfaces:**
- Consumes: PR #383 reviewed source `4188b2325c9492c65e2da3a7b88ceca0ea4414c0`, squash merge `acdff36d4732062737f8abaa2fcab8ac5985337a`, passing exact-head gate [38749318140](https://github.com/HarleyBartles/wild-bunch/actions/runs/38749318140), passing develop-push gate [38750428568](https://github.com/HarleyBartles/wild-bunch/actions/runs/38750428568), fresh exact-head review with no findings, and verified N-80 worktree/local/upstream ref retirement.
- Produces: N-80's dated roadmap delivery record; N-81 as the active row-18 plan; this committed plan and version `0.1.0-dev.199`.

- [x] Verify the fresh worktree starts at current `origin/develop` `acdff36d4732062737f8abaa2fcab8ac5985337a`; confirm N-80's merged PR, exact-head and develop-push gates, review and paired cleanup from the recorded source evidence.
- [x] Record N-80's exact review/source/merge/gate/cleanup evidence in the roadmap; retire the N-80 plan; set row 18's active plan and remaining scope to N-81 plus the later qualification work; advance the version once to `0.1.0-dev.199`; create this plan.
- [x] Run `git diff --check`, stage only these four paths, and commit the successor checkpoint through the normal check-only commit hook.

### Task 2: Qualify one wanted-poster read and restoration in Chromium

**Files:**
- Modify: `src/WildBunch.Web/browser-tests/player-startup.spec.ts`

**Interfaces:**
- Consumes: the production public web build, the existing `startIsolatedApi()` disposable PostgreSQL path, the server-created game ID, the existing town-building control, the existing `POST /api/games/{id}/wanted-posters/read` action, and `GameSessionDto.wantedPosters` from `GET /api/games/{id}`.
- Produces: browser evidence that entering Sheriff Office and reading posters reveals exactly one server-returned public poster, and a separate page close/reopen restores and renders that same poster without another read.

- [x] Extend the existing real-browser journey after town restoration: click the visible `Sheriff Office` button, wait for the Sheriff Office heading, capture the real wanted-poster POST response, assert HTTP success and `success === true`, assert exactly one returned poster, and capture its `targetDisplayName` from that response.
- [x] Assert that the existing Sheriff Office surface renders the returned target after the read. Close the page, create a new page in the same isolated browser context, reopen the production app, wait for the same game's GET response, and assert its `wantedPosters` contains exactly the same target.
- [x] Enter Sheriff Office again through the existing town control and assert the same target renders without clicking `Read wanted posters` again; this distinguishes restoration from a second acquisition.
- [x] Run the focused browser route on current behavior. Temporarily remove the success-path `refreshGameResources` call in `src/WildBunch.Web/src/flow/places/SheriffPlace.tsx` and rerun the focused route; confirm it fails at the user-visible poster assertion, restore the source, and confirm the route passes.
- [x] Run the existing `WantedPosterAcceptanceTests` PostgreSQL acceptance test to retain its independent event-persistence and hidden-truth proof; do not duplicate those assertions in the browser test.

### Task 3: Record the bounded PG-005 evidence

**Files:**
- Modify: `docs/features.md`

**Interfaces:**
- Consumes: the passing production-browser test and existing wanted-poster acceptance test.
- Produces: a dated PG-005 assessment stating the exact Sheriff Office route, one-poster result, browser page-reopen restoration, and limits on other knowledge sources and deployed behavior.

- [ ] Update PG-005's assessment from “browser evidence ... is not recorded” to the observed N-81 browser path; link the actual Playwright journey and existing API acceptance test, preserving “partial” and the remaining saloon gossip/observation and deployment limits.
- [ ] Run the feature-matrix maintenance review and compare the final browser and evidence diff against PG-005, the baseline spec, ADR-0028, ADR-0046, and ADR-0048; record no current promise or durable decision change if the live behavior matches this plan.

### Task 4: Verify, review, publish and close N-81

**Files:**
- Review the full N-81 branch diff, committed plan, baseline specification, roadmap, PG-005 matrix entry, ADR-0028, ADR-0046, ADR-0048 and applicable testing/release guidance.

**Interfaces:**
- Consumes: a committed N-81 browser proof, truthful matrix evidence, passing local and hosted gates, and an independent review of the exact candidate.
- Produces: a reviewed PR to `develop` at `0.1.0-dev.199`, passing exact-head hosted CI, a verified merge result and paired worktree/branch cleanup.

- [ ] Run the focused web browser lane and PostgreSQL wanted-poster acceptance test; run the complete fail-fast `py -3 tools/run.py ci --check` on the final candidate and use the normal check-only commit hook for each commit.
- [ ] Review the final diff against the relevant ADRs, feature matrix, code-review profile and review runbook; dispatch a fresh-context reviewer on the committed exact head, resolve Critical and Important findings with witnessed behavior proof, and rerun required validation after any correction.
- [ ] Push the dedicated branch and open a Draft PR targeting `develop`; verify its exact head, base, version, draft state and required hosted gate; merge only after the fresh review and exact-head gate pass, then verify the develop-push gate on the exact merge commit.
- [ ] Fast-forward the main checkout to the verified merge without disturbing the pre-existing user edit; use `finishing-a-development-branch`'s `remove_worktree.py --check` then `--apply`; delete only N-81's exact local and upstream branch refs, and verify the worktree and both refs are absent.

## Scope Boundaries

This slice qualifies one source of public knowledge only: wanted posters in the existing Sheriff Office context, using one real read and page-reopen restoration. It does not qualify saloon gossip or observation, named take-in, clue-to-target inference, multiple towns/poster circulation, cache invalidation recovery, invalid-history rejection, travel, shopping, start over, terminal death, all of PG-005, hosted release, deployed resume or user ownership.
