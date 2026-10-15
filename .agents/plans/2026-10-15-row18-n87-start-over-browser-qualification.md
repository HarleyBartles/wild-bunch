# N-87: Qualify confirmed Start Over in the public browser

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task. Steps use checkbox syntax for tracking.

**Goal:** Prove through the production web build and isolated PostgreSQL-backed API that cancelling Start Over leaves a playthrough active, while confirmation archives it durably before any replacement game exists.

**Architecture:** Extend the existing Playwright production-browser suite through the public setup, prologue, map and town surfaces, then exercise the player-facing Game Settings confirmation. The server event history remains authoritative; the browser test observes the real archive command and reads the persisted archived status after reopening without issuing a second archive or setup command.

**Tech Stack:** React, Playwright Chromium, .NET API, PostgreSQL, repository command bus.

**Spec:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md), including the PG-002 confirmed archive contract and row 18 qualification requirement.

**Execution Strategy:** `executing-plans` - this is one tightly coupled browser behavior with a single evidence update, and inline execution keeps the real browser/API/PostgreSQL observations together while avoiding per-task review overhead.

## Global Constraints

- `develop` is the integration branch; merge this plan through one reviewed PR to `develop`.
- The development version for this PR is `0.1.0-dev.205`, authored only in `Directory.Build.props`.
- Use the production web build and public player actions against the isolated PostgreSQL-backed browser-test API; do not call developer endpoints or edit database rows to create the scenario.
- Confirmed Start Over commits archival before returning to setup, and cancellation leaves the playthrough unchanged.
- The event stream is authoritative; the client may clear its saved playthrough pointer only after the archive command succeeds.
- Keep the plan and its PR's result evidence until the PR completes; retire the completed N-86 plan in this successor's first substantive implementation commit.
- Do not add an ADR because the slice qualifies the already-settled PG-002 behavior; compare the final diff through the decision-record playbook.

## Review Focus

- Cancellation must not post the archive command, clear the saved game pointer, or leave the in-town surface; the Playwright test observes the request count and continued active game.
- Confirmation must produce exactly one persisted archive, survive closing and reopening before a replacement game is created, and return to setup; the Playwright test observes the API result, a fresh server read, and the reopened public page.
- Page reload after archival must not repeat the archive command or silently create a new game; the Playwright test observes archive and setup POST counts.

---

## Settled scope

N-64 and related PG-002 behavior tests already cover retry and response-reconciliation rules; N-70 covers modal keyboard/focus behavior; API/PostgreSQL integration tests cover persisted archive responses. This slice adds the missing production-browser connection between player confirmation and durable server archival. It does not add account ownership, archived-game browsing, cross-tab synchronization, a new start-over flow, or new archive semantics.

## Task 1: Add the real-browser Start Over lifecycle proof

**Files:** Modify `src/WildBunch.Web/browser-tests/player-startup.spec.ts`; update `docs/features.md`; update `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`; delete `.agents/plans/2026-10-15-row18-n86-trip-arrival-browser-qualification.md` in the first substantive implementation commit; update `Directory.Build.props` to `0.1.0-dev.205`.

**Consumes:** The current public start flow, `startIsolatedApi`, the Playwright production-web server, the player-facing Game Settings/Start Over confirmation, the archive endpoint, and the existing `GameApiArchiveTests` persisted-state proof.

**Produces:** One deterministic browser scenario that observes a cancelled archive as a no-op, then confirms Start Over, verifies the returned and freshly read server session is archived, closes and reopens the browser to the setup surface, and proves no duplicate archive or replacement setup was sent.

- [ ] **Step 1: Add the browser scenario first**

Add a Playwright test to `player-startup.spec.ts` that starts a named game through the public setup, acknowledges the prologue, selects an API-returned town, and waits for the real town surface. Track `POST /api/games/setup` and `POST /api/games/{id}/archive` requests. Open `Game Settings`, choose `Start Over`, then choose `Keep riding`; assert the dialog closes, the settings/town remain usable, the archive POST count is zero, and a server GET still reports the playthrough `Active`. Reopen the confirmation, choose `Archive and start over`, and assert exactly one archive POST returns `Archived`. Close the page and reopen the public app in the same browser context; assert the player sees the setup form, a direct read of the original game still reports `Archived`, and neither another archive POST nor a new setup POST occurred. Keep all API operations on the real public endpoints and all writes in the isolated disposable database.

- [ ] **Step 2: Run the focused browser test and inspect the first result**

Run: `npm --prefix src/WildBunch.Web run build`

Run: `npm --prefix src/WildBunch.Web run test:browser -- --grep "confirmed Start Over"`

Expected: the new test reaches the player confirmation through the built public app, verifies cancellation as a no-op, then verifies one durable archive and setup-page restoration after reopening. If the browser test cannot start the isolated PostgreSQL-backed API, report that prerequisite failure and repair only the named environment prerequisite.

- [ ] **Step 3: Prove the browser test detects a broken archive path**

Temporarily change the archive mutation so it returns success without calling `archiveGame`, run only the new browser scenario, and confirm it fails because the real persisted game remains active or the expected archive request is absent. Restore the mutation byte-for-byte, rerun the focused test, and confirm it passes. Do not commit the deliberate mutation.

- [ ] **Step 4: Reconcile feature and successor artifacts, then validate**

Update PG-002 dated evidence to state only the observed local production-browser behavior, persisted archive status after a fresh server read, and no-duplicate/no-replacement result; retain explicit limits for accounts, archived-game restoration, deployed behavior and complete player lifecycle. In the row-18 roadmap, record N-86's merged PR and exact hosted gates, point the active plan to N-87, and delete the completed N-86 plan in this first substantive implementation commit. Change the sole authored application version in `Directory.Build.props` to `0.1.0-dev.205`. Run `npm --prefix src/WildBunch.Web run build` and the full `npm --prefix src/WildBunch.Web run test:browser`; start PostgreSQL with `pwsh -NoProfile -File tools/postgres-dev.ps1 ensure` before the browser lane. Stage the intended tree and commit through the normal check-only hook; its `py -3 tools/run.py ci --check` result is the canonical local gate. Do not rerun the full canonical gate immediately before or after a successful hooked commit.

## Task 2: Review, publish, merge and clean N-87

**Files:** Review the committed N-87 diff, this plan, the baseline specification, PG-002, row 18, applicable ADRs and the browser test's actual assertions.

**Consumes:** The focused browser proof, feature-matrix update, successor-artifact retirement and passing canonical local hook.

**Produces:** Independent whole-branch review, a reviewable PR to `develop`, passing exact-head hosted CI and develop-push CI, a verified fast-forwarded primary checkout, and paired worktree/branch retirement.

- [ ] **Step 1: Complete readiness and independent review**

Use `handoff-gates` completion-readiness, then prepare a fresh-context whole-branch review package from the current `origin/develop` to `HEAD`. Include the plan, specification, current ADR and feature-matrix comparisons, and the witnessed test mutation. Use the standing authorization for an independent reviewer. Resolve Critical and Important findings with witnessed RED/GREEN and rerun the canonical gate; defer only Minor findings with an explicit reason.

- [ ] **Step 2: Publish and verify the PR**

Push the branch and create a Draft PR targeting `develop`. Read back the PR head, source diff, body, Draft state and status checks. Confirm the PR carries `0.1.0-dev.205`, then mark it ready only after review and local validation are complete. Do not merge until hosted canonical CI passes on the exact PR head.

- [ ] **Step 3: Merge and verify integration**

Squash-merge the reviewed PR to `develop`, verify the merge contains the reviewed source and `0.1.0-dev.205`, then wait for the develop-push canonical gate to pass on that exact merge commit.

- [ ] **Step 4: Refresh and clean the execution unit**

Fast-forward the primary checkout to the verified merge while preserving `.agents/investigations/stable-0.1.0/2026-10-07-web-test-followup.md`. Use `finishing-a-development-branch` and its remove-worktree script with `--check` then `--apply`; remove only the N-87 worktree, then delete its exact local and upstream branch refs. Verify the PR source/merge identities, both hosted gates, primary `HEAD`, preserved user edit, and absence of the N-87 worktree and refs.

## Execution rulings

Record only consequential browser or behavior findings discovered during execution. Keep test output, screenshots and command receipts in branch-scoped off-repository scratch; retain durable product truth in `docs/features.md` and delivery facts in the roadmap.
