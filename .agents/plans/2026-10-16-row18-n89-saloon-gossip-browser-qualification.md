# N-89: Qualify saloon gossip in the public browser

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task. Steps use checkbox syntax for tracking.

**Goal:** Prove that a player can gather saloon gossip through the public browser, that the server records and projects the resulting clue into the player journal, and that the same fact is restored after reopening.

**Architecture:** Extend the existing Playwright production-browser suite through public setup, prologue, shared map and saloon context, then invoke the real gossip API. Assert the command's server-returned journal contains the clue and the player-facing journal renders it after a fresh server read following page reopen. Use the test harness's unique disposable PostgreSQL database and preserve the existing event-sourced server authority.

**Tech Stack:** React, Playwright Chromium, .NET API, PostgreSQL, repository command bus.

**Spec:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md), PG-005 knowledge sources and row 18 qualification.

**Execution Strategy:** `executing-plans` - one retained knowledge-source route and its restored journal behavior form one tightly coupled browser scenario and evidence update.

## Global Constraints

- `develop` is the integration branch; deliver this plan through one reviewed PR to `develop`.
- The development version for this PR is `0.1.0-dev.207`, authored only in `Directory.Build.props`.
- Use the production web build and real API against the uniquely named disposable PostgreSQL database created by `startIsolatedApi`; do not mutate or drop the shared development database.
- The event stream owns what happened; journal and browser state are projections, not independent truth.
- Keep the N-89 plan and its evidence until its PR completes; retire the completed N-88 plan in N-89's first substantive implementation commit.
- This is evidence for the saloon gossip route only. Do not expand gossip rules, clue generation, deduction, saloon features, telegraph or browser scope beyond the contract already specified.
- Compare the final diff through the decision-record and feature-matrix playbooks. No ADR change is expected because this qualifies the settled PG-005 contract.

## Review Focus

- The UI action must invoke the real `POST /api/games/{id}/investigations/local-gossip/gather` for the active game.
- The successful API result must include a journal clue that also appears in a fresh session/journal read after reopening; an optimistic command response alone is insufficient.
- The test must prove its persistence assertion detects a success-shaped but non-committing command response.

---

## Settled scope

PG-005 retains saloon gossip as a player knowledge source. Domain, Application and persistence coverage already owns clue selection, one-source-per-visit legality and event replay. Browser evidence currently covers Sheriff Office wanted posters and a saloon observation/take-in route, but not the gossip action or the resulting player journal. N-89 connects the retained gossip interaction to the server-authoritative journal and proves restoration after reopening. It does not qualify other knowledge sources, clue generation rules, every source failure path, hosted behavior or deployment.

## Task 1: Add the production-browser gossip and restoration proof

**Files:** Modify `src/WildBunch.Web/browser-tests/player-startup.spec.ts`; update PG-005 dated evidence in `docs/features.md`; update `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`; delete `.agents/plans/2026-10-15-row18-n88-store-purchase-browser-qualification.md` in the first substantive implementation commit; update `Directory.Build.props` to `0.1.0-dev.207`.

**Consumes:** The public setup/prologue/map route, town Saloon building interaction, `startIsolatedApi`, production-web Playwright server, `POST /api/games/{gameSessionId}/investigations/local-gossip/gather`, `GET /api/games/{gameSessionId}`, the event-derived journal endpoint/resource and existing PG-005 Domain/Application/persistence tests.

**Produces:** One deterministic public-browser scenario that enters Saloon, gathers gossip exactly once, proves the returned journal contains the disclosed clue, closes and reopens the browser, then proves a fresh server read restores the clue in the player-facing journal without another gossip command.

- [x] **Step 1: Add the browser scenario**

Add a named-game scenario to `player-startup.spec.ts`. Start using a unique player name and the existing canonical seed helper, acknowledge the prologue, select an API-returned town on the shared world map, and enter Saloon via the town building interaction. Observe the gossip POST and resulting session/journal GET. Assert the POST succeeds for the active game, the response journal includes a non-empty fact/clue beyond the opening lead, and the visible player journal displays the same text. Close and reopen the page in the same browser context; assert a fresh session read includes the fact, open Journal and assert the same text is visible, and confirm no second gossip POST occurred. Avoid hard-coded generated clue prose; use the real command response as the fact identity and assert its corresponding learned clue/source semantics.

- [x] **Step 2: Run the focused route and inspect its failure modes**

Run: `npm --prefix src/WildBunch.Web run build`

Run: `npm --prefix src/WildBunch.Web run test:browser -- --grep "saloon gossip"`

Expected: the production bundle reaches Saloon through the normal player flow, the real gossip action returns and displays one recorded learned fact, and reopening restores the same fact from the server. The browser harness must create and remove only its uniquely named disposable database.

- [x] **Step 3: Prove the test detects a non-committing gossip action**

Temporarily intercept the gossip POST with a success-shaped response that contains a plausible updated journal/clue but does not forward the request to the API. Confirm the immediate response-based assertion can observe that supplied fact, while the fresh session read after reopen omits it and the browser test fails. Restore the test file byte-for-byte, verify its before/after SHA-256, rerun the focused test against the real API and confirm it passes. Do not commit the interception.

- [x] **Step 4: Reconcile evidence and successor artifacts, then validate**

Update PG-005 evidence with the bounded local route, real gossip command, disclosed clue in the player journal and restored server read; retain limits for other knowledge sources, hosted behavior and deployment. In the row-18 roadmap, record N-88's merged PR #391, source and squash-merge identities, both hosted gates and paired worktree/branch cleanup, then point the active plan to N-89. Delete the completed N-88 plan in this first substantive implementation commit and change the sole authored application version to `0.1.0-dev.207`. Run `npm --prefix src/WildBunch.Web run build` and the full `npm --prefix src/WildBunch.Web run test:browser`. Stage the intended tree and commit through the normal check-only hook; its `py -3 tools/run.py ci --check` result is the canonical local gate. Do not rerun the full canonical gate immediately before or after a successful hooked commit.

## Task 2: Review, publish, merge and clean N-89

**Files:** Review the committed N-89 diff, this plan, the baseline specification, PG-005, row 18, applicable ADRs and the browser test assertions.

**Consumes:** The focused browser proof, feature-matrix update, N-88 successor-plan retirement and passing canonical local hook.

**Produces:** Independent whole-branch review, a Draft PR to `develop`, passing exact-head hosted CI and develop-push CI, a verified fast-forwarded primary checkout, and paired worktree/branch retirement.

- [ ] **Step 1: Complete readiness and independent review**

Use the `handoff-gates` readiness procedure and prepare a fresh-context whole-branch review package from current `origin/develop` to `HEAD`. Include this plan, the applicable specification and ADR/feature-matrix comparisons, the non-committing failure witness and test results. Follow the user's standing authorization to dispatch the independent reviewer without asking again; use gpt-6-luna. Resolve any findings and repeat review after corrections.

- [ ] **Step 2: Publish and verify the Draft PR**

Run the PR runbook. Push the branch, open a Draft PR targeting `develop`, attach it to this task if the app permits, then read back the PR head/body/state and exact-head hosted checks. Do not merge until exact-head hosted CI passes.

- [ ] **Step 3: Merge and verify integration**

Merge the reviewed PR to `develop` using the repository's current accepted method. Wait for and verify the develop-push gate on the exact merge commit. Advance the primary checkout to that merge while preserving unrelated user changes.

- [ ] **Step 4: Retire the worktree and exact branch refs together**

Use the `finishing-a-development-branch` worktree script in check/apply mode. Verify the PR head, merged tree equivalence and clean worktree before removing the exact local and upstream feature branches. Confirm the worktree, local branch and remote tracking refs are all absent; keep this plan through its completing PR for assessment by the next slice.
