# Correct World Map Scope and Decision History Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the 0.1.0 world-map contract and ADR history accurately state that town selection is pointer-operated, while preserving the shared map and React-owned legal selection.

**Architecture:** Add a successor decision that narrowly supersedes the keyboard-equivalent map selection requirement for the 0.1.0 baseline. Preserve ADR-0035's durable React/Phaser ownership and ADR-0039's setup/prologue/first-arrival flow; update the feature matrix, baseline spec, roadmap and investigation so none treat the withdrawn map input requirement or already-delivered shared map resource as unfinished work.

**Tech Stack:** Markdown, repository ADR and planning conventions, Python command bus.

**Spec:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`, PG-003 map contract and row 15 display/rendering scope, reconciled to the user's settled decision that this cleanup does not restore the lost keyboard town-selection path.

**Decision records:** Create ADR-0044 as a narrow successor to ADR-0035 and ADR-0039 for 0.1.0 town-selection input scope. Keep both predecessor decisions and their original dates intact; add dated history and successor links rather than rewriting their original choices.

**Execution Strategy:** `executing-plans` with Native inline execution. The ADR, release scope, feature truth and historical investigation must move together to keep one decision coherent; a single task and whole-branch review avoid splitting that decision across commits or PRs.

## Global Constraints

- This documentation correction does not implement keyboard or screen-reader town selection and does not remove keyboard support from other controls.
- One world map, one canonical map read, React-owned selection legality, Phaser-as-renderer/input-adapter and the setup -> prologue -> first arrival flow remain unchanged.
- Preserve unknown historical provenance; do not invent why the previous keyboard path disappeared.
- Advance the sole authored product version once from `0.1.0-dev.80` to `0.1.0-dev.81` in `Directory.Build.props`.
- The first substantive commit retires the completed canonical-session plan and replaces its stale roadmap link; the new plan remains through this PR.
- Each successor plan uses a fresh worktree from refreshed `origin/develop`, updates its delivery evidence, and merges to `develop` only after hosted exact-head CI passes.

## Review Focus

- Historical ADRs must retain their original decision and date while clearly identifying the narrow superseded scope and current successor.
- Removing the map-specific keyboard requirement must not be read as withdrawing keyboard/focus obligations for overlays, forms, or other supported controls.
- The historical A-11 audit and web test follow-up must not leave an active instruction to restore keyboard map selection after ADR-0044 withdraws that baseline requirement.
- The source already has one `getWorldMap` endpoint, one `['world-map', gameId]` query identity in setup and travel, and one shared renderer. Do not create implementation work or preserve stale claims of separate map resources.

---

### Task 1: Record and route the corrected map scope

**Files:**
- Create: `docs/decisions/ADR-0044-pointer-operated-world-map-selection-for-0.1.0.md`
- Modify: `docs/decisions/ADR-0035-react-shell-with-phaser-as-renderer-input-adapter-for-playfield-surfaces.md`
- Modify: `docs/decisions/ADR-0039-seeded-game-setup-and-free-first-arrival.md`
- Modify: `docs/decisions/README.md`
- Modify: `docs/features.md`
- Modify: `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-06-adr-truth-investigation.md`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-07-web-layer-investigation.md`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-07-web-test-followup.md`
- Modify: `Directory.Build.props`

**Interfaces:**
- ADR-0044 defines only the 0.1.0 town-selection input scope; it leaves map truth, eligibility and accepted selection with the existing React/server flow.
- The feature matrix and baseline spec describe current selection input truth without expanding keyboard-accessibility scope.
- WB-14 distinguishes the unshipped input path from the already-shared map resource and renderer.

- [x] **Step 1: Write the successor decision and preserve history.** Add ADR-0044 declaring pointer-operated town selection sufficient for the 0.1.0 baseline and withdrawing only the earlier equivalent keyboard-selection requirement from this baseline. State that the prior path's removal reason is unknown. Add dated successor history and cross-links to ADR-0035 and ADR-0039; retain their original decision prose and dates. Update the catalogue status and summary.

- [x] **Step 2: Align live product and release documents.** Update PG-003, the baseline map contract, row 15 and its acceptance language so pointer town selection is the 0.1.0 scope. Preserve keyboard/focus obligations for non-map controls and the one-map/setup/prologue/first-arrival contract. Verify the successor commit already records PR #265 (`ac73d062` -> `1a021386`, exact-head gate `38119160942`, develop-push gate `38119428612`) in row 13, keeps it executing, and replaces the retired plan link with this JIT plan.

- [x] **Step 3: Correct investigation and test-planning truth.** Keep WB-14's finding that the map has no keyboard town-selection path, but mark that input gap outside the current baseline per ADR-0044. Remove its stale assertion that setup and travel use separate map query keys or duplicate map authorities; record that PRs #245/#250 already established the shared map projection/query and that both surfaces use the same renderer. Add a dated current disposition to ADR audit A-11 while preserving its original evidence, and update the web test follow-up so its map scenario protects shared data and pointer eligibility without requiring keyboard town selection. Correct source-ownership table descriptions that still imply map-resource work remains outstanding.

- [x] **Step 4: Validate and commit.** Advance `Directory.Build.props` once to `0.1.0-dev.81`. Read the changed ADRs, matrix, spec, roadmap and WB-14 together; verify every current link resolves and the old map-scope assertion is no longer presented as live. Run the repository's documentation/contract validation target if available, `git diff --check`, inspect the complete diff, and commit through the check-only pre-commit gate. Do not add a structural test for Markdown sections or rerun expensive CI immediately before the hook.

- [ ] **Step 5: Review and deliver.** Complete readiness review and request a fresh whole-branch review of the final committed revision. Resolve any Critical or Important findings with evidence. Open a PR to `develop`, verify the reviewed head and hosted exact-head canonical gate, merge after it passes, verify the develop-push gate, and fast-forward the main checkout. Leave this new plan and the parent roadmap/spec live for the next substantive successor to assess.

## Exit Criteria

- ADR-0035 and ADR-0039 retain their historical decisions while pointing to the narrow 0.1.0 map input successor.
- The feature matrix, spec, roadmap and investigation consistently describe one shared world map with pointer town selection in this baseline.
- Keyboard/focus requirements for non-map controls remain in scope, and no map accessibility implementation is added.
- The stale separate-map-resource claim is corrected against current code and merged delivery evidence.
- The PR advances the sole product version to `.81`; its reviewed exact head and develop-push gate pass before the PR's worktree and branch are retired.
