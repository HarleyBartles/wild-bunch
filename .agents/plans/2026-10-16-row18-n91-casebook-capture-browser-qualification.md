# N-91 Preserve captured-person evidence in the casebook

## Status

Active.

## Execution strategy

Use `superpowers-plus:executing-plans` inline in this worktree. This is one sequential browser-acceptance slice; no independent implementation tasks need delegation.

## Goal

Qualify that after a player correctly takes in a wanted person, the player-facing Case file keeps that person's learned identity, public warrant details, captured status and settlement visible after a fresh page load.

## Source contract

PG-008 promises that capture changes a person's status without tearing out or hiding their casebook page. ADR-0005 keeps case facts session-owned; ADR-0007 limits player reads to learned facts; ADR-0027 keeps server state authoritative and React responsible for display; ADR-0047 permits a selected retained browser flow using the real API and page-reopen recovery. Initial browser evidence and a focused domain RED witness show that `SheriffTurnInSettled` currently records the payout but does not add the living target to discovered suspects or mark that suspect Captured. The settled living turn-in event is the authoritative fact that the named target has been secured; applying it must retain the person in the session-owned casebook as Captured, and replay must reconstruct that state. Dead turn-ins must not be labeled Captured. The same evidence exposed that generated warrants carry an exact `TargetSuspectId`, but confrontation matching can ignore it and authorize a different suspect whose name happens to equal a warrant alias. For identified warrants, the ID is the authoritative identity link; names and aliases remain descriptive evidence. Retain name/alias fallback only for warrants without a target identity ID. The browser journey uses the existing development-only saloon override to align the observed person to the exact public warrant under test; its investigation and take-in commands remain the player path.

## Scope and exclusions

- Add a domain behavior test for a successful living turn-in showing the target becomes a discovered Captured suspect on command and event replay; apply the same event-derived invariant when restoring the case-file snapshot.
- Extend the existing PG-006 public-poster, matching-observation and correctly named take-in browser journey to open the Case file after settlement and verify the named suspect remains visible as Captured with the associated warrant's learned details and alive-turn-in settlement.
- Close and reopen the page, require a fresh server journal read, reopen the Case file, and verify the same captured suspect and settled warrant without issuing another take-in command.
- Use the browser harness's uniquely named disposable PostgreSQL database. Do not reset, truncate, or otherwise mutate `wildbunch_dev` for automated acceptance.
- Do not expose suspect IDs in player API or add public developer controls; use the existing development-only saloon override only as deterministic browser-test setup.
- Do not add clues, deduction automation, casebook features, new API fields, browser state authority, alternate interaction flows, or broad PG-008 completion claims.
- Preserve existing behavior unless the real browser contract exposes a specific discrepancy; any necessary repair stays within the already promised case-file display/read boundary and receives its own witnessed RED/GREEN proof.

## Review focus

- The same person identified from the public poster is the one captured, and the casebook associates the settlement with that person's known warrant without leaking internal IDs or hidden culprit/progress state.
- A person's name appearing in a different warrant's alias list cannot override the warrant's exact target-suspect identity.
- The page-reopen assertions come from a fresh GET `/api/games/{id}/journal` and visible Case file content, not a retained React Query cache or the confrontation response.
- Opening and restoring the Case file does not repeat the confrontation command or create player history.
- The evidence updates only the PG-008 boundary actually exercised and does not claim the remaining complete casebook/journal journey is qualified.

## Task 1: Extend the real browser route and reconcile evidence

**Files:** Update `src/WildBunch.Domain/Cases/CaseFile.cs`, `src/WildBunch.Domain/Game/BountyLoop.cs`, `tests/WildBunch.Domain.Tests/GameSessionSheriffTurnInTests.cs`, `tests/WildBunch.Domain.Tests/GameSessionSaloonPersonOfInterestTests.cs`, `src/WildBunch.Web/browser-tests/player-startup.spec.ts`, `docs/features.md`, `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`, and `Directory.Build.props`; delete `.agents/plans/2026-10-16-row18-n90-terminal-death-postgres-api-acceptance.md` in the first substantive implementation commit.

**Consumes:** The existing `a player can start, restore a public poster, and take in the named person through the saloon` Chromium journey, the PG-008 casebook contract, and the current `CaseFileSurface`/`JournalMapper`/`CaseBoardMapper` behavior.

**Produces:** Event-derived domain state that retains a successfully captured person in the casebook, a real production-web-to-API Chromium assertion for captured-person casebook retention and page-reopen restoration, truthful bounded PG-008 evidence, the N-90 successor-artifact retirement, and application version `0.1.0-dev.209`.

- [x] Implement CaseFile behavior so applying an accepted living sheriff settlement both discovers its target and sets SuspectStatus.Captured; ensure CaseFile snapshot restoration derives the same state from persisted living settlements. Leave dead turn-ins outside Captured status.
- [x] Prove the domain behavior with a focused test that fails before the change and passes for both the command path and event replay; assert dead settlements do not mark a suspect Captured, and add a snapshot-restoration assertion for the constructor's independent restoration path.
- [x] Resolve a warrant to its exact suspect ID whenever that ID exists, so another suspect sharing a warrant alias cannot be captured under the wrong identity; prove the failure with a negative domain test.
- [x] Add assertions immediately after the successful named take-in in the existing browser journey that open the player Case file and verify the named person is still listed as Captured, the learned poster feature/alias remains attached to the person’s warrant record, and the casebook records the alive settlement and bounty.
- [x] Close the browser page and reopen the same origin. Await a new successful GET `/api/games/{id}/journal`; assert its case-file projection contains the same suspect status and settled warrant facts, then open the visible Case file and verify those facts render. Track confrontation POSTs and assert the single take-in was not repeated by opening or restoring the read surface.
- [x] Run the focused Playwright journey against the real production web build and uniquely named disposable PostgreSQL API. Prove the new acceptance assertions can fail by temporarily substituting a non-captured label in the `CaseFileSurface` render; the focused journey failed at the visible Captured status assertion. Restore the component byte-for-byte and verify its Git blob matches the planned base.
- [x] Perform a manual browser pass of the same bounded case-file route using this worktree's canonical API/web servers from `scripts/dev-servers.ps1`, keep it separate from automated proof, and record the actual URLs, worktree server match, deterministic observation, case-file contents after capture and page reopen, plus console/network errors.
- [x] Add dated PG-008 evidence that states the exact captured-person, warrant, page-reopen and local-browser boundary and retains the explicit limits on the rest of PG-008, hosted behavior, deployment and whole-release qualification.
- [x] Record N-90's merge of PR #393 from source `b4b49aefaca3d3faff6d4c19577a70c421136566` as `bc4ebf8624576e8422065fedc0c3e73474a5b4c4`, exact-head hosted gate run `38834903479`, develop-push gate run `38835751598`, independent review outcome and paired worktree/branch cleanup in row 18; make N-91 the active plan and retire the N-90 plan file in this first substantive implementation commit.
- [x] Set the sole authored product version to `0.1.0-dev.209`. Compare the final diff with ADR-0005, ADR-0007, ADR-0027 and ADR-0047 and state in the PR that no durable decision changed unless implementation evidence requires otherwise.
- [x] Ensure PostgreSQL with `pwsh -NoProfile -File tools/postgres-dev.ps1 ensure`, run the focused Chromium test with `npm --prefix src/WildBunch.Web run test:browser -- browser-tests/player-startup.spec.ts --grep "take in the named person through the saloon"`, run the repository's manual browser procedure against this worktree's paired servers, then rely on the full `py -3 tools/run.py ci --check` gate through the normal check-only commit hook; do not rerun the full gate immediately before or after a successful hooked commit.

## Task 2: Review, publish, merge and clean N-91

**Files:** Review the committed N-91 diff, this plan, PG-008, the row 18 roadmap entry, applicable ADRs and the Playwright assertions.

**Consumes:** The witnessed real-browser casebook assertion, fresh-server-read and restoration proof, manual browser observation, truthful feature evidence, N-90 plan retirement and development version `0.1.0-dev.209`.

**Produces:** Independent whole-branch review, a Draft PR to `develop`, passing exact-head hosted CI and develop-push CI, a verified fast-forwarded primary checkout, and paired worktree/local/upstream-branch retirement.

- [ ] Use the handoff-gates completion-readiness procedure and prepare a fresh-context whole-branch review package from current `origin/develop` to `HEAD`, including this plan, the baseline and PG-008 comparison, relevant ADR comparison, failure witness, focused and manual browser evidence, and the canonical local hook result. Follow the user's standing authorization to dispatch an independent reviewer without asking again; use gpt-6-luna. Resolve any findings and repeat review after corrections.
- [ ] Follow the PR runbook, push the branch and open a Draft PR targeting `develop`; read back its body, head SHA, draft state and exact-head hosted CI. Do not merge before exact-head hosted CI succeeds.
- [ ] Merge the reviewed PR to `develop`; verify the develop-push gate succeeds on the exact merge commit, then fast-forward the primary checkout while preserving unrelated user changes.
- [ ] Use the worktree finishing helper in check/apply mode; verify PR head, merge tree and clean worktree before removing the exact local and upstream branch refs with the worktree. Confirm all three are absent and leave the N-91 plan through its completing PR.

## Validation boundaries

The browser test's API and PostgreSQL process are isolated from the shared development database. Chromium proof covers one known public warrant and correctly named living take-in path plus case-file read and reopen. It does not certify all wanted people, all casebook records, all journal behavior, other browser engines, hosted behavior, deployment, or a public release.
