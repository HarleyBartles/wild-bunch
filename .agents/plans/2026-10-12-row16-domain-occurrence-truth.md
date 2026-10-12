# Domain Event Occurrence Truth Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task inline. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Close the stale DN-13 Domain investigation finding by recording that event occurrence time belongs to persisted envelopes and that PR #240 already removed volatile Domain event getters.

**Architecture:** Keep Domain events limited to established facts and keep occurrence metadata in the Persistence envelope, as implemented by PR #240 and the existing Application audit read path. Preserve DN-13's original finding as historical evidence, then add a dated correction and update only its three per-file disposition references.

**Tech Stack:** Markdown, Git, repository command bus.

**Spec:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`; row 16 in `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`; DN-13 in `.agents/investigations/stable-0.1.0/2026-10-07-domain-layer-investigation.md`.

**Execution Strategy:** `executing-plans` inline. This is one tightly coupled audit correction across the finding and its file inventory, followed by the required successor-plan, version and roadmap bookkeeping; splitting these edits would add handoffs without independent behavior proof.

## Global Constraints

- Do not change Domain event contracts, event payloads, Persistence envelopes, upcasters, Application audit projection behavior, or runtime code.
- Keep DN-13's original finding and uncertainty as historical evidence; add a dated correction instead of rewriting it as if the original investigation had observed today's source.
- Do not add tests for comments, headings, or document structure; existing PR #240 behavior and persistence tests remain the evidence for runtime behavior.
- Advance `Directory.Build.props` exactly once to `0.1.0-dev.108` and retire the `.107` plan only in this successor PR.
- Record PR #292 reviewed source `1b8e5b3cd3a5f13f7fb4943c669c556dc9a6d114`, merge `3ae3dafd66f45b7cbb02efe8b64ba155eb5054be`, successful exact-head gate `38219319640`, successful develop push gate `38219882093`, clean review, and verified paired worktree/local/upstream ref cleanup in the roadmap.
- Deliver by reviewed PR to `develop`; verify exact head and hosted gate before merge. After merge, fast-forward the primary checkout, remove this worktree, delete its paired local/upstream branch refs, and verify all three absences before the next plan.

## Review Focus

Verify the three event declarations no longer expose computed occurrence timestamps, PR #240's upcasters preserve historical event facts while removing payload timestamps, the Persistence envelope retains stored timestamps, and the Domain audit correction does not imply runtime work in this slice.

---

### Task 1: Commit the JIT plan before implementation

**Files:**
- Add: `.agents/plans/2026-10-12-row16-domain-occurrence-truth.md`

- [ ] Commit this plan by itself before changing the Domain investigation, roadmap, or version.

### Task 2: Date and close the DN-13 source finding

**Files:**
- Modify: `.agents/investigations/stable-0.1.0/2026-10-07-domain-layer-investigation.md`

- [x] Preserve the original DN-13 finding text and add a dated disposition citing PR #240, source commit `be2af352007424662e712c006e32eeac63e87f3b`, and merge commit `d692b8f6bc617f9c40a02b958c6cdabf8a0c011e`.
- [x] State that `CaseFileGenerated`, `WorldGenerated`, and `StartingTownSelected` no longer expose computed `OccurredAt` getters; persisted event envelopes own occurrence time, and the existing Application audit path reads that stored metadata.
- [x] State that PR #240 preserved historical event facts through registered upcasters without rewriting stored rows or adding a migration; cite the existing dated Persistence and test follow-ups rather than duplicating their detailed evidence.
- [x] Correct only the three per-file disposition entries for these events so they no longer imply DN-13 remains open. Keep unrelated DN-07, DN-11 and DN-16 references intact.
- [x] Do not change the Application investigation, which already contains a dated AP-14 disposition, or revise an ADR or feature matrix.

### Task 3: Retire `.107` and route the `.108` successor

**Files:**
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `Directory.Build.props`
- Delete: `.agents/plans/2026-10-12-row16-persistence-truth.md`

- [x] Record PR #292's reviewed source, merge, passing exact-head and develop-push gates, clean whole-branch review, and verified worktree/local/upstream ref cleanup; route this `.108` plan as the active row-16 slice.
- [x] Advance the product version from `.107` to `.108` exactly once and retire the `.107` plan in this successor.
- [x] Review the dated Domain disposition against the live event declarations and existing PR #240 persistence/upcaster evidence, run `git diff --check`, then commit through the normal check-only hook and complete canonical gate.

**Validation:** This documentation-only correction does not alter behavior, so no new test is justified. The staged-snapshot hook provides the canonical repository gate for the final committed tree; confirm the event declarations, upcasters, envelope mapping, and existing behavior tests support the historical disposition before committing.
