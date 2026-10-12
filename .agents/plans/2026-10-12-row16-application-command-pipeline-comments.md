# Application Command Pipeline Comments Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `executing-plans` to implement this plan task-by-task inline. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Correct the Application command-pipeline comments that assign session loading or setup-phase ownership to nonexistent methods or the wrong layer.

**Architecture:** `GameSessionCommandHandler.ExecuteWithRetryAsync` loads the session, enforces the Application setup guard through `RequiresGameStarted` and `GameSession.IsSetupPhase`, invokes a supplied delegate, and commits produced events. The Domain exposes the phase fact; it does not own this Application rejection.

**Tech Stack:** C#, XML documentation, Markdown audit records.

**Spec:** `.agents/specs/2026-10-07-stable-0.1.0-baseline.md`; source finding AP-10 in `.agents/investigations/stable-0.1.0/2026-10-07-application-layer-investigation.md`.

**Execution Strategy:** `executing-plans` (one tightly coupled source-comment correction with its dated audit disposition).

## Global Constraints

- Change comments only; preserve command execution, guard behavior, retry behavior, event storage and exception behavior.
- Do not add tests for comment text or nonexistent comment-named methods. Existing execution and setup-phase tests remain the behavioral evidence.
- Record AP-10 as resolved without rewriting the original audit-time observation.
- Advance `Directory.Build.props` once to `0.1.0-dev.104` for this PR.
- In the first substantive commit, record PR #288's exact merge and hosted-gate evidence, retire the completed `.103` plan and add this successor in the roadmap.
- Deliver through a PR to `develop`, review the exact proposed diff, require exact-head hosted CI success before merge, then fast-forward the primary checkout and remove the clean worktree before deleting and verifying the matching local and upstream branch refs.

## Review Focus

- Comments describe the current delegate-based handler contract and the actual location of the setup-phase check.
- No executable source, signatures or existing behavior tests change.
- ADR-0028 and `docs/features.md` remain accurate because this corrects implementation narration without changing architecture or product behavior.

---

### Task 1: Commit the JIT plan before execution

**Files:**
- Add: `.agents/plans/2026-10-12-row16-application-command-pipeline-comments.md`

- [x] Commit this plan by itself before changing implementation or successor bookkeeping.

### Task 2: Correct stale command-pipeline ownership comments

**Files:**
- Modify: `src/WildBunch.Application/Games/Execution/GameSessionCommandHandler.cs`
- Modify: `src/WildBunch.Application/Games/Exceptions/SetupPhaseException.cs`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-07-application-layer-investigation.md`
- Modify: `.agents/investigations/stable-0.1.0/2026-10-07-application-test-followup.md`
- Modify: `.agents/roadmaps/2026-10-07-stable-0.1.0-cleanup.md`
- Modify: `Directory.Build.props`
- Delete: `.agents/plans/2026-10-12-row16-turn-result-contract.md`

- [x] Replace the nonexistent `ExecuteAsync` subclass reference and both claims that subclasses load the session with the actual delegate/base-pipeline contract.
- [x] Describe `SetupPhaseException` as an Application rejection raised by the base pipeline when `RequiresGameStarted` is true and the loaded session reports `IsSetupPhase`; do not claim `GameSession.ThrowIfSetupPhase` exists or owns this guard.
- [x] Add dated AP-10 dispositions to the source and test follow-up records. Preserve the audit-time finding and state that no runtime test is needed because behavior is unchanged; retain the real orchestration and setup-guard behavior tests.
- [x] In the first substantive commit, record PR #288 head `14b5dee717d71deb329983b5ab1fe2ae330f8ba8`, merge `a11efd7e46f6bf034f7d148806928c822a8208d6`, and exact-head hosted gate run `38207230748`; note that the `.103` plan is retired here and this `.104` plan is its successor.
- [x] Confirm the source-only comment correction does not require an ADR or feature-matrix change.
- [x] Review `git diff --check` and run the canonical staged-snapshot gate through the normal commit hook. Do not add a test that asserts comments, method names mentioned in prose or source shape.

**Validation:** Baseline full gate passed on clean `develop` before edits. The completing commit must pass the canonical gate; whole-branch review must approve the exact branch range; hosted canonical CI must pass on the exact PR head before merge.
