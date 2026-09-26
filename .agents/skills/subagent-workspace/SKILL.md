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

The workspace lives outside the repository at the host-resolved sibling scratch root, conventionally `<main-checkout>/../_agent-scratch/<repo-name>/<branch>/<plan-basename>/`. Use the resolver scripts below rather than inventing a machine drive or absolute path. The workspace is never committed and survives `git clean`.

## Scripts

- `py -3 scripts/workspace.py --apply [PLAN_FILE]` — workspace resolver.
- `py -3 scripts/task_brief.py --apply PLAN_FILE TASK_NUMBER [OUTFILE]` — task-brief extractor.
- `py -3 scripts/review_package.py --apply PLAN_FILE BASE HEAD [OUTFILE]` — review-package builder; `PLAN_FILE` can be `-` for no plan.

All helpers are Python 3 CLIs. They default to read-only `--check` behavior; filesystem writes require `--apply`. They print the absolute output path and write UTF-8 without a BOM so subagent `read` can open the files.

## Usage

For subagent-driven plans:

1. Run `py -3 scripts/workspace.py --apply PLAN_FILE` and capture the printed path.
2. Run `py -3 scripts/task_brief.py --apply PLAN_FILE <task-number>` to produce the task brief.
3. Run `py -3 scripts/review_package.py --apply PLAN_FILE BASE HEAD` to produce the review package.
4. Write the subagent prompt and report under the same workspace.
5. When the task is done, the scratch directory can be discarded.

For iterative review:

1. Run `py -3 scripts/workspace.py --apply` with no plan file and capture the workspace path.
2. Run `py -3 scripts/review_package.py --apply - <base> <head> <output-path>` to produce the UTF-8 diff package.
3. Write `pr.json` and `review-log.md` under the same `iterative-review-<pr_number>` directory.

## Rules

- Do not commit scratch files into the repo.
- Do not place canonical source or durable custody in scratch.
- If a scratch artifact ends up in the repo tree, remove it before committing.
