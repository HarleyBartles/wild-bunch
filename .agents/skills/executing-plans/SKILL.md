---
name: executing-plans
description: Use when executing an approved written plan in a separate session or
  resuming plan execution from a durable checkpoint.
metadata:
  source-id: executing-plans
  source-path: codex-marketplace/plugins/superpowers-plus/skills/executing-plans/SKILL.md
  provenance-name: Executing Plans first-party skill
  source-category: first_party
  status: active
  owner: Harley Bartles
  use_when:
  - a written implementation plan exists and the work stays in the current
    session.
  - tasks are sequential or tightly coupled.
  - subagent support is unavailable or not desired.
  do_not_use_when:
  - tasks are independent and subagents are available; prefer subagent-driven-development.
  - without an approved plan.
  - the plan has critical gaps or unresolved blockers.
  related_skills:
  - handoff-gates
  - writing-plans
  - subagent-driven-development
  - finishing-a-development-branch
  - requesting-code-review
license: MIT
---
## Provenance

This marketplace-maintained derivative is based on `obra/superpowers` v6.3.0 commit `b36e0829c6d0140e93cfef2ca599b1b07d4a7797` under the MIT License. Upstream source is not vendored; this directory contains the maintained Superpowers+ implementation.

# Executing Plans

## Overview

Load plan, review critically, execute all tasks, report when complete.

**Announce at start:** "I'm using the executing-plans skill to implement this plan."

**Note:** Tell your human partner that Superpowers works much better with access to subagents (Claude Code, Codex CLI, Codex App, Copilot CLI, and Gemini CLI all qualify; see the per-platform tool refs in `../using-superpowers-plus/references/`). If subagents are available, use subagent-driven-development instead of this skill.

## The Process

### Step 0: Load baseline and local guide

Read this skill's baseline (`references/implementation-baseline.md`) and the repo's `.agents/runbooks/implementing.md` before executing the stage checklist.

### Step 1: Load and Review Plan
1. On a resumed or compacted session, read the durable checkpoint before live repository inspection. Its claims are context, not current truth, but it determines the minimum state that must be reconciled.
2. Ensure an isolated workspace: use using-git-worktrees to create one or verify the existing one
3. Read plan file (or the minimum sections named by the checkpoint)
4. Note the `Execution Strategy` in the plan header. **MUST READ:** `references/execution-lane-override.md` and confirm the lane you are using is the right one: human explicit direction wins, then your own assessment, then the plan's recommendation
5. Announce the lane you will use and see it through unless the human asks to change
6. Review critically - separate falsifiable technical concerns from human-owned requirements, product/canon choices, authority, and unauthorized irreversible/external consequences
7. Resolve falsifiable technical concerns with bounded inspection or an evidence-backed technical ruling; record the ruling and continue
8. Ask the human only when the shared human stop boundary is reached; otherwise create todos for the plan items and proceed

### Step 2: Execute Tasks

For each task:
1. Mark as in_progress
2. Follow each step exactly (plan has bite-sized steps)
3. Run verifications as specified
4. Mark as completed

### Step 3: Complete Development

After all tasks complete and verified:
1. Run the `handoff-gates` completion-readiness lane against the plan and repo code-review guide. Rate the work (9/10 target), report the rating in the current handoff, and do not persist it or hand off below 8/10.
2. Invoke `requesting-code-review` for the final whole-branch review.
3. Announce: "I'm using the finishing-a-development-branch skill to complete this work."
4. **REQUIRED SUB-SKILL:** Use `finishing-a-development-branch`
5. Follow that skill to verify tests, present options, execute choice

## When to Stop and Ask for Help

**STOP executing immediately when:**
- A human-owned requirement, product/canon choice, or authority decision is required
- An unauthorized destructive, irreversible, permission-changing, security-sensitive, or externally consequential action is required
- A plan defect leaves every plausible path forward as a guess

Missing dependencies, failed tests, unclear technical instructions, and failed
verification are diagnosis inputs, not automatic human pauses. Investigate and
rule when the question is falsifiable; ask rather than guess only when the
shared human stop boundary is actually reached.

If a missing fact is human-owned or cannot lawfully be resolved by inspection,
invoke `asking-clarifying-questions`. Otherwise investigate the technical fact
and continue without converting it into a permission question.

## When to Revisit Earlier Steps

**Return to Review (Step 1) when:**
- Partner updates the plan based on your feedback
- Fundamental approach needs rethinking

**Don't force through human-owned or safety/authority blockers.** Resolve
technical blockers from evidence where possible; stop only when no lawful
technical path remains.

## Remember
- Review plan critically first
- Follow plan steps exactly
- Don't skip verifications
- Reference skills when plan says to
- Stop only at the shared human/safety/authority boundary; investigate technical blockers rather than guessing
- Never start implementation on main/master branch without explicit user consent
