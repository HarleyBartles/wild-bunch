# N-88: Qualify Store purchase in the public browser

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task. Steps use checkbox syntax for tracking.

**Goal:** Prove through the production web build and PostgreSQL-backed API that a player can buy a currently offered Store item from the public town route and restore the event-backed wallet and inventory after reopening.

**Architecture:** Extend the existing Playwright production-browser suite through public setup, prologue, shared map, arrival, town Store and a real purchase. The API supplies the active town offer; the server response updates the visible purchase confirmation and inventory, and reopening plus a fresh server read proves the purchase was committed rather than held only in browser state.

**Tech Stack:** React, Playwright Chromium, .NET API, PostgreSQL, repository command bus.

**Spec:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md), including the PG-004 Store purchase contract and row 18 qualification requirement.

**Execution Strategy:** `executing-plans` - one tightly coupled public purchase and restoration behavior has a single browser test, one evidence update and one version checkpoint; inline execution preserves shared API/browser context without repeated implementer handoffs.

## Global Constraints

- `develop` is the integration branch; deliver this plan through one reviewed PR to `develop`.
- The development version for this PR is `0.1.0-dev.206`, authored only in `Directory.Build.props`.
- Use the public production web build and player actions against the uniquely named disposable PostgreSQL database created by `startIsolatedApi`; do not use or drop the shared development database or mutate rows to create the scenario.
- The server and event history own wallet and inventory truth; browser state only displays the command result and server read.
- Purchase a currently offered item at its current API-returned price; do not hard-code a price or expand store catalog, balancing, vendor, stock or purchase semantics.
- Keep the N-88 plan and its result evidence until its PR completes; retire the completed N-87 plan in N-88's first substantive implementation commit.
- Do not add an ADR because this slice qualifies the settled PG-004 purchase contract; compare the final diff through the decision-record playbook.

## Review Focus

- Store entry must be reachable from the arrived town through the public building interaction, and the purchase request must use that session, town, API-returned item and quantity.
- The visible and restored wallet and inventory must match the persisted server result, not merely the successful mutation response or optimistic browser state.
- The purchase response must be visible to the player, and reopening after purchase must not issue a second purchase; the test observes the receipt, purchase request count and server-owned inventory.

---

## Settled scope

PG-004 already has API/PostgreSQL acceptance coverage for a successful two-Food purchase, wallet debit, inventory increase, purchase log entry and persisted reload, while web component tests cover Store feedback and request wiring. N-88 supplies the missing production-browser connection from public town entry to Store purchase, visible receipt and restored server-owned state. It does not certify every catalog tier, purchase rejection, travel consumption, horse/canteen lifecycle, cross-tab updates, account ownership or deployed behavior.

## Task 1: Add the real-browser Store purchase and restoration proof

**Files:** Modify `src/WildBunch.Web/browser-tests/player-startup.spec.ts`; update the PG-004 evidence in `docs/features.md`; update `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`; delete `.agents/plans/2026-10-15-row18-n87-start-over-browser-qualification.md` in the first substantive implementation commit; update `Directory.Build.props` to `0.1.0-dev.206`.

**Consumes:** The public setup/prologue/map/arrival route, `startIsolatedApi`, the production-web Playwright server, the public Store building route, `GET /api/games/{gameSessionId}/towns/{townId}/store-offers`, `POST /api/games/{gameSessionId}/towns/{townId}/store/buy`, the session read, purchase journal projection and `StorePurchaseAcceptanceTests`.

**Produces:** One deterministic public-browser scenario that buys two Food at the live offer price, verifies the server response's cash and Food changes, closes and reopens the same browser context, reads the session from the API, and proves the same wallet, inventory and single purchase journal result without another purchase.

- [ ] **Step 1: Add the browser scenario first**

Add a named-game test to `player-startup.spec.ts` that starts via public setup, acknowledges the prologue, selects an API-returned starting town on the shared map, and waits for town arrival. Track `POST /api/games/setup` and `POST /api/games/{gameSessionId}/towns/{townId}/store/buy`. Read the starting `GameSessionDto` and Store offers through the public browser requests; select the Food offer from the server-returned catalog and set quantity to two using its accessible quantity label. Enter Store through the town's Store building control and press Buy. Assert exactly one setup POST and one purchase POST, the purchase request contains the actual session ID, arrived town ID, Food item kind and quantity 2, the API response succeeds, wallet cash decreases by exactly twice the response offer price, Food increases by exactly two, and the Store displays the server-returned purchase confirmation. Close the page, reopen the public app in the same browser context, observe the fresh `GET /api/games/{id}`, enter Store again, and assert the visible wallet and Food quantity match the server result, the purchase confirmation is not replayed, and the purchase POST count is still one. Keep the production API endpoint real and all mutations in the unique disposable database created by `startIsolatedApi`.

- [ ] **Step 2: Run the focused browser test and inspect the first result**

Run: `npm --prefix src/WildBunch.Web run build`

Run: `npm --prefix src/WildBunch.Web run test:browser -- --grep "Store purchase"`

Expected: the built public app reaches Store through the town interaction, submits one purchase using the API-returned Food offer, updates the event-backed wallet/inventory, then restores the same values after reopening. The test setup must create and remove its uniquely named database; it must not require a playthrough reset in the shared development database or the user's currently open browser tab.

- [ ] **Step 3: Prove the browser test detects a non-committing purchase**

Temporarily route the browser's Store purchase POST to a success-shaped response containing the pre-purchase `currentSession`, without forwarding the request to the API. Run only the new browser test and confirm it fails because a fresh server read and reopened Store still show the original wallet and Food quantity. Restore the test file byte-for-byte, verify its before/after SHA-256, rerun the focused test against the real API, and confirm it passes. Do not commit the interception.

- [ ] **Step 4: Reconcile feature and successor artifacts, then validate**

Update PG-004's dated evidence to state only the observed local production-browser purchase route, live Food offer price, wallet and inventory deltas, restored server values and visible purchase confirmation; retain explicit limits for other catalog tiers, rejection cases, the remaining resource lifecycle and deployed behavior. In the row-18 roadmap, record N-87's merged PR #390, exact source/merge identities, both hosted gates and paired worktree/ref retirement; point the active plan to N-88. Delete the completed N-87 plan in this first substantive implementation commit and change the sole authored application version to `0.1.0-dev.206`. Run `npm --prefix src/WildBunch.Web run build` and the full `npm --prefix src/WildBunch.Web run test:browser`; the lane creates its own disposable database on the local PostgreSQL service. Stage the intended tree and commit through the normal check-only hook; its `py -3 tools/run.py ci --check` result is the canonical local gate. Do not rerun the full canonical gate immediately before or after a successful hooked commit.

## Task 2: Review, publish, merge and clean N-88

**Files:** Review the committed N-88 diff, this plan, the baseline specification, PG-004, row 18, applicable ADRs and the browser test's actual assertions.

**Consumes:** The focused browser proof, feature-matrix update, N-87 successor-plan retirement and passing canonical local hook.

**Produces:** Independent whole-branch review, a reviewable PR to `develop`, passing exact-head hosted CI and develop-push CI, a verified fast-forwarded primary checkout, and paired worktree/branch retirement.

- [ ] **Step 1: Complete readiness and independent review**

Use `handoff-gates` completion-readiness, then prepare a fresh-context whole-branch review package from the current `origin/develop` to `HEAD`. Include this plan, the specification, current ADR and feature-matrix comparisons, and the witnessed non-committing purchase. Use the standing authorization for an independent reviewer. Resolve Critical and Important findings with witnessed RED/GREEN and rerun the canonical gate; defer only Minor findings with an explicit reason.

- [ ] **Step 2: Publish and verify the PR**

Push the branch and create a Draft PR targeting `develop`. Read back the PR head, source diff, body, Draft state and status checks. Confirm the PR carries `0.1.0-dev.206`, then mark it ready after review and local validation are complete. Do not merge until hosted canonical CI passes on the exact PR head.

- [ ] **Step 3: Merge and verify integration**

Squash-merge the reviewed PR to `develop`, verify the merge contains the reviewed source and `0.1.0-dev.206`, then wait for the develop-push canonical gate to pass on that exact merge commit.

- [ ] **Step 4: Refresh and clean the execution unit**

Fast-forward the primary checkout to the verified merge while preserving `.agents/investigations/stable-0.1.0/2026-10-07-web-test-followup.md`. Use `finishing-a-development-branch` and its remove-worktree script with `--check` then `--apply`; remove only the N-88 worktree, then delete its exact local and upstream branch refs. Verify the PR source/merge identities, both hosted gates, primary `HEAD`, preserved user edit, and absence of the N-88 worktree and refs.

## Execution rulings

Record only consequential browser or behavior findings discovered during execution. Keep test output, screenshots and command receipts in branch-scoped off-repository scratch; retain durable product truth in `docs/features.md` and delivery facts in the roadmap.
