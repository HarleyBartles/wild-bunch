---
name: executing-plans
description: Use when executing an approved written plan inline, especially when tasks are sequential or tightly coupled.
metadata:
  source-id: executing-plans
  source-path: codex-marketplace/plugins/superpowers-plus/skills/executing-plans/SKILL.md
  provenance-name: Executing Plans first-party skill
  source-category: first_party
  status: active
  owner: Harley Bartles
  use_when:
    - a written implementation plan exists and the current session will implement it.
    - tasks are sequential or tightly coupled and benefit from one integration context.
    - per-task TDD plus one fresh whole-branch review is the chosen execution cost.
  do_not_use_when:
    - no approved plan exists.
    - independent tasks warrant fresh implementer and reviewer contexts per task; use subagent-driven-development.
  related_skills:
    - handoff-gates
    - writing-plans
    - subagent-driven-development
    - finishing-a-development-branch
    - requesting-code-review
license: MIT
---

## Provenance

This marketplace-maintained derivative is based on `obra/superpowers` v6.4.1 commit `5bf4e78011075bcfc0dc295f0724994cd123ee71` under the MIT License. Upstream source is not vendored; this directory contains the maintained Superpowers+ implementation.

# Executing Plans

Execute the plan yourself, task by task, in this session. Native inline execution uses no implementer or reviewer per task; it buys one fresh whole-branch review at the end.

The brief is the task contract, the off-repo ledger survives compaction, TDD is the per-task gate, and the final reviewer supplies the independent context.

**Announce at start:** "I'm using the executing-plans skill to implement this plan with Native inline execution."

**Continuous execution:** Do not pause between tasks. Stop only for a human-owned requirement or product/canon/privacy/licensing decision, missing authority for an irreversible or externally consequential action, a security-sensitive action requiring approval, or a plan defect that leaves every plausible path as a guess.

**Rulings, not stalls:** Resolve falsifiable technical conflicts from the spec, plan, repository evidence, and tests. Record each decision as `Ruling: <what> — <why> — <cost if wrong>` before continuing.

## Setup

1. Read `references/implementation-baseline.md` and the repository's `.agents/runbooks/implementing.md`.

2. Verify the existing linked worktree with `using-git-worktrees`; never begin implementation on `main` or `master` without explicit authority.

3. Read the committed plan and its spec. On resume, read the committed in-flight checkpoint before live repository inspection, then reconcile its claims against Git.

4. Read `references/execution-lane-override.md`. Human direction wins, then the executor's assessment, then the plan recommendation. Announce one lane and keep it unless the human changes it.

5. Load `test-driven-development` before the first task.

6. Resolve the shared off-repo workspace with:

   `py -3 ../subagent-workspace/scripts/workspace.py --apply PLAN_FILE`

   The workspace and ledger format are shared with `subagent-driven-development`. Never move them into the tracked tree.

7. Scan producer/consumer interfaces and Global Constraints. Ledger any conflict and its ruling before Task 1.

## Task Loop

Before the first helper invocation, resolve `bash` in the agent's host shell and identify its environment (`Get-Command bash` on Windows; `command -v bash` elsewhere; then `bash -lc 'uname -s; command -v git'`). Confirm Bash and Git belong to the same host environment as the active checkout. A Windows-hosted agent must not cross implicitly into WSL. Stop and repair host command resolution instead of searching for an alternate Bash ad hoc.

The helpers revalidate this boundary through `scripts/resolve-runtime`. They also resolve one concrete, same-host Python 3 executable in this order: `PYTHON_EXECUTABLE`, `python3`, `python`, then the Windows `py -3` launcher as a locator of last resort. They invoke the resolved executable, never the launcher. Environment mismatch is a hard stop before task work.

For each incomplete task:

1. Mark it in progress. Run `bash scripts/task-start PLAN_FILE N` through the verified same-host Bash. Do not create or request a PowerShell translation.
2. Read the emitted brief even when you remember the plan. `task-start` uses the shared Python `task_brief.py` helper; the brief carries exact paths, values, interfaces, and expected outputs.
3. Follow every step in order under TDD: write the test, witness the intended RED, implement the minimum GREEN, then refactor without adding behavior.
4. Run every specified command and compare its real output with `Expected:`. If code is wrong, use `systematic-debugging`; if the plan is wrong, ledger the smallest evidence-backed ruling that preserves the spec.
5. Commit as the task specifies. Multi-commit tasks keep the BASE printed by `task-start`; never substitute `HEAD~1`.
6. Run `bash scripts/task-done PLAN_FILE N BASE -- TEST_COMMAND [ARGS...]` through the same verified Bash. The helper stores the full test log, prints its tail, and appends completion only after a passing command.
7. Mark the task complete and continue without a ceremonial check-in.

## Per-Task Completion Contract

A task is complete only when:

- every named test exists and ran in this task;
- the RED failed for the intended missing behavior;
- the final focused command passed and its output was read;
- every expected output was compared with evidence;
- every deviation has a ledgered `Ruling:`; and
- the repository's applicable complete gate is green when the task or local contract requires it.

`verification-before-completion` governs every completion claim.

## Final Whole-Branch Review

After all tasks:

1. Run `handoff-gates` completion-readiness against the plan and repository code-review guide. Do not hand off below its readiness floor.
2. Build the range with `py -3 ../subagent-workspace/scripts/review_package.py --apply PLAN_FILE MERGE_BASE HEAD`.
3. Invoke `requesting-code-review` with a fresh whole-branch reviewer. Supply the review package, plan and spec, the plan's `Review Focus`, and every ledgered ruling. Model selection does not grant delegation authority; when runtime policy forbids a child, disclose the weaker self-review fallback.
4. Re-grade findings by effect on a reasonable user. Every `Declined to judge` line receives an explicit ruling rather than disappearing.
5. Fix Critical and Important findings in one pass, each with a witnessed RED/GREEN cycle, then run the complete repository gate. Ledger Minor findings as deferred rather than broadening the slice.

## Complete Development

1. Use `completing-planning-artifacts` in its completing-slice lane: promote durable decisions, mark the plan `completed-awaiting-retirement`, and retain it through the completing PR.
2. Use `finishing-a-development-branch` for final validation, publication proof, and the user's integration choice.
3. Keep a Draft PR draft until self-review and the latest committed tree pass the repository's required gate.
4. Before deleting scratch, report every ledgered ruling and deferred minor. Delete only this plan's workspace after the final review is clean.

## Common Rationalizations

| Excuse                                           | Reality                                                                                                               |
| ------------------------------------------------ | --------------------------------------------------------------------------------------------------------------------- |
| "I remember the task."                           | Read the brief; memory is a summary.                                                                                  |
| "The plan is wrong, so I'll quietly fix it."     | Make the technical correction and ledger the ruling.                                                                  |
| "Focused tests passed, so the project is green." | Run the consumer's declared complete gate before the completion claim.                                                |
| "I should ask whether to continue."              | The approved plan already authorizes the next task; stop only at the named human/safety boundary.                     |
| "Inline means no independent review."            | Native removes per-task review, not the one fresh whole-branch review.                                                |
| "Any Bash is close enough."                      | Bash must share the agent and checkout's host environment; WSL is not an implicit substitute for Windows-hosted Bash. |
| "Windows needs a PowerShell copy."               | The Bash helpers resolve a concrete same-host Python interpreter; do not fork their workflow semantics.               |
