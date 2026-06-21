---
name: linear-superpowers
description: Use when shaping Linear issues, issue tracks, and worker packets so they name the smallest applicable Superpowers workflow skill, explain why it applies, and name the evidence required to prove it was followed.
metadata:
  source-id: linear-superpowers
  source-path: sources/first_party/skills/linear-superpowers/SKILL.md
  provenance-name: MARK-139 Linear Superpowers compositional skill
license: "MIT"
---
# Linear Superpowers

Use this skill when Linear work needs to be shaped around the smallest applicable workflow skill instead of a generic catch-all packet.

## Core job

Shape boring, worker-send-ready Linear issues and issue tracks so they say:

1. which workflow skill is the smallest applicable fit;
2. why that skill applies;
3. what evidence will prove the workflow was followed.

## Composition

Start with `/using-superpowers` as the workflow-selection entrypoint.

When the Linear packet is plan-shaped and meant for a worker, instruct the packet to use `/writing-plans` for route review and `/executing-plans` as the outer execution workflow.

Use `/connector-safety` as mandatory for Linear connector writes and blocked-write recovery, including issue create or update, comments, status changes, labels, relations or blockers, documents, assignments, and project moves. This is not optional background guidance.

Use `/linear-issue-compactor` when the issue body is long, dense, connector-hostile, or when source seams, plans, guardrails, validation, coverage maps, or evidence should move to attached Linear docs. Do not duplicate the full compactor procedure here.

Use `/unslop-superpowers` when the Linear packet needs repo-specific anti-slop controls, profile-aware non-goals, or evidence requirements.

Nesting rule:

- pick the smallest specialist workflow that actually fits;
- use TDD, debugging, verification, or closeout skills only when they are the smallest applicable specialist workflow;
- do not stack skills just because they are available.

## Linear shaping rules

- Shape one boring Linear issue or one boring parent tracker at a time.
- For parent trackers, require a comprehensive parent DOD.
- For child tracks, require each child to own a slice of the parent DOD as its own DOD.
- Check that the children collectively cover the parent DOD.
- Prefer vertical slices of provable value over horizontal component slices.
- Require read-before-write on the smallest relevant Linear surface when practical.
- Prefer one Linear side effect per call.
- Keep issue create, update, comment, status, label, project, and relation payloads narrow.
- Treat blocked connector writes as a signal to narrow, verify, or stop, not as completion.
- Do not claim a Linear mutation succeeded unless the connector result or readback proves it.

## Native Linear delegation guard

Worker-send-ready issue creation is not Linear native delegation. Do not set Linear connector `delegate` or agent delegation fields unless the user explicitly asks to use Linear's native delegation mechanism for that issue.

Ambiguous phrases such as "dispatch a worker", "send to a worker", or "worker issue" should be treated as issue-shaping intent unless the user clearly authorizes Linear native delegation.

Codex-from-Linear delegation is not part of the default workflow until separately promoted.

## Authority split

This skill shapes Linear work packets and workflow instructions.

It does not dispatch Codex workers, prove GitHub or source state, or claim execution, publication, merge, or closeout.

## Phrase to preserve

Use the base phrase `vertical slices of provable value`.

Project-specific variations, such as Wild Bunch `vertical slices of playable value`, are extension examples, not the default rule.
