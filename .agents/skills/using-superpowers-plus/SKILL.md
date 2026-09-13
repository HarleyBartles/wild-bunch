---
name: using-superpowers-plus
description: Use when starting or resuming a conversation that may need workflow,
  doctrine, safety, or repository-scope routing.
metadata:
  source-id: using-superpowers-plus
  source-path: codex-marketplace/plugins/superpowers-plus/skills/using-superpowers-plus/SKILL.md
  provenance-name: Using Superpowers Plus first-party skill
  source-category: first_party
  status: active
  owner: Harley Bartles
  scope: First-turn workflow routing with explicit proportionality, checkpoint,
    taste-ambiguity, and destructive-authority exceptions
  use_when:
  - starting any conversation to find and invoke the right skill.
  - unsure whether a skill applies to the current task.
  - a workflow skill might be relevant to the next response or action.
  do_not_use_when:
  - dispatched as a subagent with a specific task.
  - user instructions explicitly override skill selection.
  - a substitute for reading the chosen skill.
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

This marketplace-maintained skill succeeds upstream `using-superpowers` and is based on `obra/superpowers` v6.3.0 commit `b36e0829c6d0140e93cfef2ca599b1b07d4a7797` under the MIT License. Upstream source is not vendored; this directory contains the maintained Superpowers+ implementation.

<SUBAGENT-STOP>
If you were dispatched as a subagent to execute a specific task, ignore this skill.
</SUBAGENT-STOP>

<EXTREMELY-IMPORTANT>
At the start of every conversation, use `using-superpowers-plus` as the sole
first-turn router, including the explicit exceptions and fast paths below.

Do not invoke other skills before `using-superpowers-plus` has routed you to the owning skill. Once the owning skill is active, invoke the skills it explicitly tells you to at the relevant points in its workflow.
</EXTREMELY-IMPORTANT>

## First-turn exceptions and fast paths

These rules are part of this router. Apply them directly from the request; do
not read another skill or inspect the repository first.

### Tiny reversible fast path

For one fully specified, local, reversible edit with one obvious target and no
product, taste, authority, safety, publication, or architectural decision:

1. Do not invoke `inspecting-the-environment` or narrate skill selection.
2. Give at most one short action update.
3. Make the edit, run one focused check, and report the result.

### Taste ambiguity stop

An unresolved human-owned taste word such as “premium,” “playful,” “bold,” or
“more polished” is not an implementation target. Invoke
`asking-clarifying-questions` immediately and ask one concrete question before
reading source or editing. Do not silently translate taste into copy, colour,
layout, or architecture.

### Checkpoint-first resume exception

When a resumed or compacted-work request explicitly identifies a durable
checkpoint as the first source, read that checkpoint before this skill or any
other repository source. Then invoke `using-superpowers-plus`, reconcile the
checkpoint against live state, and continue by the selected route. This narrow
ordering exception preserves the checkpoint's role without treating its claims
as current truth.

### Destructive-authority stop

When the request asks for destructive or irreversible work but does not grant
clear authority, the first response must state that authority is missing and
offer a reversible alternative. Do not inspect the repository or announce an
intention to perform the destructive action first. Invoke `risk-gates` only
after that immediate safety response if further work remains. A backup or
reversible preparation does not grant authority: stop and wait for explicit
authorization before inspection, `git switch --orphan`, reflog expiry, garbage
collection, branch replacement, or another history-rewrite step.

### Portable and repository guidance conflict

If a portable suggestion conflicts with repository guidance, inspect repository
canon and the owner gate, then name the required local evidence before ruling.

## The Rule

**Apply `using-superpowers-plus` before any ordinary response or action.** Its
first-turn exceptions above may require a bounded edit, checkpoint read,
clarifying question, or safety response before another skill or repository
inspection. Otherwise it resolves the owning skill for the request.

**Then announce "Using [skill] to [purpose]" and follow that skill exactly.** If it has a checklist, create a todo per item. Do not load additional skills unless the current skill explicitly leaves a decision unresolved and another skill directly owns it.

**Before entering plan mode:** `using-superpowers-plus` will route to `brainstorming` if the request needs shaping, or directly to `writing-plans` if an approved spec already exists.

## Skill Priority

When multiple skills apply, process skills come first — they set the approach, then implementation skills (frontend-design, etc.) carry it out. Brainstorming and systematic-debugging are Superpowers' most common process skills, but the rule holds for any of them.

- "Let's build X" → brainstorming first, then implementation skills.
- "Fix this bug" → systematic-debugging first, then domain skills.

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

1. **Classify the request.** Pick the smallest sufficient mode from
   [`references/bootstrap-routing.md`](references/bootstrap-routing.md) using
   user intent and immediately available context. Announce the route so the
   human can override it.
2. **Inspect only route-changing environment dimensions.** Invoke
   `inspecting-the-environment` when shell, repository, branch, worktree, or
   connector facts can change the selected route or immediate action. Do not
   perform a broad inventory merely because the skill is available.
3. **Load only selected doctrine.** Invoke `base-doctrine` for cross-runtime
   invariants, then read only the repo-local doctrine and owning references
   required by the selected route. For local-doctrine and user-instruction
   priority rules, see [`references/repo-doctrine.md`](references/repo-doctrine.md).
4. **Route and stop.** Hand off to the owning skill and stop reading once the
   next lawful action is known. Do not load additional
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
