---
name: writing-roadmaps
description: Use when a goal is too large for one writing-plans plan and requires a sequenced roadmap of consecutive plans.
metadata:
  source-id: writing-roadmaps
  source-path: codex-marketplace/plugins/superpowers-plus/skills/writing-roadmaps/SKILL.md
  provenance-name: Writing Roadmaps first-party skill
  source-category: first_party
  status: active
  owner: Harley Bartles
  scope: Decompose large goals into roadmaps and execute consecutive plans.
  use_when:
  - Use when writing-plans scope check fails because the spec covers multiple independent subsystems.
  - Use when the human frames a request as a large or epic goal.
  - Use when continuing an existing epic roadmap.
  do_not_use_when:
  - Do not use when the goal fits a single tight writing-plans plan (see references/scope-notes.md for the one-plan-vs-epic boundary)
  - Do not use as a substitute for writing-plans on small, well-defined tasks.
  related_skills:
  - handoff-gates
  - writing-plans
  - executing-plans
  - subagent-driven-development
  - brainstorming
  use_after:
  - brainstorming
  use_before:
  - handoff-gates
  - executing-plans
  use_with:
  - handoff-gates
  - writing-plans
  - executing-plans
  - subagent-driven-development
license: MIT
---

# Writing Roadmaps

## Overview

Break large goals into a roadmap of consecutive plans, execute them, and keep the roadmap as a live work log.

## Lane 1 — Start an Epic

1. Read the spec from brainstorming or the human.
2. Run `handoff-gates` spec-readiness.
3. Create `.agents/plans/<epic-name>/roadmap.md` with a plan sequence table.
4. Use `writing-plans` to write Plan 1 with roadmap context.
5. Run `handoff-gates` plan-readiness.
6. Hand off to `executing-plans` or `subagent-driven-development`.

## Lane 2 — Continue an Epic

1. Read the roadmap.
2. Pick the next pending or blocked item.
3. Use `writing-plans` to write the next plan just-in-time, including all prior commits, PRs, worktree state, and learnings.
4. Run `handoff-gates` plan-readiness.
5. Execute the plan.
6. Update the roadmap with status, commit, PR, final rating, and notes.
7. Repeat until done. Run `handoff-gates` completion-readiness before code review.

## Lane 3 — Split an Oversized Plan

If a plan in progress grows beyond one deliverable, stop writing. **MUST READ:** `references/plan-scope-sizing.md` and use escape hatch 3:

1. Close the current plan file at a clean boundary (end of the last fully scoped task).
2. Create a new plan file for the remaining scope in the same epic directory.
3. Update the epic roadmap table with the new plan, its place in the sequence, and a `Handoff Notes` entry explaining why the split happened.
4. Mark the original plan as `blocked` or `replan` in the roadmap if it cannot continue as written.

This is a fallback for legitimately over-scoped plans. It is not an excuse to create endless epics of tiny plans.

## Roadmap Schema

A markdown table with `#`, `Title`, `Status`, `Plan File`, `Commit`, `PR`, `Rating`, `Notes`.
Status values: `pending`, `writing`, `ready`, `executing`, `done`, `blocked`.

## Blocked Plans

If a plan is stuck below 8/10 and cannot be strengthened autonomously, ask the human one focused question. Do not proceed below 8/10. Do not reduce scope without human consultation. Update the roadmap item to `blocked`.

## Scope Changes

The roadmap is a live look-ahead document. Edit it inline as decisions change the forward path and document the change in `Handoff Notes`. Major structural changes may trigger a quick re-plan via `brainstorming`.

## Boundary cases

If a roadmap item should split into a new epic, a scope change invalidates multiple pending plans, or you are choosing between asking the human and escalating through `risk-gates`, load `references/scope-notes.md` and `references/plan-scope-sizing.md` and follow their guidance.

## Common Mistakes

- Writing all plans upfront. → Write each plan just-in-time with current context.
- Skipping the rating gate. → Every plan must pass `handoff-gates` plan-readiness before execution.
