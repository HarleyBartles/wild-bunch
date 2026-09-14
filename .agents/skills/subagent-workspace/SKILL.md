---
name: subagent-workspace
description: Use when resolving the off-repo scratch workspace for subagent tasks and placing short-lived subagent inputs and outputs.
license: MIT.
metadata:
  source-id: subagent-workspace
  source-path: codex-marketplace/plugins/superpowers-plus/skills/subagent-workspace/SKILL.md
  provenance-name: Subagent Workspace first-party skill
  source-category: first_party
  status: active
  owner: Harley Bartles
  use_when:
  - a subagent task needs an off-repo scratch directory.
  - materializing inputs (diffs, PR descriptions, issues) for subagents to read.
  - routing subagent briefs, reports, review packages, or review logs to a disposable location.
  do_not_use_when:
  - durable custody, canonical source, provenance, or publication proof.
  - the artifact must survive beyond the current task.
  related_skills:
  - subagent-driven-development
  - iterative-review
  - selecting-a-subagent
---

## Provenance

This skill is a first-party skill authored for this repository. It is not derived from an upstream snapshot.

# Subagent Workspace

Resolve the canonical off-repo scratch workspace and place short-lived subagent artifacts there.

## Workspace location

The workspace lives outside the repository at the host-resolved sibling scratch
root, conventionally
`<main-checkout>/../_agent-scratch/<repo-name>/<branch>/<plan-basename>/`.
Use the resolver scripts below rather than inventing a machine drive or absolute
path. The workspace is never committed and survives `git clean`.

## Scripts

- `scripts/sdd-workspace [PLAN_FILE]` — bash workspace resolver.
- `scripts/sdd-workspace.ps1 [PLAN_FILE]` — PowerShell workspace resolver.
- `scripts/task-brief PLAN_FILE TASK_NUMBER [OUTFILE]` — bash task-brief extractor.
- `scripts/task-brief.ps1 PLAN_FILE TASK_NUMBER [OUTFILE]` — PowerShell task-brief extractor.
- `scripts/review-package PLAN_FILE BASE HEAD [OUTFILE]` — bash review-package builder; `PLAN_FILE` can be `-` for no plan.
- `scripts/review-package.ps1 PLAN_FILE BASE HEAD [OUTFILE]` — PowerShell review-package builder; `PLAN_FILE` can be `-` for no plan.

All scripts print the absolute output path. They write UTF-8 without a BOM so subagent `read` can open the files.

## Usage

For subagent-driven plans:

1. Run `scripts/sdd-workspace PLAN_FILE` and capture the printed path.
2. Run `scripts/task-brief PLAN_FILE <task-number>` to produce the task brief.
3. Run `scripts/review-package PLAN_FILE BASE HEAD` to produce the review package.
4. Write the subagent prompt and report under the same workspace.
5. When the task is done, the scratch directory can be discarded.

For iterative review:

1. Run `scripts/sdd-workspace` with no plan file and capture the workspace path.
2. Run `scripts/review-package - <base> <head> "$workspace/iterative-review-<pr_number>/review-<base7>..<head7>.diff"` to produce the UTF-8 diff package.
3. Write `pr.json` and `review-log.md` under the same `iterative-review-<pr_number>` directory.

## Rules

- Do not commit scratch files into the repo.
- Do not place canonical source or durable custody in scratch.
- If a scratch artifact ends up in the repo tree, remove it before committing.
