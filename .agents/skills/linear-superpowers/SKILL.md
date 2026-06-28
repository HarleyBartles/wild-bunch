---
name: linear-superpowers
description: Use when shaping Linear issues, issue tracks, and worker packets so they
  name the smallest applicable Superpowers workflow skill, explain why it applies,
  and name the evidence required to prove it was followed.
metadata:
  source-id: linear-superpowers
  source-path: sources/first_party/skills/linear-superpowers/SKILL.md
  provenance-name: Linear Superpowers first-party skill
  source-category: first_party
  status: active
  owner: Harley Bartles
  scope: Use when shaping Linear issues, issue tracks, and worker packets so they
    name the smallest applicable Superpowers workflow skill, explain why it applies,
    and name the evidence required to prove it was followed.
  use_when:
  - Use when shaping Linear issues, issue tracks, and worker packets so they name
    the smallest applicable Superpowers workflow skill, explain why it applies, and
    name the evidence required to prove it was followed.
  do_not_use_when:
  - Do not use when another more specific skill owns this task.
license: MIT
---
# Linear Superpowers

Use this skill when Linear work needs to be shaped around the smallest applicable workflow skill instead of a generic catch-all packet.

## Core job

Shape boring, worker-send-ready Linear issues and issue tracks so they say:

1. which workflow skill is the smallest applicable fit;
2. why that skill applies;
3. what evidence will prove the workflow was followed.
4. what durable route-state block the worker must read before any implementation lane is selected.

## Worker route state

Use this compact block when a worker packet needs durable route state:

```text
## Worker route state
Route status: preflight-needed | preflight-complete-pending-approval | approved-plan-execution-ready | stale-plan-repair-needed | blocked-ambiguous | executed | superseded
Plan PR: none | <url>
Plan repo path: none | .agents/docs/superpowers/plans/<file>.md
Plan approved: yes | no | unknown
Plan merged to main: yes | no | unknown
Approved plan commit: none | <sha>
Last staleness check: none | <sha/date/result>
Execution PR: none | <url>
```

## Composition

Start with `/using-superpowers` as the workflow-selection entrypoint.

When the Linear packet is plan-shaped and meant for a worker, keep the route-state block as the compact control/index surface and hand the packet to `/using-superpowers` for lane choice. Do not make this skill choose between planning and execution lanes itself.

If the route state is `stale-plan-repair-needed` and the drift is repairable within the approved scope, repair the repo-resident plan in the execution branch, keep the route-state block current, and continue execution in the same PR. If the drift changes scope or makes execution unsafe, stop and request human review.

Every execution PR must include the updated repo-resident plan file with checked boxes. The plan file is the execution receipt: if the plan was fresh, include the checked-off plan; if the plan was stale, include the repaired plan plus implementation.

Use `/connector-safety` as mandatory for Linear connector writes and blocked-write recovery, including issue create or update, comments, status changes, labels, relations or blockers, documents, assignments, and project moves. If a Linear write is blocked, rejected, safety-filtered, permission-rejected, schema-rejected, or validation-rejected, route into `/connector-safety` immediately instead of retrying from memory or paraphrasing the same payload.

For any mention of Linear `delegate` or `!`-prefixed labels, defer to `/linear` as the owning connector surface and keep this skill focused on packet shape.

For normal Linear packets that are long, dense, connector-hostile, or need attached docs for source seams, plans, guardrails, validation, coverage maps, or evidence, compact the issue directly in place and keep the issue body as the TOC/control surface. For worker-ready repo tasks, route the packet through `linear-issue-shaping` and follow its compact worker issue-shape reference. Do not duplicate the compact issue-shape procedure here.

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
- After a plan merges, plan-only PRs and implementation PRs are separate by default unless the issue explicitly authorizes a combined PR.
- Keep issue create, update, comment, status, label, project, and relation payloads narrow.
- Treat blocked connector writes as a signal to narrow, verify, or stop, not as completion.
- If a Linear write blocks, stop and use `/connector-safety` recovery before any retry.
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
