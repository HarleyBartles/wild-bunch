---
name: git-github-worker-operations
description: Govern worker-side git and GitHub operations with dirty-tree hygiene, issue-comment rules, gitlink awareness, and publication handoff discipline.
---

# git-github-worker-operations

## Purpose

Provide the canonical workspace-wide skill for worker-side git and GitHub operations so workers do not rediscover status checks, dirty-tree triage, issue-comment rules, submodule/gitlink handling, or publication handoff rules on every dispatch.

## When To Use

- You need to inspect or describe repository state before mutating anything.
- You need to classify dirty files and decide what is safe to stage, commit, ignore, clean, or escalate.
- You need to write or review commit messages without accidental issue closure keywords.
- You need to add an issue comment when the dispatch authorizes it.
- You need to verify `origin/main`, local heads, or submodule/gitlink topology before publication.
- You need to verify review feedback against source reality before acting on it.
- You need to challenge vague validation claims or route validation-selection decisions to the right capability.
- You need to decide whether workspace preflight/orchestration belongs in `will-mainline-stack-publication`.
- You need to verify issue-backed worker returns or closure posture against observable repo/workspace state before accepting GREEN.

## When Not To Use

- You are looking for generic Git training.
- You are trying to replace the publication helper.
- You are trying to close issues as a worker.
- You are handling archive source evidence, ProjectDB custody, canon, or manuscript content directly.

## Lanes

- `status-and-dirty-tree-triage` - inspect repository state and classify dirt.
- `diff-and-path-review` - review scoped changes before staging or commit.
- `commit-message-hygiene` - keep commit messages clean and non-closing.
- `issue-commenting` - add issue comments when authorized.
- `issue-closure-gate` - report closure posture without closing issues and require issue-goal conformance before accepting GREEN.
- `origin-main-verification` - verify `origin/main` and current heads.
- `submodule-gitlink-awareness` - preserve child-first publication and keep parent pointer hygiene out of domain-worker completion.
- `review-feedback-intake` - verify review feedback against current source reality before acting, pushing back, or routing.
- `validation-selection-handoff` - point validation-class questions at the validation-selection skill instead of turning them into publication claims.
- `workspace-preflight-handoff` - route Will/Chris orchestration through `will-mainline-stack-publication` when the task boundary requires it.
- `amber-repair-cleanup` - classify blockers and residue honestly.

## Required Reads

- `Doctrine/Governance/GITHUB_ISSUE_WORKFLOW_POLICY.md`
- `Doctrine/Governance/GITHUB_ISSUE_BODY_QUALITY_STANDARD.md`
- `Doctrine/Governance/REVIEW_FEEDBACK_VERIFICATION_POLICY.md`
- `../../Doctrine/Contracts/ACTOR_BOUND_WORKER_RETURN_CONTRACT.md`
- `Doctrine/Governance/VALIDATION_SELECTION_POLICY.md`
- `Skills/validation-selection/SKILL.md`
- `Skills/will-mainline-stack-publication/SKILL.md`
- `Skills/will-mainline-stack-publication/scripts/publish_mainline_stack.py`
- `Skills/review-feedback-verification/SKILL.md`
- `Rooms-Mostly/Reference/GPT_RUNTIME_SYSTEM_PROMPT_ROOMS_MOSTLY.md`
- `Rooms-Mostly/Chris/Doctrine/CHRIS_OPERATING_DOCTRINE.md`

## Concise Git Checklist

- Run `git status --short` first.
- Run `git diff --check` before committing anything.
- Use `git diff` or `git diff --cached` to review the exact scope you intend to touch.
- Use `git log --oneline -n 5` or `git show --stat` when you need recent history or a commit inspection.
- Use `git submodule status --recursive` before publishing any stack that includes gitlinks.
- Fetch and compare `origin/main` before claiming publication or clean mainline alignment.

## Dirty-Tree Classification

Classify every dirty path before you act:

- `intended` - belongs to the authorized task and may be staged or published.
- `unrelated` - belongs to other user work and must be left alone unless explicitly authorized.
- `accidental` - task-adjacent noise that should be removed or corrected before GREEN.
- `residual` - leftover temp, smoke, proof, or generated residue that should be kicked if safely in scope.
- `blocker` - protected, ambiguous, or out-of-scope dirt that prevents a safe green claim.

Do not hide unrelated dirt by omitting it from the report. Excluding it from a publication scope is not the same thing as resolving it.

## Commit Message Hygiene

- Keep commit messages specific to the authorized change.
- Prefer `Refs #123` or `Addresses #123 pending verification` when the issue is only being referenced.
- Do not use `fixes`, `closes`, or `resolves` unless verifier-owned closure is intentionally being triggered.
- Do not let a commit body accidentally auto-close an issue when the worker is only reporting progress.

## Issue Commenting And Closure Gate

- Workers may add issue comments when the dispatch authorizes it.
- Workers may report closure posture, progress, or residual risks.
- Workers must not close issues independently.
- Worker GREEN does not equal issue closure.
- GPT or another verifier owns closure decisions with evidence and Harley support or context.

## Origin Main Verification

- Verify the local branch head with `git rev-parse HEAD`.
- Verify the tracked remote head with `git rev-parse origin/main`.
- Fetch `origin/main` before claiming the remote is current.
- Compare the expected head to the fetched remote head after publication.
- Treat a mismatch as a publication or sync problem, not as a clean green result.

## Worker GREEN Falsification

Before accepting a worker `GREEN`, verify the claim against boring falsifiers instead of verifying only that the worker wrote a plausible report.

Partition the judgment when needed:

- `implementation_goal`
- `validation_report`
- `publication`
- `cleanup`
- `issue_goal_conformance`
- `overall_closeout`

For worker returns that include local/manual/browser validation, local dev servers, test watchers, containers, or any run touching a local workspace such as `C:/WORK/**`, include a cleanup falsification lane. Ask whether the return itself proves cleanup of worker-owned helpers and repo/file-lock risk. Missing or incomplete cleanup proof makes the cleanup lane `AMBER` or `RED`, even when implementation and issue-goal conformance are `GREEN`.

A sufficient cleanup proof names the helpers started, helpers stopped, process and command-line evidence for remaining relevant helpers, every port used during validation, browser/session cleanup where relevant, and repo/file-lock posture when local mirrors/backups could be affected. Checking only default ports, omitting alternate dev/preview ports, or saying "no remaining processes" without a post-cleanup scan is not enough.

If cleanup proof is missing, report `implementation_goal: GREEN` only if proven, but keep `overall_closeout` non-GREEN until cleanup is repaired or explicitly accounted for. A later user finding a worker-owned helper from the validation run after `GREEN` falsifies the cleanup lane.

## Submodule And Gitlink Awareness

- Treat child repos as child-first publication surfaces.
- Publish child content before any boundary-owned pointer sync.
- Do not bump a parent pointer if it already matches the published child head.
- Verify wrapper and workspace pointer chain state when the task crosses repo boundaries and the boundary owner is publishing that repo.
- Do not assume a gitlink changed just because a child repo has a newer local commit.

## Publication Hand-off

- Use `Skills/will-mainline-stack-publication/SKILL.md` for Will/Chris preflight publication orchestration.
- Do not bypass the helper with raw git push reasoning when the orchestration layer owns the publication boundary.
- Use this skill to triage state, scope the change, and verify the lane before handoff.
- Leave actual stack publication to the helper unless the helper is concretely unfit and that blocker is recorded.

## Validation

- Run `git status --short` and review the dirty classification.
- Run `git diff --check` on the intended change set.
- Verify `origin/main` when publication or clean-head claims are involved.
- Inspect `git submodule status --recursive` when gitlinks might move.
- Confirm issue comments are authorized and closure is not being claimed by the worker.
- Before accepting a worker return, restate the issue goal as observable repo/workspace state, inspect the surfaces that would falsify it, compare the worker claim to observed state, and only then judge GREEN/AMBER/RED.
- For local/manual/browser validation returns, run the cleanup falsification lane before accepting overall GREEN.
- Route validation-class questions to `validation-selection` instead of treating a selected class as execution proof.
- Route review-feedback questions to `review-feedback-verification` instead of treating comments as orders or closure evidence.

## False-Green Risks

- Publishing unrelated dirt.
- Treating a worker comment as issue closure.
- Treating review comments as orders or closure evidence.
- Letting a commit message close an issue unintentionally.
- Forgetting child-first publication when the boundary owner is publishing that repo.
- Bypassing the publication helper for orchestration-owned publication.
- Claiming green while dirty residue, pointer mismatch, or remote mismatch remains.
- Claiming green while observable repo/workspace state still contradicts the issue goal.
- Claiming overall green while worker-owned validation helpers, dev servers, browser kernels, watchers, or repo-lock risks remain unproved or unaccounted for.
- Treating remote-head equality, changed-file lists, or commit messages as sufficient without issue-goal conformance.
- Treating a validation class or coverage matrix as proof that checks ran.

## Shared Support Dependencies

- `Doctrine/Governance/GITHUB_ISSUE_WORKFLOW_POLICY.md`
- `Doctrine/Governance/GITHUB_ISSUE_BODY_QUALITY_STANDARD.md`
- `Skills/will-mainline-stack-publication/SKILL.md`
- `Skills/will-mainline-stack-publication/scripts/publish_mainline_stack.py`

## Decommission State

Canonical capability skill bundle. Keep it discoverable as the workspace-wide worker git and GitHub operations surface until a governed replacement exists.

## Bundle

- `SKILL.md`
- `agents/openai.yaml`
