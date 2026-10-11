# Keep the existing pointer-operated town map in 0.1.0

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task.

**Goal:** Make the stable 0.1.0 baseline and ADR log accurately state that the existing shared world map remains pointer-operated; keyboard and screen-reader town selection is not added to this release.

**Architecture:** Starting-town choice and later travel already use one `WorldMapScene` and `PhaserMapHost`, with interaction-specific eligible-town rules. The map has pointer selection only. Do not add another map, town selector, state owner, or interaction path. Preserve ADR-0045 as historical evidence and supersede it with a dated decision that reflects the user's release-scope decision.

**Specification:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`, PG-003 and Workstream E.

**Investigation:** `.agents/investigations/stable-0.1.0/2026-10-07-web-layer-investigation.md`, WB-14; `.agents/investigations/stable-0.1.0/2026-10-07-web-test-followup.md`.

**Decision records:** Add ADR-0046 to supersede ADR-0045. Keep ADR-0044's historical status and ADR-0045's original dated assertion intact. Update the authored ADR catalogue.

**Execution Strategy:** Documentation-only scope correction with implementation evidence already inspected. No source or test changes. An independent whole-branch review checks historical truth, current feature claims, routing, and successor retirement.

## Global Constraints

- State the actual feature boundary: one shared map, pointer-operated town selection, different legal town sets for starting choice and later travel.
- Do not claim that town-hub keyboard building navigation is map town selection.
- Do not add keyboard/screen-reader town controls or change map implementation in this slice.
- Keep ADR-0045 and ADR-0044 historically accurate; use ADR-0046 for the current decision.
- Reconcile WB-14 as an explicit 0.1.0 disposition and update the test follow-up without adding a checker that freezes documentation shape.
- Advance the single authoritative product version in `Directory.Build.props` from `0.1.0-dev.85` to `0.1.0-dev.86`.
- Retire the completed travel-preview plan and record PR #270's reviewed source, merge, identical tree and both hosted gates in the roadmap.
- Deliver this plan in a fresh worktree from refreshed `origin/develop`, through a PR to `develop`, exact-head hosted gate, merge, develop-push gate, primary-checkout fast-forward and verified cleanup.

## Review Focus

- The feature matrix, baseline specification, ADR catalogue and roadmap agree that town selection remains pointer-operated for 0.1.0.
- ADR-0045 remains a true record of the decision made on 2026-10-11 and is marked superseded by ADR-0046.
- Row 15 no longer promises a new keyboard/screen-reader map selector, while preserving required accessibility work for existing controls and overlays.
- The roadmap retains its one-map architecture and does not describe start and travel as separate map implementations.
- WB-14's unresolved implementation proposal is replaced with a dated disposition; its historical observation remains understandable.

---

### Task 1: Record the current map scope and retire the stale requirement

**Files:**
- Add: `docs/decisions/ADR-0046-pointer-operated-town-selection-for-0.1.0.md`
- Modify: `docs/decisions/README.md`
- Modify: `docs/features.md`
- Modify: `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-07-web-layer-investigation.md`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-07-web-test-followup.md`
- Modify: `Directory.Build.props`
- Delete: `.agents/plans/2026-10-11-travel-preview-resource-identity.md`

- [ ] Add ADR-0046 with dated history, decision, rationale and consequences. Record that map-level keyboard/screen-reader town selection is absent, so adding it is new 0.1.0 work; preserve one shared pointer-operated map for this release.
- [ ] Mark ADR-0045 superseded by ADR-0046 in its status/successor section and catalogue, without rewriting the decision it records.
- [ ] Update PG-003's feature matrix, baseline contract, Workstream E, roadmap row 15 and detailed row-15 scope to remove the keyboard/screen-reader town-selection requirement and cite ADR-0046.
- [ ] Keep accessibility expectations for existing supported controls, overlays and building navigation intact; remove only the unbuilt map-level town selector requirement.
- [ ] Add a dated WB-14 disposition and test-follow-up note. Record why no map-selection test or implementation is added for this baseline; retain actual pointer-selection tests and map behavior obligations.
- [ ] Update row 13 with PR #270 reviewed source `55ccfa21fd59be376bd6d97dbc9cfc5c2047d0f1`, merge `8bfb26e6975c25e19fda37749bf56b4d5584a4db`, identical source/merge tree `35a9b37835f22ca4de609e362f626a3a0319e21b`, exact-head gate `38131228340`, develop-push gate `38131620570`, and retire the completed travel-preview plan.
- [ ] Advance `Directory.Build.props` from `0.1.0-dev.85` to `0.1.0-dev.86`.
- [ ] Run the command-bus documentation checks and full `py -3 tools/run.py ci --check` gate on the committed implementation state.
- [ ] Request an independent whole-branch review; resolve findings and re-review any changed diff.
- [ ] Open a Draft PR targeting `develop`, verify body and exact head, mark ready, and merge after the exact-head hosted canonical gate passes.
- [ ] In the successor plan, record this PR's reviewed source, merge/tree and both hosted gates, then fast-forward the primary checkout and clean only the verified merged worktree and branch.

## Out of Scope

- Implementing map-level keyboard or screen-reader town selection.
- Changing world-map data, renderer ownership, town eligibility, first arrival, travel legality or pointer behavior.
- Removing keyboard support from town-building navigation or other existing controls.
- Beginning the remaining row-13 React ownership work or row-15 renderer repairs.
