---
name: using-superpowers-plus
description: Use when starting any conversation - establishes how to find and use skills,
  requiring skill invocation before ANY response including clarifying questions
metadata:
  source-id: using-superpowers-plus
  source-path: codex-marketplace/plugins/superpowers-plus/skills/using-superpowers-plus/SKILL.md
  provenance-name: Using Superpowers Plus first-party skill
  source-category: first_party
  status: active
  owner: Harley Bartles
  scope: Use when starting any conversation - establishes how to find and use skills,
    requiring skill invocation before ANY response including clarifying questions
  use_when:
  - Use when starting any conversation to find and invoke the right skill.
  - Use when unsure whether a skill applies to the current task.
  - Use before any response or action when a workflow skill might be relevant.
  do_not_use_when:
  - Do not use when dispatched as a subagent with a specific task.
  - Do not use when user instructions explicitly override skill selection.
  - Do not use as a substitute for reading the chosen skill.
  use_before:
  - brainstorming
  - systematic-debugging
  - writing-plans
  - executing-plans
  - subagent-driven-development
  - using-git-worktrees
  - test-driven-development
  - verification-before-completion
  - publishing-source
  - finishing-a-development-branch
  - requesting-code-review
  - iterative-review
  - writing-roadmaps
  related_skills:
  - brainstorming
  - systematic-debugging
  - writing-plans
  - executing-plans
  - subagent-driven-development
  - using-git-worktrees
  - test-driven-development
  - verification-before-completion
  - publishing-source
  - finishing-a-development-branch
  - requesting-code-review
  - receiving-code-review
  - iterative-review
  - writing-skills
  - writing-roadmaps
  - repo-worker-base
  - base-doctrine
  - inspecting-the-environment
license: MIT
---

## Provenance

This skill is a first-party authored derivation of `obra/superpowers` v6.2.0, released under the MIT License. The original upstream snapshot is retained in `codex-marketplace/plugins/superpowers-plus/skills/using-superpowers-plus/` for reference.

<SUBAGENT-STOP>
If you were dispatched as a subagent to execute a specific task, ignore this skill.
</SUBAGENT-STOP>

<EXTREMELY-IMPORTANT>
At the start of every conversation, invoke `/using-superpowers-plus` first. It is the sole first-turn router.

Do not invoke other skills before `/using-superpowers-plus` has routed you to the owning skill. Once the owning skill is active, invoke the skills it explicitly tells you to at the relevant points in its workflow.
</EXTREMELY-IMPORTANT>

## The Rule

**Invoke `/using-superpowers-plus` before any response or action.** — including clarifying questions, exploring the codebase, or checking files. It will resolve the owning skill for the request.

**Then announce "Using [skill] to [purpose]" and follow that skill exactly.** If it has a checklist, create a todo per item. Do not load additional skills unless the current skill explicitly leaves a decision unresolved and another skill directly owns it.

**Before entering plan mode:** `/using-superpowers-plus` will route to `/brainstorming` if the request needs shaping, or directly to `/writing-plans` if an approved spec already exists.

## Skill Priority

When multiple skills apply, process skills come first — they set the approach, then implementation skills (frontend-design, etc.) carry it out. Brainstorming and systematic-debugging are Superpowers' most common process skills, but the rule holds for any of them.

- "Let's build X" → /brainstorming first, then implementation skills.
- "Fix this bug" → /systematic-debugging first, then domain skills.

## Red Flags

These thoughts mean STOP—you're rationalizing:

| Thought | Reality |
|---------|---------|
| "This is just a simple question" | Questions are tasks. Check for skills. |
| "I need more context first" | Skill check comes BEFORE clarifying questions. |
| "Let me explore the codebase first" | Skills tell you HOW to explore. Check first. |
| "I can check git/files quickly" | Files lack conversation context. Check for skills. |
| "Let me gather information first" | Skills tell you HOW to gather information. |
| "This doesn't need a formal skill" | If a skill exists, use it. |
| "I remember this skill" | Skills evolve. Read current version. |
| "This doesn't count as a task" | Action = task. Check for skills. |
| "The skill is overkill" | Simple things become complex. Use it. |
| "I'll just do this one thing first" | Check BEFORE doing anything. |
| "This feels productive" | Undisciplined action wastes time. Skills prevent this. |
| "I know what that means" | Knowing the concept ≠ using the skill. Invoke it. |

## Bootstrap order

This skill is the generic workflow router for any repo that installs the
superpowers-plus skill pack. At session start, resume, or when the next action
is unclear, run these steps in order and then hand off.

1. **The invocation rule.** If a skill applies to the request, invoke it before
   any response or action. You do not have a choice if a skill matches.
2. **Inspect the environment.** Invoke `/inspecting-the-environment` if the
   current environment is unknown or may have changed. Record the shell, repo,
   branch, worktree, and available connectors. Do not route until the
   environment is known.
3. **Load doctrine.** Invoke `/base-doctrine` for cross-runtime invariants,
   then load the repo-local doctrine from `.agents/doctrine/` by reading
   `.agents/doctrine/AGENTS.md` for scope and the relevant topic files.
   For how local doctrine and user instructions shape routing, see
   [`references/repo-doctrine.md`](references/repo-doctrine.md).
4. **Classify the request.** Pick the smallest sufficient mode from
   [`references/bootstrap-routing.md`](references/bootstrap-routing.md).
5. **Route and stop.** Hand off to the owning skill. Do not load additional
   skills unless the current skill leaves a decision unresolved and the
   candidate skill directly owns it.

## Platform Adaptation

If your harness appears here, read its reference file for special instructions:

- Codex: `references/codex-tools.md`
- Pi: `references/pi-tools.md`
- Antigravity: `references/antigravity-tools.md`
- Gemini: `references/gemini-tools.md`

For the local-doctrine and user-instruction priority rules, see
[`references/repo-doctrine.md`](references/repo-doctrine.md).
