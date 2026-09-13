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
  - a spec is ready to move from brainstorming to planning.
  - a plan is ready to move from writing-plans to execution.
  - completed work is ready to move from executing-plans to code review.
  do_not_use_when:
  - the artifact is not clearly at a stage boundary (see references/scope-notes.md for boundary cases)
  - a substitute for risk-gates when the question is pre-action risk.
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

Rate stage-boundary artifacts for execution confidence. Never hand off below
8/10. Target 9/10+. The score is a behavioral forcing function used during the
handoff; do not persist it in a roadmap, ledger, PR, or other durable record.

## Lanes

- **spec-readiness** (brainstorming → planning): Can a planning agent expand this spec into a full plan without improvising or discovering seams mid-flight?
- **plan-readiness** (planning → execution): Can the implementing agent or orchestrator plus subagents execute this plan without improvising mid-flight?
- **completion-readiness** (execution → code review): What will a code reviewer find when they review this work against the plan and the repo's code review guide?

## Rating scale

Use a 1–10 execution-confidence scale.

- **< 8:** Identify gaps, strengthen, and re-rate. Never proceed below 8.
- **8–8.9:** Try one bounded strengthening pass to reach 9+.
- **≥ 9:** Proceed to handoff and report the final rating in the current
  conversation only.

## How to Use

1. Read the artifact produced by the previous stage.
2. Pick the lane matching the boundary.
3. Score the artifact against the lane question and checklist.
4. Strengthen gaps until the score is at least 8, targeting 9+.
5. Report the rating in the current handoff and proceed, or return `blocked`
   with the unresolved gaps.

## Plan-Readiness Checklist

For SDD `plan-readiness`, rate the artifact against these items. Strengthen any that fail before handoff.

- [ ] **Dependency-order coherence.** Each task's `Consumes` block only references earlier tasks. If a later output is needed earlier, move the producer, split a step, or add a bridge.

- [ ] **Task ordering.** Schedule producers before consumers. In this repo, source and overlay edits precede regeneration and CI. In consumer repos, use the consumer's canonical regeneration and preflight commands.

- [ ] **Clean CI gate.** Do not run the repo's canonical CI immediately before a normal commit or immediately after a successful hooked commit. Stage the intended tree and commit; the pre-commit hook will materialize the staged snapshot, run the repository's canonical apply gate, stage the owned generated surfaces, and run the repository's canonical check gate with diagnostics before allowing the commit. Use the consumer's canonical check command only for an uncommitted verification, pipeline diagnosis, or explicit CI-parity work. Do not use `git commit --no-verify` to bypass the pre-commit hook.

- [ ] **Explicit verification.** Each regeneration or distribution task names the exact consumer command and any follow-up CI check. Do not assume a particular repository helper or command exists in every consumer repo.

- [ ] **No temporary validation drift.** If a task is expected to leave the tree in a temporarily unbuildable state, it is explicitly documented so the implementer and reviewer know it is expected.

## Boundary cases

If the artifact is intentionally thin, blocked externally, or touches `verification-before-completion` or `requesting-code-review`, load `references/scope-notes.md` and only proceed on a green path.

## Common Mistakes

- Handing off at 7/10 because the artifact is "good enough."
- Chasing a 10 forever instead of handing off after the bounded strengthening pass.
