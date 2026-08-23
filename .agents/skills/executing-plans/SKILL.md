---
name: executing-plans
description: Use when you have a written implementation plan to execute in a separate
  session with review checkpoints
metadata:
  source-id: executing-plans
  source-path: codex-marketplace/plugins/superpowers-plus/skills/executing-plans/SKILL.md
  provenance-name: Executing Plans first-party skill
  source-category: first_party
  status: active
  owner: Harley Bartles
  scope: Use when you have a written implementation plan to execute in a separate
    session with review checkpoints
  use_when:
  - Use when a written implementation plan exists and the work stays in the current
    session.
  - Use when tasks are sequential or tightly coupled.
  - Use when subagent support is unavailable or not desired.
  do_not_use_when:
  - Do not use when tasks are independent and subagents are available; prefer subagent-driven-development.
  - Do not use without an approved plan.
  - Do not use when the plan has critical gaps or unresolved blockers.
  related_skills:
  - handoff-gates
  - writing-plans
  - subagent-driven-development
  - finishing-a-development-branch
  - requesting-code-review
license: MIT
---
## Provenance

This skill is a first-party authored derivation of `obra/superpowers` v6.2.0, released under the MIT License. The original upstream snapshot is retained in `codex-marketplace/plugins/superpowers-plus/skills/executing-plans/` for reference.

# Executing Plans

## Overview

Load plan, review critically, execute all tasks, report when complete.

**Announce at start:** "I'm using the executing-plans skill to implement this plan."

**Note:** Tell your human partner that Superpowers works much better with access to subagents (Claude Code, Codex CLI, Codex App, Copilot CLI, and Gemini CLI all qualify; see the per-platform tool refs in `../using-superpowers-plus/references/`). If subagents are available, use /subagent-driven-development instead of this skill.

## The Process

### Step 0: Load baseline and local guide

Read this skill's baseline (`references/implementation-baseline.md`) and the repo's `.agents/runbooks/implementing.md` before executing the stage checklist.

### Step 1: Load and Review Plan
1. Ensure an isolated workspace: use /using-git-worktrees to create one or verify the existing one
2. Read plan file
3. Note the `Execution Strategy` in the plan header. **MUST READ:** `references/execution-lane-override.md` and confirm the lane you are using is the right one: human explicit direction wins, then your own assessment, then the plan's recommendation
4. Announce the lane you will use and see it through unless the human asks to change
5. Review critically - identify any questions or concerns about the plan
6. If concerns: Raise them with your human partner before starting
7. If no concerns: Create todos for the plan items and proceed

### Step 2: Execute Tasks

For each task:
1. Mark as in_progress
2. Follow each step exactly (plan has bite-sized steps)
3. Run verifications as specified
4. Mark as completed

### Step 3: Complete Development

After all tasks complete and verified:
1. Run `handoff-gates` completion-readiness lane. Rate the completed work against the plan and the repo code review guide (9/10 target). Report the final rating. Do not hand off below 9/10.
2. Invoke `/requesting-code-review` for the final whole-branch review.
3. Announce: "I'm using the finishing-a-development-branch skill to complete this work."
4. **REQUIRED SUB-SKILL:** Use `/finishing-a-development-branch`
5. Follow that skill to verify tests, present options, execute choice

## When to Stop and Ask for Help

**STOP executing immediately when:**
- Hit a blocker (missing dependency, test fails, instruction unclear)
- Plan has critical gaps preventing starting
- You don't understand an instruction
- Verification fails repeatedly

**Ask for clarification rather than guessing.**

If a single missing fact blocks the next step, invoke `/asking-clarifying-questions` before guessing.

## When to Revisit Earlier Steps

**Return to Review (Step 1) when:**
- Partner updates the plan based on your feedback
- Fundamental approach needs rethinking

**Don't force through blockers** - stop and ask.

## Remember
- Review plan critically first
- Follow plan steps exactly
- Don't skip verifications
- Reference skills when plan says to
- Stop when blocked, don't guess
- Never start implementation on main/master branch without explicit user consent
