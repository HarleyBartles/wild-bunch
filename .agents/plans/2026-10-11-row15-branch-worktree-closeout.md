# Row 15 Branch and Worktree Closeout Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the stable 0.1.0 delivery contract treat each merged plan worktree and its corresponding local and upstream branch refs as one closeout unit, and record the verified PR #279 outcome.

**Architecture:** The specification owns the durable paired-resource rule; the roadmap owns its verification and deletion sequence and the chronological row-15 delivery record. This documentation-only slice changes no application behavior and adds no tests; the existing whole-repository gate verifies the shared version identity and repository state.

**Tech Stack:** Markdown, MSBuild version property, Wild Bunch command bus.

**Spec:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`, delivery decision; `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`, develop integration and per-plan delivery.

**Execution Strategy:** `executing-plans` because the spec, roadmap, successor-artifact retirement and one shared version increment form a single sequential documentation change.

## Global Constraints

- The epic integrates through PRs to `develop`; each ordinary merged PR advances `Directory.Build.props` once.
- A merged plan's worktree and matching local/upstream branch refs are one temporary execution unit.
- Preserve branches when merge identity, worktree cleanliness or ownership is uncertain.
- The current epic specification and roadmap remain authoritative until the epic is complete.

## Review Focus

- The squash-merged plan branch is not required to be an ancestor of `develop`; verify the exact PR head and merge identity instead.
- Do not retire a plan until its merged output and validation record are represented by the successor.

---

## Tasks

### Task 1: Record paired worktree and branch closeout

**Files:**
- Modify: `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Delete: `.agents/plans/2026-10-11-player-dialog-focus.md`
- Create: `.agents/plans/2026-10-11-row15-branch-worktree-closeout.md`
- Modify: `Directory.Build.props`

- [x] Update the specification's Gitflow delivery decision to state that worktree plus matching local and upstream refs form one temporary plan unit and that closeout is incomplete while any remains.
- [x] Make the roadmap require exact merged-head and develop verification, clean-worktree verification, worktree removal first, then local/upstream ref deletion and verification; account for hosting services that auto-delete upstream refs.
- [x] Record PR #279's reviewed head, merge commit, exact-head and develop-push gates, and verified removal of its worktree and both branch refs in row 15.
- [x] Retire the completed `.94` dialog-focus plan through this successor while retaining this plan through its completing PR.
- [x] Advance the sole authored application version from `0.1.0-dev.94` to `0.1.0-dev.95`.
- [x] Run the complete fail-fast repository gate and inspect the final diff.
- [x] Commit the complete slice for PR publication to `develop`.

**Validation:** `py -3 tools/run.py ci --check`; `git diff --check`; verify only the intended version authority is authored and the new roadmap links resolve.
