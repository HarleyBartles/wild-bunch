---
name: handoff-gates
description: Use when a stage-boundary artifact (spec, plan, or completed work) needs a readiness check before handoff.
metadata:
  source-id: handoff-gates
  source-path: codex-marketplace/plugins/superpowers-plus/skills/handoff-gates/SKILL.md
  provenance-name: Handoff Gates first-party skill
  source-category: first_party
  status: active
  owner: Harley Bartles
  scope: Readiness gates for brainstorming, planning, execution, and code-review handoffs.
  use_when:
  - Use when a spec is ready to move from brainstorming to planning.
  - Use when a plan is ready to move from writing-plans to execution.
  - Use when completed work is ready to move from executing-plans to code review.
  do_not_use_when:
  - Do not use when the artifact is not clearly at a stage boundary (see references/scope-notes.md for boundary cases)
  - Do not use as a substitute for risk-gates when the question is pre-action risk.
  related_skills:
  - risk-gates
  - writing-plans
  - executing-plans
  - subagent-driven-development
  - writing-roadmaps
  use_after:
  - brainstorming
  - writing-plans
  - executing-plans
  use_before:
  - writing-plans
  - executing-plans
  - subagent-driven-development
  - finishing-a-development-branch
  - requesting-code-review
license: MIT
---

# Handoff Gates

## Overview

Rate stage-boundary artifacts for execution confidence. Never hand off below 8/10. Target 9/10+.

## Lanes

- **spec-readiness** (brainstorming → planning): Can a planning agent expand this spec into a full plan without improvising or discovering seams mid-flight?
- **plan-readiness** (planning → execution): Can the implementing agent or orchestrator plus subagents execute this plan without improvising mid-flight?
- **completion-readiness** (execution → code review): What will a code reviewer find when they review this work against the plan and the repo's code review guide?

## Rating Scale

1–10 execution-confidence scale.

- **< 8:** Identify gaps, strengthen, re-rate. Never proceed below 8.
- **8–8.9:** Try one bounded strengthening pass to reach 9+.
- **≥ 9:** Proceed to handoff. Report the final rating and record it in the roadmap.

## How to Use

1. Read the artifact produced by the previous stage.
2. Pick the lane matching the boundary.
3. Score the artifact against the lane question.
4. Strengthen gaps until the score is ≥ 8 (target ≥ 9).
5. Report the final rating and hand off to the next stage.

## Plan-Readiness Checklist

For SDD `plan-readiness`, rate the artifact against these items. Strengthen any that fail before handoff.

- [ ] **Dependency-order coherence.** Each task's `Consumes` block only references earlier tasks. If a later output is needed earlier, move the producer, split a step, or add a bridge.

- [ ] **Task ordering.** Schedule producers before consumers. In this repo, source and overlay edits precede regeneration and CI. In consumer repos, use the consumer's canonical regeneration and preflight commands.

- [ ] **Plan-step tracking.** Each task includes a final sub-step for the implementer to mark the task's own checklist boxes `[x]` in the plan file.

- [ ] **Clean CI gate.** Do not run the repo's canonical CI on an uncommitted tree. Regenerate any generated surfaces, stage all changes, run the canonical CI check on the staged tree, then commit. Use `tools/run ci --check` here; consumer repos use their preflight. Do not use `git commit --no-verify` to bypass the pre-commit hook.

- [ ] **Explicit verification.** Each regeneration or distribution task names the exact consumer command and any follow-up CI check. Do not assume a `tools/run` target exists in every repo.

- [ ] **No temporary validation drift.** If a task is expected to leave the tree in a temporarily unbuildable state, it is explicitly documented so the implementer and reviewer know it is expected.

## Boundary cases

If the artifact is intentionally thin, blocked externally, or touches `verification-before-completion` or `requesting-code-review`, load `references/scope-notes.md` and only proceed on a green path.

## Common Mistakes

- Handing off at 7/10 because the plan is "good enough." → Scores below 8 are blocked.
- Chasing a 10 forever. → One strengthening pass from 8–8.9 is enough.
