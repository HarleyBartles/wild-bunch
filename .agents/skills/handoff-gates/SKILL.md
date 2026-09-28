---
name: handoff-gates
description: Use when a plan or completed implementation needs a readiness check before handoff to execution or code review.
metadata:
  source-id: handoff-gates
  source-path: codex-marketplace/plugins/superpowers-plus/skills/handoff-gates/SKILL.md
  provenance-name: Handoff Gates first-party skill
  source-category: first_party
  status: active
  owner: Harley Bartles
  scope: Readiness gates for planning-to-execution and execution-to-code-review handoffs.
  use_when:
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
    - writing-plans
    - executing-plans
  use_before:
    - executing-plans
    - subagent-driven-development
    - finishing-a-development-branch
    - requesting-code-review
license: MIT
---

# Handoff Gates

## Overview

Rate stage-boundary artifacts for execution confidence. Never hand off below 8/10. Target 9/10+. The score is a behavioral forcing function used during the handoff; do not persist it in a roadmap, ledger, PR, or other durable record.

## Lanes

- **plan-readiness** (planning → execution): Can the implementing agent or orchestrator plus subagents execute this plan without improvising mid-flight?
- **completion-readiness** (execution → code review): What will a code reviewer find when they review this work against the plan and the repo's code review guide?

## Rating scale

Use a 1–10 execution-confidence scale.

- **< 8:** Identify gaps, strengthen, and re-rate. Never proceed below 8.
- **8–8.9:** Try one bounded strengthening pass to reach 9+.
- **≥ 9:** Proceed to handoff and report the final rating in the current conversation only.

## How to Use

1. Read the artifact produced by the previous stage.
2. Pick the lane matching the boundary.
3. Score the artifact against the lane question and checklist.
4. Strengthen gaps until the score is at least 8, targeting 9+.
5. Report the rating in the current handoff and proceed, or return `blocked` with the unresolved gaps.

## Plan-Readiness Checklist

For plan-readiness, rate the artifact against these items. Strengthen any that fail before handoff.

- [ ] **Dependency-order coherence.** Each task's `Consumes` block only references earlier tasks. If a later output is needed earlier, move the producer, split a step, or add a bridge.

- [ ] **Task ordering.** Schedule producers before consumers. In this repo, source and overlay edits precede regeneration and CI. In consumer repos, use the consumer's canonical regeneration and preflight commands.

- [ ] **Clean CI gate.** Do not run the repo's canonical CI immediately before a normal commit or immediately after a successful hooked commit. Stage the intended tree and commit; the pre-commit hook will materialize the staged snapshot, run the repository's canonical apply gate, stage the owned generated surfaces, and run the repository's canonical check gate with diagnostics before allowing the commit. Use the consumer's canonical check command only for an uncommitted verification, pipeline diagnosis, or explicit CI-parity work. Do not use `git commit --no-verify` to bypass the pre-commit hook.

- [ ] **Explicit verification.** Each regeneration or distribution task names the exact consumer command and any follow-up CI check. Do not assume a particular repository helper or command exists in every consumer repo.

- [ ] **No temporary validation drift.** If a task is expected to leave the tree in a temporarily unbuildable state, it is explicitly documented so the implementer and reviewer know it is expected.

- [ ] **Plan-specific execution lane.** Compare the plan's proposed lane with its nearest credible alternative using evidence from the saved plan. Consider task independence, dependency order, shared implementation context, state coupling, review burden, context-reconstruction cost, and consequence. State why the selected lane wins for this plan. Plan length and task count alone do not decide the lane.

## Execution Lane Recommendation

After reviewing the complete plan and before handing it to execution, examine its `Execution Strategy` field. Treat the template's parenthetical `subagent-driven-development (recommended)` as a candidate to question, not evidence that SDD fits this plan. Compare that proposal with the nearest credible alternative; when parallel tracks are genuinely independent, include `dispatching-parallel-agents` among the candidates.

Ask yourself: "Having read this plan, do I really recommend the lane currently written here? Would I choose it if the template had not called SDD recommended?"

Use plan evidence to weigh:

- whether tasks can be implemented and reviewed independently;
- how tasks depend on earlier outputs and how much shared state or implementation context they use;
- whether fresh per-task implementer and reviewer contexts are likely to catch meaningful defects;
- how much handoff and context-reconstruction work repeated task boundaries add;
- whether parallel progress or continuity matters more for the consequence and verification burden of this change.

Do not infer independence from separate checklist items or test steps. A long plan can be tightly coupled; a short plan can contain independent work. Compare the closest credible alternative and explain the concrete trade-off in the current handoff. Before handoff, update or confirm that the saved plan's `Execution Strategy` field names the selected lane and gives a concise plan-specific reason. Keep the required subskill header unchanged. Do not persist the readiness score or extended comparative reasoning in the plan.

The value left in the saved `Execution Strategy` field is the operative recommendation for this plan. The required subskill header remains as authored by the planning template and does not override that selected value.

## Boundary cases

If the plan is blocked externally, or the boundary touches `verification-before-completion` or `requesting-code-review`, load `references/scope-notes.md` and only proceed on a green path.

## Common Mistakes

- Handing off at 7/10 because the artifact is "good enough."
- Chasing a 10 forever instead of handing off after the bounded strengthening pass.
