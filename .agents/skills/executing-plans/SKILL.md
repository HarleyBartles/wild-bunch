---
name: executing-plans
description: "Use when you have a written implementation plan to execute in a separate session with review checkpoints"
metadata:
  source_category: "third_party"
  upstream_name: "executing-plans"
  upstream_version: "v6.1.0"
  adaptation_overlay: "adapters/codex/superpowers-plus/executing-plans"
  projection_plugin: "superpowers-plus"
  source_author: "obra"
  source_license: "MIT"
  source_repo: "https://github.com/obra/superpowers"
  source_path: "sources/third_party/superpowers/obra-superpowers/v6.1.0/skills/executing-plans/SKILL.md"
  content_mode: "adapted"
  adapted_author: "Harley Bartles"
  adaptation_note: "Added handoff-gates composition metadata and completion-readiness step to the execution workflow."
  use_when:
    - "Use when a written implementation plan exists and the work stays in the current session."
    - "Use when tasks are sequential or tightly coupled."
    - "Use when subagent support is unavailable or not desired."
  do_not_use_when:
    - "Do not use when tasks are independent and subagents are available; prefer subagent-driven-development."
    - "Do not use without an approved plan."
    - "Do not use when the plan has critical gaps or unresolved blockers."
  use_after: [handoff-gates, writing-plans]
  use_before: [handoff-gates, finishing-a-development-branch, requesting-code-review]
  use_with: [handoff-gates]
  related_skills: [handoff-gates, writing-plans, subagent-driven-development, finishing-a-development-branch, requesting-code-review]
---

# Executing Plans

## Overview

Load plan, review critically, execute all tasks, report when complete.

**Announce at start:** "I'm using the executing-plans skill to implement this plan."

**Note:** Tell your human partner that Superpowers works much better with access to subagents. The quality of its work will be significantly higher if run on a platform with subagent support (Claude Code, Codex CLI, Codex App, and Copilot CLI all qualify; see the per-platform tool refs in `../using-superpowers/references/`). If subagents are available, use superpowers:subagent-driven-development instead of this skill.

## The Process

### Step 1: Load and Review Plan
1. Read plan file
2. Review critically - identify any questions or concerns about the plan
3. If concerns: Raise them with your human partner before starting
4. If no concerns: Create todos for the plan items and proceed

### Step 2: Execute Tasks

For each task:
1. Mark as in_progress
2. Follow each step exactly (plan has bite-sized steps)
3. Run verifications as specified
4. Mark as completed

### Step 3: Complete Development

After all tasks complete and verified:
  - Run `handoff-gates` completion-readiness lane. Rate the completed work against the plan and the repo code review guide (9/10 target). Report the final rating. Do not hand off below 9/10.
- Announce: "I'm using the finishing-a-development-branch skill to complete this work."
- **REQUIRED SUB-SKILL:** Use superpowers:finishing-a-development-branch
- Follow that skill to verify tests, present options, execute choice

## When to Stop and Ask for Help

**STOP executing immediately when:**
- Hit a blocker (missing dependency, test fails, instruction unclear)
- Plan has critical gaps preventing starting
- You don't understand an instruction
- Verification fails repeatedly

**Ask for clarification rather than guessing.**

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

## Integration

**Required workflow skills:**
- **superpowers:using-git-worktrees** - Ensures isolated workspace (creates one or verifies existing)
- **superpowers:writing-plans** - Creates the plan this skill executes
- **superpowers:finishing-a-development-branch** - Complete development after all tasks
