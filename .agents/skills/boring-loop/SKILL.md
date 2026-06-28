---
name: boring-loop
description: Use when coordinating a boring work loop, picking the next smallest safe
  move, or preventing false-green repo work.
metadata:
  source-id: boring-loop
  source-path: sources/first_party/skills/boring-loop/SKILL.md
  provenance-name: Boring Loop first-party skill
  source-category: first_party
  status: active
  owner: Harley Bartles
  scope: Use when coordinating a boring work loop, picking the next smallest safe
    move, or preventing false-green repo work.
  use_when:
  - Use when coordinating a boring work loop, picking the next smallest safe move,
    or preventing false-green repo work.
  do_not_use_when:
  - Do not use when another more specific skill owns this task.
license: MIT
---
# Boring Loop

Use this skill when the work needs a small, repeatable loop that stays honest about readiness, scope, and proof.

## Core loop

1. Read the current durable state.
2. Pick the next boring move that reduces uncertainty or closes a blocker.
3. Do one bounded slice of work.
4. Verify the result against durable evidence.
5. Return the exact state, evidence, and remaining queue.

Do not turn the loop into a broad process handbook. Keep it to coordination and route selection.

## Readiness

Do not mark work green unless all of these are true:

- the target is named;
- the source of truth is current;
- the mutation surface is bounded;
- the proof route is known;
- the result is falsifiable from durable evidence;
- there is no hidden dependency on chat-only context.

If any item is missing, the right answer is amber or blocked, not green.

## False-green prevention

Treat these as warning signs:

- a report that sounds complete but lacks a durable artifact or verified state;
- a plan that does not name the exact files, records, or surfaces it will touch;
- a claim that one child issue covers a whole parent DOD when it only covers part of it;
- a queue that keeps growing because the next move was never narrowed enough.

If the work looks too broad, narrow it before continuing.

Route-state false greens to name explicitly:

- executing without a fresh staleness check against current source;
- treating an unmerged plan PR as if it were approved execution authority;
- relying on chat memory instead of durable Linear/repo evidence;
- missing plan path, plan commit, or plan PR evidence;
- laundering ambiguous or contradictory route state into execution.

## Parent and child coverage

When a parent issue exists, the parent owns the full boring definition of done.

Each child issue must own a single slice of that parent DOD.

The children are only good enough when their slices collectively cover the parent.

## Queue grooming

Keep the queue short and explicit.

Prefer the next item that is:

- smallest;
- cheapest to prove;
- least likely to hide a dependency;
- most likely to unblock the rest of the queue.

Do not leave a vague queue order that depends on memory.

## Route to specialist skills

Route out instead of restating specialist procedure.

- When readiness is uncertain or the work might be too broad or false-green: `boring-buster`
- Linear issue shaping, issue-track shaping, or worker-packet shaping: `linear-superpowers`
- Linear connector side effects, including issue create/update, comments, status, labels, assignments, or readback after mutation: `connector-safety`
- Implementation planning: `writing-plans`
- Code execution workflow: `executing-plans`
- GitHub proof, review routing, or branch closeout: `github-operations` or `github-superpowers`
- Verification before completion: `verification-before-completion`
- Repo-specific anti-slop controls: `unslop-superpowers`
- Skill lifecycle work: use the current repo-backed install/projection lane for skill work; do not route new work through the retired package/install/handoff stack.
- Dense issue bodies, connector-hostile content, or moving detail to attached Linear docs: `linear-superpowers`

Use the specialist skill for the procedure. Use Boring Loop only to decide when to route there and what the next boring move is.

## Variant boundary

Codex-worker guidance:

- participate in the loop;
- return evidence;
- report blockers without dressing them up.

GPT-controller guidance:

- operate the loop;
- choose the next boring move;
- route to the right specialist;
- ask for repair when the work is not yet boring enough.

Both variants use the same first-party source and the same retained doctrine.
