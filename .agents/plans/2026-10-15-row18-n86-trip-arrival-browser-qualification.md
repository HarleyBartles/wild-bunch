# N-86: Qualify trip completion and arrival in the public browser

**Status:** executing

**Roadmap:** [Stable 0.1.0 cleanup, row 18](../roadmaps/2026-10-07-stable-0.1.0-cleanup.md)

**Specification:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md)

**Base:** `origin/develop` at `44b2f73449dce89a56de5462c6f871f2bcbb0e73`.

**Branch:** `codex/row18-n86-trip-arrival-browser-qualification`.

**Development version:** `0.1.0-dev.204`.

## Outcome

Add one deterministic production-browser qualification that completes a real map-connected journey, restores the completed arrival after page reopen, acknowledges arrival through the visible player action, and verifies the destination becomes the server-owned current town after another reopen without duplicate acknowledgement. This closes the specific browser-evidence gap for trip completion and arrival; it does not qualify all map routes, encounter outcomes, deployed behavior, all PG-007 behavior, or the complete 0.1.0 candidate.

## Settled scope and constraints

- Preserve one player-visible journey from town selection through travel completion and arrival; do not add a second map or arrival flow.
- Use the real public setup, map, travel preparation, legal travel-day action and `Step into town` action against the isolated PostgreSQL-backed API. No test-only application endpoint or developer control is permitted.
- Reuse the fixed canonical world seed and the player-selectable Standard difficulty with Boring entropy. Existing N-84 and N-85 probes establish that the longest real connected route is multi-day and that this Standard/Boring fixture completed without a hostile encounter; verify those conditions again in the N-86 browser test rather than silently skipping or changing production behavior.
- The server event history owns trip completion and arrival. `JourneyCompleted` already moves the player to the destination and applies deterministic arrival side effects; the completed journey remains active in its completed state until the player acknowledges arrival. `JourneyArrivalAcknowledged` archives that journey and clears it so the town surface becomes available. Reopening before and after acknowledgement must restore the corresponding event-derived state without issuing either command again.
- The shared development database is not part of this proof. Browser tests use the repository's isolated disposable PostgreSQL database through `startIsolatedApi`.
- No ADR changes are expected because this qualifies already-settled travel behavior. Recheck the decision-record playbook against the committed diff and state the result in the PR.
- Update PG-007 dated evidence only for the observed completed journey, restored arrival, acknowledged destination and no-duplicate reopen. Keep other unqualified scope explicit.

## Execution strategy

Use the approved autonomous roadmap mode with this JIT plan executed inline, `executing-plans`, TDD, the Wild Bunch browser-game, dotnet architecture, domain-modeling, testing and feature-matrix guidance, plus scoped unslop profiles for browser tests, travel transitions, and planning prose. Keep the implementation to the public-browser behavior and evidence slice; do not expand into all encounters or deployed release qualification.

## Task 1: Establish the deterministic completed-trip browser route

**Files:** inspect `src/WildBunch.Web/browser-tests/player-startup.spec.ts`, its PostgreSQL browser-test setup, travel DTOs and UI; do not change production behavior to create a fixture.

**Consumes:** N-83 departure proof, N-84 travel-day proof, N-85 hostile-encounter proof, the fixed seed helper, and current public travel actions.

**Produces:** the verified Standard/Boring route and the observable server/UI states needed to test completion followed by arrival acknowledgement.

- [x] **Step 1: Confirm the real route and completion contract**

Inspect the current helper and public route. Use the longest connected trail selected from the real returned map, confirm its public preview is multi-day, start it through the visible controls, and advance using only legal visible actions until the server returns `JourneyStatus.Completed`. The test must fail if the fixture unexpectedly interrupts, lacks a route, or cannot complete within the route's expected days plus a small documented margin. Record the actual completion response fields relevant to origin, destination, status, days travelled, and player current town.

- [x] **Step 2: Implement browser restoration and arrival acknowledgement proof**

Add a browser scenario that closes and reopens after completion but before acknowledgement; assert the real session GET restores the same completed journey and destination, the player's current town is already the destination, the completed-arrival narrative is visible, and `Step into town` is the legal action while ordinary town actions remain unavailable. Click it once and assert the real arrival acknowledgement POST succeeds, the returned session archives and clears the active journey, and the destination town surface becomes available. Reopen once more; assert the destination and archived arrival state are restored from the server and the command was not posted again. Observe endpoint POSTs to prove the advance count matches the actual completed route and arrival acknowledgement occurs exactly once.

- [x] **Step 3: Prove the new browser scenario detects a broken arrival path**

After the scenario passes, temporarily break the arrival acknowledgement handler so it rejects or fails to apply the arrival result. Run only the new browser scenario and confirm it fails at the expected persisted destination or restored player-state assertion. Restore production code byte-for-byte, rerun the focused browser scenario, and do not commit the mutation.

- [ ] **Step 4: Reconcile PG-007 evidence and run the canonical gate**

Add dated PG-007 evidence for this one completed trip and acknowledged arrival, with the exact verified route and restored result. State that other travel routes, encounter variants, deployed behavior, all PG-007 behavior and overall 0.1.0 qualification remain unproven. Run `npm run build` and `npm run test:browser` in `src/WildBunch.Web`. Commit through the normal check-only hook, which runs `py -3 tools/run.py ci --check` on the staged candidate; use that hook result as the canonical gate instead of duplicating the full gate immediately before commit. Keep disposable API, database, and browser artifacts outside the repository.

## Task 2: Review, publish, merge and retire N-86

**Files:** review the committed N-86 diff, this plan, the baseline specification, applicable ADRs, `docs/features.md`, and the row-18 roadmap; update this plan with the execution result.

**Consumes:** the committed browser qualification and passing canonical local gate.

**Produces:** independent whole-branch review, reviewable PR to `develop`, passing exact-head hosted CI, merge commit with passing develop-push CI, fast-forwarded primary checkout, and paired cleanup of N-86 worktree and branch refs.

- [ ] **Step 1: Complete readiness and independent review**

Use completion-readiness and prepare a fresh-context whole-branch review package from `origin/develop` to `HEAD`, including this plan, the specification, changed-concern review focus, execution rulings, ADR comparison, and feature-matrix comparison. Use the standing authorization for independent review. Resolve Critical and Important findings with witnessed RED/GREEN and rerun the canonical gate; defer only Minor findings with an explicit reason.

- [ ] **Step 2: Publish and verify the PR**

Push the plan branch and create a Draft PR targeting `develop`. Read it back and verify exact head SHA, source diff, ADR and feature-matrix comparisons, validation claims, and draft state. Mark it ready and require the hosted canonical gate to pass on that exact head before merge.

- [ ] **Step 3: Merge and verify integration**

Squash-merge the reviewed PR into `develop`, verify the merge commit contains the reviewed source, and wait for the develop-push canonical gate to pass on that merge commit. Do not continue if the merge tree or hosted gate differs or fails.

- [ ] **Step 4: Refresh and clean the exact execution unit**

Fast-forward the primary checkout to the verified `develop` merge without disturbing its unrelated investigation edit. Use `finishing-a-development-branch` and its remove-worktree script with `--check` then `--apply`; remove only the N-86 worktree, then delete the matching local and upstream branch refs together. Verify PR source/merge identities, both hosted gates, primary `HEAD`, preserved user edit, and absence of the worktree and both refs. Leave this N-86 plan and roadmap evidence for N-87 to assess and retire.

## Execution rulings

Record only consequential fixture or behavior findings from implementation here; promote durable truth to the feature matrix or ADR when appropriate. Keep test-run receipts in Git history and the off-repository execution ledger rather than duplicating them in tracked docs.

- **Ruling, 2026-10-15:** Source and `TravelEventApplyTests` confirm `JourneyCompleted` already moves `Player.CurrentTownId` to the destination and applies arrival side effects. `JourneyArrivalAcknowledged` archives and clears the completed active journey; it does not move the player. N-86 must assert this event-owned transition accurately and prove that the arrival screen gates access to the town surface until acknowledgement.
- **Ruling, 2026-10-15:** The fixed canonical seed, Standard difficulty and Boring entropy complete the longest connected public route without an encounter. The browser test verifies the preview is multi-day, advances only through visible legal actions, and observes a completed journey within `expectedDays + 2` actions.
- **Ruling, 2026-10-15:** Replacing the arrival handler result with a lawful rejection made the new browser scenario fail at `acknowledged.success`; restoring the handler byte-for-byte made the focused scenario pass again. The mutation was not committed.
