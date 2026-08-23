---
name: iterative-review
description: Use when a draft PR is ready for subagent review before being marked ready for CI and human review.
metadata:
  source-id: iterative-review
  source-path: codex-marketplace/plugins/superpowers-plus/skills/iterative-review/SKILL.md
  provenance-name: Iterative Review first-party skill
  source-category: first_party
  status: active
  owner: Harley Bartles
  scope: Use when a draft PR is ready for subagent review before being marked ready for CI and human review.
  use_when:
  - Use when a draft PR is ready for subagent review before being marked ready for CI and human review.
  do_not_use_when:
  - Do not use when the PR has no changes to review.
  - Do not use as a substitute for the repo's canonical CI preflight.
  related_skills:
  - requesting-code-review
  - receiving-code-review
  - handoff-gates
  - selecting-a-subagent
  - dispatching-parallel-agents
license: MIT
---

## Provenance

This skill is a first-party skill authored for this repository. It is not derived from an upstream snapshot.

## Quick start

Do not read the whole skill or the graph reference before starting. `next_node.py` enforces the graph: it only allows the single next node. You cannot make an invalid move.

1. Identify the PR number.
2. From the branch worktree, run:
   ```
   py -3 .agents/skills/iterative-review/scripts/start_review.py --pr <pr_number> --apply
   ```
3. The script prints the one allowed next node, the recipe file to read, and the command to authorize it.
4. Open that one `references/node-<node>.md` file and follow it.
5. When the recipe says "next check", run `next_node.py` again. It will tell you the next node.
6. Continue until `next_node.py` prints `ready` or `blocked`.

`start_review.py` performs the `setup` and `normalize-inputs` nodes and leaves the graph pointing at `preflight`.

# Iterative Review

Run the review state graph on a draft PR before it is marked ready for CI and human review.

## When to Use

Use when a draft PR exists and needs an automated subagent review loop before being marked ready for CI and human review.

## Core Pattern

Follow the `review-state-graph.md` reference one node at a time, driven by `next_node.py`. The graph routes the orchestrator through deterministic preflight, `scope-honesty`, parallel `lens-dispatch` (which starts with the cheap `reviewer-fast` pre-lens), `lens-triage`, fast `finding-fix` by an `implementer` for `blocking/important` lens findings, `re-preflight`, lens-aware `reviewer-fixes`, `resolved-ledger`, conditional `regression-scan`, and a final `reviewer-strong` `final-strong` pass. `trivial/deferred` findings are left for `final-strong` instead of forcing an early whole-branch review. Every finding records the node and round that discovered it. There are no fixed "Round N" steps.

## Just-in-time reading

Read only the one `references/node-<node>.md` file that `next_node.py` tells you to read. The only files to read ahead of time are:

- this `SKILL.md` (this file)
- `references/review-state-graph.md` if you want the full map (optional; do not study it before starting)

`selecting-a-subagent` is used by `lens-dispatch` and `finding-fix` nodes; read it when `next_node.py` sends you there. The relevant `reviewer-*.md` lens profiles are discovered at `lens-dispatch` time.

The Devin Desktop agents search path is: user-global `~/.config/devin/agents/` (or `%APPDATA%\devin\agents\` on Windows), then `.devin/agents/`, then `.agents/agents/`. Discover `reviewer-*.md` files from that combined path; `.devin/agents/` and `.agents/agents/` take precedence over user-global.

## Following the graph

1. Run `start_review.py --pr <pr_number> --apply` from the branch worktree. It creates the off-repo scratch workspace, materializes the diff and PR context, runs `normalize-inputs`, and advances `review-state.json` to `normalize-inputs`.
2. Run `next_node.py` to discover the single allowed next node:
   ```
   py -3 .agents/skills/iterative-review/scripts/next_node.py --state <scratch_dir>/review-state.json
   ```
   Capture the first line of output as `<node>`.
3. Open `references/node-<node>.md` and follow that one recipe.
4. When the recipe says "next check", run `next_node.py` again.
5. Validate and advance the router to the discovered node before running its recipe:
   ```
   py -3 .agents/skills/iterative-review/scripts/next_node.py --propose <node> --state <scratch_dir>/review-state.json
   ```
6. Stop when `next_node.py` prints `ready` or `blocked`.

## Recording `review-metrics.json`

At every `metrics-track` and at `ready`, `resolved-ledger`, or `blocked`, write or update `review-metrics.json` in the off-repo scratch. The schema is in `references/review-metrics-schema.json`. This file is evidence for:

- **Fast catch**: `findings_by_node.preflight` should dominate.
- **Early catch**: most lens/strong findings should appear at low `discovered_at_round` values.
- **No sloppy fixes**: `regressions` should be low relative to `rounds_per_finding`.
- **Tunable regressions**: the `regression_class` distribution tells us whether late findings are due to weak lens review (`outside-blast-radius`), shoddy same-lens fixes (`same-lens-blast-radius`), or cross-cutting regressions (`cross-lens-blast-radius`).

For every post-fix finding, set `regression_class` from the decision table in the design spec (`## Concrete regression_class assignment`). Also set `regression_of` on the `rounds_per_finding` entry for the new finding.

## Inputs the orchestrator must provide

- `<base>` and `<branch>` (or `<head_sha>`)
- `<pr_number>` or `<pr_description>`

## Invariants

- Follow the graph in `references/review-state-graph.md`. Do not follow a round list.
- Read only the `node-<node>.md` file named by `next_node.py`. Do not read ahead.
- The `final-strong` pass is reachable only through `lens-triage` or after all `blocking/important` findings are resolved; there is no edge from `setup`, `preflight`, `fast-fix`, or `scope-honesty` directly to `final-strong`. If `lens-dispatch` is skipped, unavailable, or produces no logs, the review is `blocked`.
- This skill does not modify review files or PR state beyond the scope-honesty preflight.
- The orchestrator owns the scope-honesty preflight, all verification, the `resolved-ledger`, and the final decision to flip the PR to ready. `implementer` subagents own the fix edits under the orchestrator's brief. The cheap `reviewer-fast` pre-lens is a subagent; the orchestrator does not perform it by hand.
- All review inputs, logs, metrics, and fix-diffs are written to the off-repo scratch directory; they are never committed to the repo.
- CI must pass before leaving draft.

## Lens re-run scope

`lens-dispatch` runs at most once per review cycle. It dispatches every lens whose `## Applies to` rules match the PR.

When a finding is fixed, `finding-fix` -> `re-preflight` -> `reviewer-fixes` re-runs only the originating lens for that finding. Do not re-dispatch all lenses after a single fix; that is unnecessary churn and can introduce unrelated feedback late in the cycle.

## Machine-managed files

The following files in the off-repo scratch must be written only through the provided scripts. The orchestrator must not use `write` or `edit` on them:

- `review-state.json` - written by `next_node.py --propose` or `next_node.py --resync --apply`.
- `findings.jsonl`, `resolutions.jsonl`, `regressions.jsonl`, `blockers.jsonl` - written by `record_*.py` scripts.
- `lenses.jsonl` - written by `select_lenses.py --apply`.
- `review-log-reviewer-fast.md` - written by the `reviewer-fast` subagent as the pre-lens report.
- `review-metrics.json` - written by `compile_metrics.py`.

Lens subagents write their own `review-log-<lens>.md` files with `write` and end them with a one-line status. The `write` tool warning applies to orchestrator-authored files; it causes IDE buffer contention when a file is also open or being updated by a script.

## Common Mistakes

- Treating the skill as a fixed list of rounds. Use the graph.
- Reading `references/review-state-graph.md` or all `node-*.md` files before starting. Only read the one node `next_node.py` names.
- Treating `reviewer-fast` as a pass that allows skipping `lens-dispatch`, `lens-triage`, or `final-strong`. It is a cheap pre-filter, not a substitute for deep review.
- Running `lens-dispatch` without `reviewer-fast` in the selection. The `reviewer-fast` pre-lens is mandatory and is always included by `select_lenses.py`.
- Claiming subagents are unavailable and proceeding to `ready` without `lens-dispatch` or `final-strong`. If `run_subagent` cannot be used, the review is `blocked`.
- Skipping `re-preflight` after a fix. A fix can re-introduce deterministic issues.
- Skipping `regression-scan` for a non-trivial fix. A fix can cause a new issue in an adjacent area.
- Letting `reviewer-fixes` drift into a full branch review. Keep the input tightly scoped to the fix.
- Blindly applying reviewer findings without verification. Use `receiving-code-review` for each finding.
- Skipping CI after the reviewer loop. The reviewer "green" signal is not the draft/ready gate.
- Flipping a PR to ready without archiving the completed plan/spec/roadmap it implements. The ready state should represent the completed plan, including the moved planning artifacts.
