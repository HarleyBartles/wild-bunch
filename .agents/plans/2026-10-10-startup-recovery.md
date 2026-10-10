# Restore saved playthroughs and reconcile confirmed start over

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task.

**Goal:** A saved game resumes from server state without a transient read failure offering a blank new game, and confirming Start Over resolves correctly when the archive response is lost.

**Architecture:** Browser storage remains a pointer to one session ID. The server-side session view is authoritative for the player, while the event history reconstructs invalidated cached state. Session bootstrap distinguishes no saved ID, a saved session being restored, an archived save, and an unresolved read failure; failure never masquerades as a new game. Start Over keeps the existing `PlaythroughArchived` fact; when its response is uncertain, the client reads the same session and reconciles from persisted status instead of guessing from error text or emitting another archive.

**Tech Stack:** React, TanStack Query, TypeScript, existing ASP.NET session read API, event-sourced `GameSession`, xUnit, Vitest and repository command bus.

**Spec:** [Stable 0.1.0 baseline, PG-001 and PG-002](../specs/2026-10-07-stable-0.1.0-baseline.md#product-boundary), plus the [playthrough lifecycle contract](../investigations/stable-0.1.0/2026-10-07-playthrough-lifecycle-contract.md).

**Execution Strategy:** `executing-plans` inline. Bootstrap, route gating and archive reconciliation share one client session-lifecycle state and must be verified as a continuous saved-session lifecycle; API, state and UI tasks are sequential and do not form independent parallel tracks.

## Global Constraints

- `Directory.Build.props` remains the only authored application version source; this PR uses `0.1.0-dev.67`.
- The event stream remains the authority for whether a playthrough was archived; preserve the existing `PlaythroughArchived` event and do not add an archive event, direct mutation, schema or migration.
- Browser storage remains a session ID pointer, never game state; saved phase and archive status come from the server session query.
- Keep the one-playthrough player flow, global current-session behavior, no-account scope and accepted stale-other-tab behavior; do not introduce ownership, session lists, cross-tab synchronization or SignalR.
- An initial saved-session read failure must not offer setup or create/replace a game; keep the saved ID until server truth is known.
- Archive recovery must not create a second archive event or clear the pointer while the server still reports an active session.
- Follow the current CQRS/event-sourcing boundaries, ADRs, feature-matrix playbook, React component contracts and behavior-first testing rules.
- Each epic plan uses a fresh worktree from current `develop`, publishes to `develop`, advances the sole authored development version, and is retired by its successor after merge evidence is recorded.
- Use the fail-fast command bus and check-only commit hook; do not run the full gate immediately before a normal hooked commit.

## Review Focus

- With a saved ID and an initial network/server failure, show recovery, retain the ID, and prevent setup until retry succeeds or the player explicitly leaves a confirmed missing save.
- Refreshing into an archived save returns to setup without issuing another archive command or deleting event history.
- If archive POST fails after commit, a fresh GET reporting Archived resolves Start Over successfully and clears the pointer.
- If POST and reconciliation fail, or reconciliation reports Active, retain the pointer and report failure instead of claiming success.
- Never use error-message substrings such as `409` or `already archived` as proof that an event committed.

---

### Task 1: Advance the roadmap and commit this JIT plan

**Files:**
- Create: `.agents/plans/2026-10-10-startup-recovery.md`
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `Directory.Build.props`
- Retire: `.agents/plans/2026-10-10-startup-seed-submission.md`

**Interfaces:**
- Consumes: merged PR #252 source `17586ce131af2f1f7af774e6389837ea58caaf91`, squash merge `98a0af6ea6e80727fb14822e50c6cc0e3a3a101b`, and passing exact-head hosted canonical gate run `38068685859`.
- Produces: committed `.67` plan from `develop` at `98a0af6ea6e80727fb14822e50c6cc0e3a3a101b`, row 09 still executing with this plan as current JIT slice, and completed `.66` plan retired in this successor.

- [ ] Record PR #252's source, merge and exact-head hosted gate in row 09; record seed submission and session-bound prologue as implemented while saved-phase resume and archive-response reconciliation remain.
- [ ] Advance the sole authored version to `0.1.0-dev.67`, point row 09 to this plan, retire the completed `.66` plan, and commit these planning changes before implementation.

Run `rg -n "<Version>|0\.1\.0-dev\.67" Directory.Build.props` to confirm the sole authored version. Stage the version, roadmap, new plan and retired predecessor, then commit with `git add Directory.Build.props .agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md .agents/plans/2026-10-10-startup-recovery.md .agents/plans/2026-10-10-startup-seed-submission.md` and `git commit -m "plan: restore startup and archive recovery"`.

### Task 2: Make session bootstrap distinguish restoration from absence

**Files:**
- Modify: `src/WildBunch.Web/src/api/httpClient.ts`
- Modify: `src/WildBunch.Web/src/hooks/useGameSessionQueries.ts` and `useCurrentGameSession.ts`
- Modify: `src/WildBunch.Web/src/state/GameSessionProvider.tsx` and `src/WildBunch.Web/src/shell/AppShell.tsx`
- Modify: `src/WildBunch.Web/src/shell/usePhaseRouteSync.ts`
- Create: `src/WildBunch.Web/src/components/session-recovery/SessionRecoverySurface.tsx`
- Create: `src/WildBunch.Web/src/tests/httpClient.test.ts`
- Test: `src/WildBunch.Web/src/tests/AppShell.test.tsx` and `usePhaseRouteSync.test.tsx`.

**Interfaces:**
- Consumes: saved session ID from `readStoredGameId`, `getGame(id)`, `GameSessionDto.status` and `startFlowPhase`, and the route derived from `usePhaseRouteSync`.
- Produces: distinct restoring, restored, no-saved-session, missing-saved-session and recoverable-read-failure outcomes; a retry that reads the same saved ID; and an explicit action to abandon a confirmed missing save before returning to setup.

- [ ] Add a failing HTTP client behavior test proving a 404 response preserves its status and server message on the thrown request error; prove a failed fetch is distinguishable from an HTTP 404.
- [ ] Add a failing shell behavior test for a stored ID whose GET fails transiently: retain the ID, show recovery, hide setup/gameplay surfaces, and do not redirect a deep link based on a fabricated pre-session phase.
- [ ] Add a failing retry behavior test proving retry reads the same saved ID and, after success, renders the phase supplied by that session; reuse the existing saved-prologue behavior where it already proves its contract.
- [ ] Add a failing 404 behavior test: do not silently offer replacement on load; disclose the missing save and require an explicit player action before clearing only its local pointer and showing setup.
- [ ] Implement the typed HTTP error and explicit bootstrap states; block setup and phase routing while a saved session is unresolved, show the recovery surface, preserve the pointer on transient failures, and allow explicit recovery only after a confirmed missing save.
- [ ] Run `npm --prefix src/WildBunch.Web exec -- vitest run src/tests/httpClient.test.ts src/tests/AppShell.test.tsx src/tests/usePhaseRouteSync.test.tsx src/tests/StartFlow.test.tsx`; witness the new tests fail before implementation and pass after.

### Task 3: Reconcile a confirmed archive after an uncertain response

**Files:**
- Modify: `src/WildBunch.Web/src/hooks/useGameSessionMutations.ts`
- Modify: `src/WildBunch.Web/src/hooks/useCurrentGameSession.ts` only if a single shared session-clearing operation is needed.
- Modify: `src/WildBunch.Web/src/tests/StartOverConfirmation.test.tsx`.
- Preserve: `tests/WildBunch.Integration.Tests/GameApiArchiveTests.cs` and `tests/WildBunch.Domain.Tests/GameSessionArchiveTests.cs` as current HTTP persistence and event-replay proof.

**Interfaces:**
- Consumes: existing `archiveGame(id)`, session `getGame(id)`, `GameStatus.Archived`, and the shared pointer/cache clearing behavior.
- Produces: a Start Over mutation that treats a failed POST as completed only after a fresh GET proves that same session is Archived; otherwise it preserves the active ID and rejects with a useful error.

- [ ] Add a failing behavior test where archive POST rejects after server commit and follow-up GET reports Archived; Start Over resolves successfully, clears the local pointer and caches, closes confirmation/settings overlays, and shows the existing completion notice.
- [ ] Add a failing behavior test where archive POST rejects and follow-up GET reports Active; keep the pointer, keep confirmation available for retry/cancel, and show failure without claiming success.
- [ ] Add a failing behavior test where both POST and reconciliation GET fail; preserve the pointer and do not infer success from exception text.
- [ ] Add a failing reload test with localStorage pointing to an already archived session; return to setup without calling archive POST.
- [ ] Implement reconciliation by reading server state after an uncertain archive result; remove message-substring checks and do not change domain archive idempotency, event shape or endpoint semantics.
- [ ] Run `npm --prefix src/WildBunch.Web exec -- vitest run src/tests/StartOverConfirmation.test.tsx src/tests/StartFlow.test.tsx`; witness each new behavior test fail before implementation and pass after.

### Task 4: Reconcile evidence and deliver the lifecycle slice

**Files:**
- Modify: `docs/features.md` PG-001 and PG-002 evidence only where shipped behavior changes the assessment.
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md` row 09 with this slice's PR and hosted gate evidence in the next successor after merge.
- Review: ADR-0014, ADR-0028 and ADR-0039 through the decision-record playbook; leave them unchanged if the implementation preserves their current read, event and replay decisions.

**Interfaces:**
- Consumes: committed implementation, focused tests, local canonical gate, whole-diff review, PR head and hosted gate.
- Produces: a reviewed `.67` PR to `develop` whose behavior and feature-matrix evidence match the accepted saved-session and confirmed-archive contract.

- [ ] Compare the actual source diff with ADR-0014, ADR-0028 and ADR-0039; create or amend a record only if the implementation changes a durable decision, and state the truthful result in the PR.
- [ ] Update PG-001/PG-002 evidence to distinguish behavior-level UI proof, persisted archive evidence and remaining browser/deployed journey limits; do not claim the entire saved-phase resume feature complete unless all saved phases are exercised.
- [ ] Ensure PostgreSQL with `pwsh -NoProfile -File tools/postgres-dev.ps1 ensure` before a database lane; run focused web tests and the existing archive integration tests if implementation changes their evidence boundary.
- [ ] Review the complete diff against this plan, feature matrix and applicable unslop observations; resolve findings with behavior proof, stage only intended changes, and commit normally so the check-only hook runs the canonical `ci --check` gate on the staged candidate. Do not run a separate full gate immediately before or after that hooked commit.
- [ ] Publish the PR to `develop`, read back the exact source SHA, make it ready to start hosted CI, wait for the canonical gate to pass on that SHA, and merge after a clean mergeability check; the next successor records merge/push evidence and retires this plan in its first substantive commit.

## Acceptance

- A stored session ID causes the browser to restore the game from server-reported phase; transient lookup failure never appears as a blank new game or authorizes accidental replacement.
- A server-confirmed missing save is disclosed and requires an explicit player choice before its pointer is cleared and setup is offered.
- Start Over remains backed by `PlaythroughArchived`. If its response is lost after commit, a fresh Archived read completes the transition without a duplicate command; if server truth cannot confirm the archive, the player is informed and the saved ID is retained.
- Reopening a page whose saved session is Archived returns to setup without removing or rewriting event history.
- Focused tests prove these behaviors; report their evidence boundary honestly and make no browser-E2E or multi-user isolation claim.

## Explicit Exclusions

Do not add identity/accounts/OIDC, per-user ownership, multiple saved-game discovery, SignalR/cross-tab synchronization, browser idempotency keys, a new archive event, archive-history deletion, persistence migrations, a second phase state machine or a full deployed browser journey. The separate cloud-playability roadmap owns public-user isolation and account-backed ownership.
