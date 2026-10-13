# Row 17 N-14 Paired Worktree and Branch Retirement Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task inline under the authorized roadmap goal.

**Goal:** Record N-13's verified closeout and make the stable 0.1.0 spec and roadmap explicit that a plan's clean worktree and its matching local and upstream branches are retired as one verified unit.

**Architecture:** The spec owns the durable delivery invariant; the roadmap owns the ordered closeout procedure and per-plan evidence. A merged PR does not itself remove local state. Verify the merged PR identities and green gates, remove the clean worktree, delete only the exact paired refs, and verify all absences before starting the next worktree. Retain unrelated, active, unmerged or uncertain refs. Preserve remaining row 17 test dispositions as active work.

**Tech Stack:** Markdown planning artifacts, Git, GitHub PR and Actions, Wild Bunch command bus.

**Spec:** [Stable 0.1.0 baseline](../specs/2026-10-07-stable-0.1.0-baseline.md), delivery decision and artifact custody; [cleanup roadmap](../roadmaps/2026-10-07-stable-0.1.0-cleanup.md), develop integration and per-plan delivery.

**Execution Strategy:** `executing-plans` - this is one tightly coupled documentation and delivery-state correction; one inline executor can reconcile its evidence, update the two authoritative planning artifacts, and validate their consistency without task handoffs.

## Global Constraints

- Base: `origin/develop` at `41eb3dcba717f252ede34236459565f0003b1f6a`, the merged N-13 PR #313 result.
- Deliver one PR to `develop`; advance `Directory.Build.props` once to `0.1.0-dev.129` for this merged PR.
- Retire `.agents/plans/2026-10-13-row17-n13-hidden-truth-read-proof.md` in the first substantive commit after confirming its full scope shipped in PR #313. Keep this N-14 plan through its completing PR.
- Use only the user-authorized N-13 branch, worktree and PR evidence. Do not delete or sweep any unrelated scratch, branch, or worktree.
- Keep row 17 incomplete; this plan only records the completed N-13 delivery and clarifies closeout requirements for remaining plans.
- Do not change product behavior, test behavior, ADRs or feature-matrix promises.
- Require source review, exact-head hosted CI and develop-push CI before merge. After merge, verify the primary checkout is current and remove the clean worktree before its exact local and upstream branch refs.

## Review Focus

- The spec and roadmap state one consistent cleanup order and distinguish this plan's dedicated refs from all other branches.
- The N-13 record is bound to PR #313's exact head, merge result and successful gates; it does not present its separately retained scratch evidence as cleaned.
- Remaining row 17 work is still marked active and N-13's plan is retired only because every planned task shipped.

---

### Task 1: Record the paired retirement contract and N-13 closeout

**Files:**
- Modify: `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `Directory.Build.props`
- Delete: `.agents/plans/2026-10-13-row17-n13-hidden-truth-read-proof.md`
- Retain: `.agents/plans/2026-10-13-row17-n14-paired-retirement.md`

**Interfaces:**
- Consumes: PR #313 merged head `4d08ec5cd930ddccf9e5e6ed8d89ded333dc67fa`, merge `41eb3dcba717f252ede34236459565f0003b1f6a`, tree `5ae35d9a08f9df4960fefe2436a56713443abf4c`, exact-head hosted gate run `38328584853`, develop-push gate run `38329081941`, and observed paired cleanup.
- Produces: clear spec wording that worktree plus exact local/upstream refs are one closeout; roadmap's exact verify-remove-delete-verify sequence and N-13 delivery evidence; version `.129`; and retirement of the fully delivered N-13 plan.

- [x] **Step 1: Reconcile the durable wording.** In the spec's delivery decision, explicitly state that worktree and branch refs are one temporary execution unit and closeout is incomplete until the clean worktree and both exact refs are absent. In the roadmap, retain the ordered process: verify merged PR/head/tree and green gates, remove the clean worktree, delete only its paired refs, verify all absences, then start the next plan worktree. State that unknown or unrelated refs are preserved.

- [x] **Step 2: Record N-13 and preserve remaining row 17 scope.** Update row 17 with PR #313, source/merge/tree identities, exact-head run `38328584853`, develop-push run `38329081941`, and verified removal of worktree `codex/row17-n13-secrecy-projection-proof` followed by deletion and absence checks for its local and upstream refs. Mark N-13 complete, N-14 as the active successor, and row 17 still in progress. Do not claim its separate branch-scoped scratch folder was removed.

- [x] **Step 3: Retire N-13 and advance the shared version.** Change `Directory.Build.props` to `0.1.0-dev.129`, delete the completed N-13 plan, retain N-14, and commit the intended planning/version changes through the check-only hook.

- [ ] **Step 4: Verify the committed artifacts.** Read the revised spec, roadmap and active plan; check that closeout sequencing and evidence agree and that the N-13 plan is absent while N-14 remains. Record the successful staged-candidate hook gate against the exact resulting tree and inspect the commit contents; do not rerun the canonical gate immediately after the successful hook. No behavioral test is added for documentation-only wording.

- [ ] **Step 5: Review, publish and close out.** Review the full branch and report why no ADR or feature-matrix change is warranted. Open one PR against `develop`; require passing exact-head hosted CI and develop-push CI. Fast-forward the primary checkout, remove this clean worktree before deleting its exact local/upstream refs, and verify all three are absent.
