---
name: linear-issue-shaping
description: 'Use when Linear-backed issue, project, and document shaping: create
  or update worker-ready Linear issues, inspect Linear comments/attachments/state,
  prepare paste-ready worker handoffs when explicitly requested, and route GitHub
  PR proof after a PR exists. Do not launch workers, delegate execution, or assume
  any execution lane; treat worker-ready as issue-ready only.'
metadata:
  source-id: linear-issue-shaping
  source-path: sources/first_party/skills/linear-issue-shaping/SKILL.md
  provenance-name: Linear Issue Shaping first-party skill
  source-category: first_party
  status: active
  owner: Harley Bartles
  scope: 'Use when Linear-backed issue, project, and document shaping: create or update
    worker-ready Linear issues, inspect Linear comments/attachments/state, prepare
    paste-ready worker handoffs when explicitly requested, and route GitHub PR proof
    after a PR exists. Do not launch workers, delegate execution, or assume any execution
    lane; treat worker-ready as issue-ready only.'
  use_when:
  - 'Use when Linear-backed issue, project, and document shaping: create or update
    worker-ready Linear issues, inspect Linear comments/attachments/state, prepare
    paste-ready worker handoffs when explicitly requested, and route GitHub PR proof
    after a PR exists. Do not launch workers, delegate execution, or assume any execution
    lane; treat worker-ready as issue-ready only.'
  do_not_use_when:
  - Do not use when another more specific skill owns this task.
license: MIT
---
# Linear Issue Shaping

Use this skill as the GPT-wide control plane for Linear-backed issue, project, and document shaping and Linear event-log handling.

This skill does not launch workers, delegate execution, assume a worker provider, or treat any execution lane as available. It shapes durable Linear issue contracts and reads Linear state. A `worker-ready` issue is ready for a future execution actor to pick up; it is not proof that a worker has been sent.

## Connector-safe Linear shaping BAU

Use the connector itself to discover the bounded parent surface, read the exact target, perform one bounded mutation, rediscover the mutated thing from the parent or bounded search, and read back the freshly discovered target before the next mutation. That is the normal Linear shaping law, not just blocked-write recovery.

Material connector blocks are blocked, rejected, safety-filtered, permission-rejected, schema-rejected, or validation-rejected writes. Treat those as connector-safety recovery. Non-material friction is a narrower target, stale vocabulary, or missing parent binding; recover by narrowing the surface, splitting create-then-enrich work, or re-reading the freshly discovered target before deciding the write is blocked.

## Core rule

Linear is the durable issue/control plane. The boring default is:

1. create or update a worker-ready Linear issue;
2. inspect Linear comments, attachments, assignee, labels, and status when checking progress;
3. prepare a paste-ready worker handoff only when your human partner explicitly asks for one;
4. switch to GitHub proof only after a GitHub PR, branch, commit, or URL exists;
5. never claim execution, publication, merge, or closeout unless the target system proves it.

If a Linear write is blocked, rejected, safety-filtered, permission-rejected, schema-rejected, or validation-rejected, route the recovery into `/connector-safety` immediately. Do not paraphrase the payload from memory or retry the same mutation shape from the same surface.

Use the same discover/read/mutate/discover/readback loop for issue create or update, project moves, document creation or updates, milestone changes, relation or blocker changes, labels, statuses, comments, and assignee fields.

## Native delegation guard

Worker-ready and worker-send-ready issue shaping are not Linear native delegation. Do not set Linear `delegate` or any `!`-prefixed label unless your human partner explicitly asks for Linear native delegation to a named agent on that issue.

Do not infer delegation from `send`, `run`, `worker-ready`, `campaign-sized`, `start`, `worker`, or `agent`.

For ordinary worker issues, keep the safe default as `assignee: me`, with `delegate` omitted or null.

## Linear Worker Issue Shaping Stack

When a Linear issue is intended to become worker-send-ready for repo or code execution, always compose this stack:

```text
work-mode-router -> /using-superpowers -> linear-issue-shaping -> boring-buster
```

Use this skill first to fetch or create the durable Linear issue surface, classify the lane, and preserve the Linear state convention.

Use `boring-buster` to decide whether the issue is bounded, lawful, route-suitable, and boring enough for the selected worker lane.

Use the route-state block and compact packet shape to check or repair the implementation-plan shape: one observable goal, likely files or source seams, small executable steps or chosen implementation route, explicit validation commands, no placeholders, and no hidden replanning requirement. Keep plan PRs and implementation PRs distinct in the packet language.

For route-state blocks, allow investigation seams and understanding questions plus the compact route-state block. The route-state block is a control/index surface for workflow phase classification, not the implementation plan. Route-state blocks must not become the full implementation plan.

Approved plans live in the repo under `.agents/superpowers/plans/`. After a plan merges, plan-only PRs and implementation PRs are separate by default unless the issue explicitly authorizes a combined PR.

If the approved plan is stale but the drift is repairable and stays inside the approved scope, repair the repo-resident plan in the execution branch, keep the route-state block current, and include the repaired plan in the execution PR. If the drift changes scope materially, invalidates the approved direction, or makes execution unsafe, stop for human review.

Every execution PR must include the updated repo-resident plan file with checked boxes. If the plan was stale, the execution PR must include the repaired plan plus implementation. If the plan was fresh, the execution PR must still include the updated checked-off plan.

Return to this skill after those gates to write or update the Linear issue only when the latest instruction authorizes mutation.

Do not require this full stack for parent trackers, product notes, research/discovery issues, or planning-only issues unless your human partner asks to make them worker-send-ready.

## Compact Worker Issue Shape

For worker-ready implementation issues, keep the issue body compact and treat it as the control surface:

- goal and repo target stay in the issue body;
- dense scope, implementation detail, validation, and return evidence move into attached Linear documents;
- a compact route-state block is required for non-trivial repo/code issues to support workflow phase classification;
- the route-state block contains workflow phase markers (design_needed, planning_needed, etc.) and is used by work-mode-router to classify the current phase;
- do not put the full implementation plan, validation matrix, or dense evidence dump into the route-state block;
- do not use the route-state block as a readiness state or second plan;
- do not keep a separate compactor trigger for normal worker issue shaping.

Use `references/compact-issue-shape.md` for the full worker issue-shape pattern when preparing or reviewing a worker-ready Linear packet.

## Issue-type classification

Classify the request before shaping so it gets the right size and return contract. Read `references/campaign-shape.md` for the full profile.

- `small worker-ready issue`: one bounded Linear issue with a compact DOD and standard worker return.
- `campaign_shape`: one durable Linear parent issue with a clear repo target, Linear documents as lane/subtask packets where a chunky campaign needs multiple seams, one PR unless a split condition triggers, and stronger return evidence.
- `planning/tracker issue`: parent/tracker or planning-only issue, no execution yet.
- `gpt-native skillwork`: GPT-native skill author/edit/package work. Route to a worker execution lane only when the editable source is repo-backed and the issue explicitly targets that repo.
- `non-repo/manual work`: UI, connector, account, research, or manual action with no PR.

Do not route GPT-native skillwork to a worker execution lane merely because it touches skill text. Do not encode "a worker can do anything." Workers remain bounded by issue scope, repo access, protected surfaces, validation, PR proof, and publication rules.

## Worker worktree isolation gate

For any repo-backed task, the issue body, launch handoff, resume nudge, and return contract must require a fresh dedicated worktree based on current `main` or the issue-specified base before mutation. Read `references/campaign-shape.md` for the exact gate language and templates.

The gate requires the worker to report, before any file mutation:

- worktree path;
- branch name;
- base commit;
- `git status --short` before mutation;
- whether any pre-existing dirty state was present.

Pre-existing dirty state must be reported, not overwritten.

## Durable Linear state convention

Preserve this convention when shaping, updating, or interpreting MARK-style worker issues:

- Worker child send-ready: `Todo` + assigned to your human partner + `WORKER` label + shaped DOD/validation + no running evidence.
- Worker child active/running: `In Progress` + assigned to your human partner + `WORKER` label + durable Linear comments, attachments, or links showing actual work evidence.
- Parent/tracker planned: `Todo` when shaped but no child work is active yet.
- Parent/tracker active: `In Progress` when at least one child is active/running or the parent itself is actively being worked.

Do not infer active/running state from phrases such as `worker-send-ready`, `worker ready`, or `send ready`. Check Linear state, assignee, labels, child issue state, comments, attachments, links, and GitHub evidence where relevant.

## Route classification

Classify the latest request before acting:

- `issue_shape`: create or update a Linear issue so a future worker can execute it. Classify the issue type first (see Issue-type classification above).
- `campaign_shape`: shape a chunky repo campaign as a campaign issue with one PR preference, lane-document option, split conditions, and the worktree isolation gate. Read `references/campaign-shape.md`.
- `worker_handoff_text`: draft a paste-ready worker handoff without mutating execution state. For repo work, include the worktree isolation gate.
- `status_check`: inspect Linear issue state, comments, and attachments.
- `pr_verification`: inspect GitHub only after a PR URL/number, branch, commit, or merged state exists.
- `native_or_planning`: route to the relevant GPT-native, connector, planning, or skill-maintenance path.

Phrases such as `worker ready`, `worker send ready`, `send-ready issue`, `worker-ready`, `make it boring`, or `make it executable` authorize issue shaping only. They do not authorize launching, assigning to an execution lane, or claiming that a worker is running.

## Normal workflow

1. For issue creation or update, read `references/issue-readiness.md` and make the issue boring enough for a future worker. For a campaign-shaped repo issue, also read `references/campaign-shape.md` and include the campaign shape, lane-document option, one-PR preference, split conditions, and worktree isolation gate.
2. For status pickup, read `references/state-machine.md`, fetch Linear state first, then decide whether GitHub proof is available.
3. For paste-ready external handoff text, read `references/external-worker-handoff.md` and produce a compact handoff without mutating repo or issue state unless separately authorized. For repo work, include the worktree isolation gate in the launch handoff and resume nudge.
4. For GitHub PR, branch, commit, merge, or main-state proof, hand off to GitHub verification tooling after the GitHub artifact is known.
5. Stop when the issue is shaped, the status is reported, or the next proof surface is named. Do not invent an execution lane to continue.

## Linear as event log

Treat Linear issue body, comments, attachments, links, assignee, labels, and status as the event log for worker-shaped work.

Useful signals:

- issue exists but lacks scope/validation/return evidence: make it worker-ready;
- issue has worker report/comment but no PR evidence: report returned state and ask for or prepare the next explicit handoff;
- issue has PR attachment/comment/URL: verify the GitHub PR;
- PR merged and main verified: report landed state and update/close Linear only when authorized.

## GitHub boundary

GitHub proves repo facts: PR metadata, diff, statuses, review comments, merge state, commits, files, and main head. GitHub Issues are not the default control plane when Linear is available.

Do not use Linear comments, worker reports, validation summaries, local paths, or generated package names as proof of repository state. Use GitHub proof after a GitHub artifact exists.

## Skill-read stop rule

After this skill classifies the route, do not read old dispatch or issue-management skills merely for comfort. Load another skill only for a named unresolved decision that this skill does not own:

- worker-send-ready boring/readiness verdict: use `boring-buster`;
- campaign issue profile, worktree isolation gate, launch/resume templates, and self-checks: read `references/campaign-shape.md`;
- implementation-plan shape for worker coding issues: use the compact worker issue-shape reference and keep the route-state block explicit for workflow phase classification;
- skill creation/update/package work: use the skill-maintenance stack;
- GitHub PR/repo proof: use GitHub verification tooling;
- validation choice after code/PR/package evidence exists: use validation guidance;
- project-specific domain constraints: use only the matching project wrapper.

If your human partner says the route is too wide, wrong, or not boring, stop expanding the skill set and return to Linear issue state plus the smallest next safe action.
