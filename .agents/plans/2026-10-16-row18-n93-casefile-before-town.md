# N-93 Prove case-file access before first-town choice

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task.

**Goal:** Prove in a real browser that a player can open the settled case file after starting a game, while the prologue is active and before choosing a town.

**Architecture:** Preserve the existing server-owned start phase, session-scoped prologue and event-derived Journal read. Exercise the current player-facing Case file overlay before first arrival, verify it displays the same session's opening lead, and prove reading it neither acknowledges the prologue nor selects a town.

**Tech Stack:** React, TypeScript, Playwright, Vite production build, ASP.NET API and PostgreSQL-backed isolated browser-test database.

**Spec:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`, especially the PG-001 start order and PG-008 casebook promise; current feature assessment in `docs/features.md#pg-008`. Follow `.agents/runbooks/implementing.md`, `.agents/playbooks/testing.md` and `.agents/playbooks/ui-browser-check.md` during execution.

**Execution Strategy:** `executing-plans` - this is one tightly coupled real-browser proof with its feature evidence and retirement of the completed N-92 plan. Inline execution keeps the game phase, session ID, prologue, case-file query and non-mutation assertion together; subagent-driven development would add task handoffs without independent implementation boundaries. Use one fresh whole-branch review at publication.

## Global Constraints

- Follow the unslop playbook and the play-surface UI and writing profiles; consult the decision-record playbook for ADR-0005, ADR-0007, ADR-0012, ADR-0027 and ADR-0047.
- The game is settled when setup succeeds. The prologue and case file refer to that session before player location is selected; case-file reads do not advance the start flow or create a town.
- Keep hidden culprit identity and internal suspect IDs out of player-facing reads. Do not add APIs, events, overlays, client-owned game facts or new gameplay.
- Use the production web build and the browser harness's uniquely named disposable PostgreSQL database; never reset or truncate `wildbunch_dev` for automated tests.
- Advance the sole authored product version in `Directory.Build.props` to `0.1.0-dev.211` for this PR and verify the generated web identity comes from the build.
- In the first substantive implementation commit, retire the completed N-92 plan and remove its stale roadmap link while recording its actual merged delivery. Keep PG-008 partial and retain the N-93 plan through its completing PR for successor retirement.
- This slice proves case-file access in the pre-town prologue phase only; it does not complete PG-008, the player lifecycle, hosted play or release-candidate qualification.

## Review Focus

- If the case file becomes unavailable until town arrival, the browser route must fail while requiring the Case file control to be enabled and its opening lead visible before town selection.
- If reading the case file advances the start flow or creates a location, the test must fail on the unchanged server phase, null current town and absence of prologue-acknowledgement or town-start POSTs.
- If the opening lead is absent from the early case-file projection, the test must fail on the exact server-projected lead rendered in the overlay.

---

### Task 1: Prove the case file is readable during the prologue

**Files:** Modify `src/WildBunch.Web/browser-tests/player-startup.spec.ts`, `docs/features.md`, `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md` and `Directory.Build.props`; delete `.agents/plans/2026-10-16-row18-n92-journal-views-browser.md` in the first substantive implementation commit.

**Consumes:** The existing production-browser start/reopen route, `GET /api/games/{id}/prologue`, `GET /api/games/{id}/journal`, the shell-owned Case file overlay and the PG-001/PG-008 phase and knowledge contracts.

**Produces:** A Chromium assertion that the active session remains in prologue with no town while the case file displays its opening lead, a witnessed failure if the surface is disabled before arrival, truthful bounded PG-008 evidence, N-92 delivery recorded in the roadmap, the N-92 plan retired, and product version `0.1.0-dev.211`.

- [x] Add a production-browser route named `a player can read the case file before town selection` that starts a game by player name, closes and reopens the page, and captures fresh successful session, prologue and journal responses for the created session ID.
- [x] Before clicking the prologue's `Ride on` action, assert the restored session is still in the prologue phase, both the session and journal have no current town, the prologue body is visible, and the journal's opening lead is nonempty.
- [x] Open the existing `Case file` control before town choice, assert it is enabled and the dialog visibly renders the exact opening lead from that fresh journal response, then assert no prologue-acknowledgement or town-start POST was issued and server phase/location remain unchanged.
- [x] Prove the browser test can fail for the intended reason by temporarily disabling the Case file control when the journal has no current town; run the focused test and confirm it fails at the early-access assertion. Restore the production component byte-for-byte and rerun the test successfully.
- [x] Ensure PostgreSQL with `pwsh -NoProfile -File tools/postgres-dev.ps1 ensure`, build the production web app with `npm --prefix src/WildBunch.Web run build`, run `npm --prefix src/WildBunch.Web run test:browser -- browser-tests/player-startup.spec.ts --grep "case file before town selection"`, and perform a separate manual browser check using this worktree's paired API/web servers. Prove the web build calls this worktree's API; record the actual URLs, pre-town phase, visible case-file lead and console/network result separately from automation.
- [x] Add dated PG-008 evidence stating the case file was opened before town selection after page reopen, naming the event-backed session/lead facts proven and retaining the partial-assessment boundary.
- [x] Update the roadmap to record N-92 PR #395, source `3d9e8e1bc66579454de15ac0aa1d86bb623c953c`, merge `8993ba3dba73553556b6c207dea9fdc87c7c043e`, exact-head gate `38849341215`, develop-push gate `38850090855`, fresh review result and paired worktree/branch cleanup. Make N-93 the active row-18 plan and remove the stale claim that N-92 observation/publication remains in progress.
- [x] Set `Directory.Build.props` to `0.1.0-dev.211`; compare the actual diff with ADR-0005, ADR-0007, ADR-0012, ADR-0027 and ADR-0047, leaving them unchanged if no durable decision changed.
- [x] Pass the normal check-only commit hook for `py -3 tools/run.py ci --check`; do not bypass it or rerun the full gate immediately before or after a successful hooked commit.

### Task 2: Review, publish, merge and clean N-93

**Files:** Review the committed N-93 diff, this plan, the row-18 roadmap entry, PG-008 evidence, the applicable ADRs and the browser route.

**Consumes:** The red/green browser witness, separate manual paired-server observation, bounded feature evidence, N-92 retirement and product version `0.1.0-dev.211`.

**Produces:** Independent whole-branch review, a reviewed PR to `develop`, passing exact-head hosted CI and develop-push CI, a verified fast-forwarded primary checkout and paired worktree/local/upstream-branch retirement.

- [ ] Build a fresh-context review package from current `origin/develop` to `HEAD`, including this plan, PG-008 and ADR comparison, mutation witness, manual and automated browser evidence and canonical hook result. Use the standing authorization for one gpt-6-luna reviewer; resolve findings and repeat review after any correction.
- [ ] Follow the PR runbook, push the branch and open a Draft PR targeting `develop`; read back its body, exact head SHA, draft state and checks. Mark it ready only after independent review and the committed local gate pass; merge only after exact-head hosted CI succeeds.
- [ ] Merge the reviewed PR to `develop`; verify the develop-push gate passes on the exact merge commit, then fast-forward the primary checkout without disturbing unrelated user changes.
- [ ] Use the finishing helper in check/apply mode; verify the merged PR head and result and clean worktree, then delete and verify absence of the exact local and upstream branch refs together with the worktree. Keep the N-93 plan through this PR for semantic retirement in the next substantive slice.

## Validation Boundaries

This proves one local browser can read one settled case file in the prologue phase before first-town selection. It does not establish every case-file state, all player paths, other browser engines, deployed behavior, hosted play or complete PG-008 qualification.
