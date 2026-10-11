# Player dialog focus and dismissal

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the player-facing Game Settings and Start Over confirmation dialogs compose as real topmost modals, keeping keyboard focus and dismissal within the active dialog and returning focus to the invoking control.

**Architecture:** Preserve the existing React-owned open state, callbacks, player copy and styled-component display ownership. Use browser-managed `<dialog>.showModal()` for the two player modal components so the browser owns modal stacking, focus containment, outside inertness and close-focus restoration. React handles `cancel` and backdrop requests, including the existing busy-confirmation rule. Do not introduce a general overlay manager or change the separate developer drawer.

**Tech Stack:** React 18, TypeScript, styled-components, Vitest/Testing Library, local PostgreSQL and Playwright Chromium.

**Spec:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`, Web resources and interactions; `.agents/investigations/stable-0.1.0/2026-10-07-web-layer-investigation.md`, WB-12; `.agents/investigations/stable-0.1.0/2026-10-07-web-test-followup.md`, row 15 disposition and ConfirmDialog coverage.

**Current source:** `develop` at `2dc75771631ee24ea8365740d37c478c02f9995b`; authored application version `0.1.0-dev.93`.

**Decision records:** The player dialog repair preserves the accepted focus/dismissal behavior and does not change product promises, ADR-0035's React-owned selection boundary, or ADR-0046's pointer-only map decision. No ADR or feature-matrix change is expected; recheck the actual diff before publication.

**Execution Strategy:** `executing-plans` because the modal component change, unit tests, browser proof and successor-roadmap update are one sequential contract with shared implementation context; inline execution avoids repeated handoffs, while a second implementer would not own an independent slice. This preserves the user's explicit per-plan lane.

## Global Constraints

- Keep modal content, player copy and existing styled-component ownership; do not add visible product-style chrome or a UI library.
- Preserve Game Settings and Start Over as player-facing controls; closing or canceling a confirmation must not archive the playthrough.
- While archive confirmation is busy, Escape, backdrop clicks and cancel controls must not dismiss it or the parent dialog.
- Do not change world-map input, add keyboard town selection, or modify developer overlay behavior in this slice.
- Advance the sole authored application version from `0.1.0-dev.93` to `0.1.0-dev.94` in this PR.
- Retire the completed setup-transport plan in this successor PR and keep this plan through its completing PR.

## Review Focus

- Escape on the nested confirmation must close only that confirmation; the settings dialog stays open and focus returns to its Start Over control.
- The parent and child dialogs must each contain keyboard focus, and closing the final dialog must return focus to the Game Settings opener.
- A busy archive confirmation must remain open on Escape and backdrop interaction, without issuing a second archive request.

## Rulings

- Ruling: Keep native modal focus and Escape-stack assertions in the real-browser proof; the configured JSDOM environment has no `HTMLDialogElement.showModal()` or `.close()`. Browser evidence showed that removing an open dialog before calling `close()` loses its invoking focus target, so React retains the dialog node and closes it while connected. Component tests assert cancel-event handling and parent-state preservation, while Chromium proves focus containment and restoration; cost if wrong: the browser proof remains the only oracle for native focus behavior, so it exercises both nested and standalone closure.
- Ruling: Give each retained Cockpit dialog instance unique accessible-name IDs; keeping closed roots mounted otherwise lets a hidden instance capture the shared static title ID; cost if wrong: open dialogs can be announced with another overlay's title.
- Ruling: Retain a shared Tab-boundary handler on the active dialog; Chromium left focus on `<body>` when Tab or Shift+Tab crossed either edge of `showModal()` content, while the modal/inert stack still handled Escape, background blocking, initial focus and focus return; cost if wrong: a custom boundary guard can duplicate browser handling, so tests stay limited to the first and last focusable controls.
- Ruling: Focus the primary confirmation control immediately after opening; React consumes `autoFocus` without emitting a native attribute, and because the dialog root remains mounted while closed, the browser opened on the first action (`Keep riding`) instead of preserving the component's previous primary-action focus; cost if wrong: focusing the destructive action can make an accidental activation more likely, so keep the explicit, non-destructive cancel action next to it and verify both initial focus and cancellation in the browser.

---

### Task 1: Add behavior tests for nested player-dialog dismissal

**Files:**
- Modify: `src/WildBunch.Web/src/tests/ConfirmDialog.test.tsx`
- Modify: `src/WildBunch.Web/src/tests/GameSettingsOverlay.test.tsx`

**Interfaces:**
- Consumes: `CockpitOverlayFrame`, `ConfirmDialog`, the Game Settings opener and the Start Over confirmation callbacks.
- Produces: focused component tests for user cancellation, busy refusal, topmost-only dismissal and focus return at the composed player surface.

- [x] Add a ConfirmDialog test that dispatches a cancel request and proves an idle confirmation requests cancellation while a busy confirmation prevents cancellation; do not assert implementation-only listener counts or modal markup inventories.
- [x] Add a composed Game Settings test that opens Start Over, dispatches the confirmation's native cancel request, keeps Game Settings open, and proves no archive call occurs; the browser proof owns focus return because JSDOM does not implement native modal focus behavior.
- [x] Retain meaningful click-cancel and busy negatives, removing only custom-keyboard tests whose behavior will move to the native browser dialog contract.
- [x] Run `npm --prefix src/WildBunch.Web test -- src/tests/ConfirmDialog.test.tsx src/tests/GameSettingsOverlay.test.tsx` and record the intended RED before implementation.

### Task 2: Move both player modals onto the browser modal stack

**Files:**
- Modify: `src/WildBunch.Web/src/components/CockpitOverlayFrame.tsx`
- Modify: `src/WildBunch.Web/src/components/ConfirmDialog.tsx`
- Modify: `src/WildBunch.Web/src/tests/ConfirmDialog.test.tsx`
- Modify: `src/WildBunch.Web/src/tests/GameSettingsOverlay.test.tsx`

**Interfaces:**
- Consumes: the tested player-modal callbacks and open/busy state.
- Produces: styled native dialogs whose open state follows React and whose Escape, focus and inert behavior follows the browser's modal stack.

- [x] Convert each player modal's styled root to a native `<dialog>` opened with `showModal()` and closed while its node remains connected when React withdraws `open`; keep closed roots visually hidden and preserve existing labels, content, backdrop appearance and component-owned styling.
- [x] Handle the browser `cancel` request through the current React callback; prevent default while archive is busy and prevent unmanaged native closure from diverging from React state.
- [x] Preserve backdrop dismissal only for an idle topmost dialog, retain the busy guard for button cancellation, and use one shared first/last Tab-boundary helper because browser proof showed native modal focus does not wrap here; remove redundant global Escape listeners and custom initial-focus/restoration effects.
- [x] Place initial focus on the Close control and primary confirmation action immediately after `showModal()`; run this open-transition handoff only when `open` changes, never when callbacks or `busy` change.
- [x] Run the focused tests and the relevant Web test target; confirm busy refusal, child-only cancellation and return-to-parent behavior.

### Task 3: Verify nested-dialog keyboard behavior in a real browser

**Files:**
- No tracked evidence files; use a temporary Playwright script and screenshots in the branch-scoped scratch workspace.

**Interfaces:**
- Consumes: the worktree Vite app, API, shared PostgreSQL service and a disposable active local playthrough.
- Produces: browser evidence for topmost Escape handling, focus containment/restoration, responsive dialog layout and absence of archive side effects on cancellation.

- [x] Ensure PostgreSQL with `.\tools\postgres-dev.ps1 ensure`; start only this worktree's API and Vite servers with `.\scripts\dev-servers.ps1 ensure` and verify their URLs and checkout identity with `.\scripts\dev-servers.ps1 status`.
- [x] Open the matching app in a clean Chromium context and resume an active local playthrough; if none exists, create one through the normal UI flow and retain it as disposable local test state.
- [x] Open Game Settings, then Start Over; prove Tab and Shift+Tab wrap within the confirmation, Escape closes only it, Game Settings remains open, and focus returns to Start Over; then prove Tab wraps in Game Settings and closing it returns focus to its opener.
- [x] Reopen and cancel the confirmation, then close Game Settings; prove focus returns to the Game Settings opener and no archive POST occurred.
- [x] Repeat the open-and-cancel flow at a narrow mobile viewport, capture desktop and mobile screenshots, check console/page errors, then stop only this worktree's servers with `.\scripts\dev-servers.ps1 stop`.

### Task 4: Retire the predecessor plan and publish the successor slice

**Files:**
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Delete: `.agents/plans/2026-10-11-setup-request-transport.md`
- Keep: `.agents/plans/2026-10-11-player-dialog-focus.md` through this PR.
- Modify: `Directory.Build.props`

**Interfaces:**
- Consumes: verified merged row-14 PRs #277 and #278, the completed browser proof, and the row-15 modal-focus work.
- Produces: a roadmap that records row 14 complete and row 15 active at `0.1.0-dev.94`, plus a reviewable develop PR.

- [x] Record row 14 as complete with PR #277 merge `77592f74bf6c632d30917657f469d207eff1cd07`, PR #278 merge `2dc75771631ee24ea8365740d37c478c02f9995b`, their successful hosted gates and the passing row-14 review; mark the row-14 plan retired in this successor.
- [x] Set row 15's active JIT plan to this player-dialog slice at `0.1.0-dev.94`; preserve its remaining map/style work as pending rather than claiming the full row complete.
- [x] Confirm the actual change does not alter feature truth or a durable ADR decision; keep `docs/features.md` and the ADR log unchanged if that remains true.
- [x] Inspect and stage only the intended source, tests, plan, roadmap, version and completed predecessor plan retirement.
- [ ] Commit through the check-only pre-commit hook; do not run the full canonical gate immediately after a successful hooked commit.
- [ ] Request a fresh whole-branch review, resolve findings, push, open a non-draft PR to `develop`, and verify its exact head and successful hosted canonical gate.
- [ ] Merge to `develop`, verify the develop-push gate, fast-forward the main checkout, then remove this clean worktree and its matching local/upstream branch refs together and verify each is gone.
