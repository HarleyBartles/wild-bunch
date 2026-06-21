---
name: session-buster
description: create compact YAML continuity exports for future ChatGPT sessions while avoiding stale-state laundering. use when Harley asks for a session buster, continuity export, handoff, next-session block, or session closeout. when a dominant handoff focus exists, include a suggested next session name for navigation only. for coding work, preserve durable Linear issue IDs, Codex state, PR IDs, and next verification checks instead of bulky dispatch packets; Linear/Codex is the normal workflow surface and session busters are fallback continuity.
metadata:
  source-id: session-buster
  source-path: sources/first_party/skills/session-buster/SKILL.md
  provenance-name: "MARK-9 chunk ledger \xC3\xA2\xE2\u201A\xAC\xE2\u20AC\x9D base and control plane"
license: "MIT"
---
# Session Buster

## Purpose

Produce a machine-ingestable continuity export for the next ChatGPT session.

`session-buster` is fallback continuity. It is not live source truth, not worker execution, not repo publication proof, not task authority, and not the normal coding control plane when Linear/Codex/GitHub records exist.

## Core lesson

Session busters are useful when a session is ending, but they are not free. The failure mode is continuity laundering: a tidy handoff sounds authoritative, so the next GPT treats stale chat memory as current issue, repo, PR, package, worker, or source state.

For coding work, prefer durable product surfaces over chat memory:

- Linear issue/body/comments/attachments for task and Codex worker event state.
- Codex task links from Linear for the human `Create PR` gate.
- GitHub PR/commit/status/main for repo proof.
- Session buster only as an index of durable IDs and first checks.

When a handoff has a dominant next-step focus, add `suggested_next_session_name` so the next session has a clean navigation label. Treat that field as metadata only. It can steer the next session's reading order, but it does not establish truth, authority, priority, or task scope.

## Lawful YAML use

Session-buster YAML is a lawful control-plane artifact distinct from dispatch YAML.

Use `artifact_type: session_buster`. A session buster must never be executable as a worker packet, Linear issue body, or Codex instruction pack.

## When to use

Use when Harley asks for a session buster, continuity export, handoff block, next-session prompt, or session closeout.

When asked, stop ordinary forward work and produce the continuity block instead. Do not create disk handoff files or mutate external systems unless Harley explicitly asks for that separate artifact.

## Coding continuity after Linear/Codex adoption

For coding work, do not carry heavy dispatch packets, branch anxiety, or repeated worker reports in the buster. Linear/Codex is the normal durable workflow.

Record only:

- Linear issue IDs and project/team when relevant.
- Codex state if known: `planned`, `delegated/running`, `returned/pr-gate`, `pr-created`, `landed`, or `unknown_recheck_linear`.
- PR numbers/URLs if Linear already shows a PR attachment or `Created pull request` comment.
- The next Linear or GitHub query the next session should run.
- Any blocked external action that needs Harley, such as opening the Codex task link and clicking `Create PR`.

If a Codex completion comment exists in Linear but no PR attachment/comment exists, record `returned/pr-gate` and tell the next session to ask Harley to use the Codex task link in Linear and click `Create PR`. Do not debug shell GitHub credentials as the default response.

If a PR attachment/comment exists, record `pr-created` and tell the next session to verify the GitHub PR, not reread old dispatch doctrine.

## Golden gate for buster content

Before adding a work item to `current_running_work`, `open_queue`, or `recommended_next_action`, classify its real surface:

- `linear_codex_default`: coding task with Linear issue/Codex path.
- `gpt_native_skillwork`: native skill/package work; route through skill-creator/validator/packager/buster, not Codex Cloud unless the editable source is known to be repo-backed.
- `repo_backed_codex_candidate`: repo file/doc/setup work that Codex Cloud can actually edit and publish.
- `legacy_plan_b`: Linear/Codex unavailable or explicitly not in use.
- `ordinary_chat`: no buster-worthy state.

This prevents converting GPT-native skill work, UI setup, research, or connector configuration into a fake Codex Cloud dispatch.

## Durable handoff cadence

Before writing a large buster, identify any session-held planning context that belongs durably on an existing Linear issue, PR, or other project record instead of only inside the continuity export. Examples include issue-specific implementation strategy, closeout criteria, worker-return follow-up, review findings, and future constraints.

Do not mutate Linear, GitHub, repo files, labels, comments, calendars, email, or any other external surface merely because a buster was requested. Durable handoff is allowed only when the latest user instruction authorizes posting or updating that surface, or when the user has already explicitly asked in the current turn flow to land that exact planning context. If authority is absent, draft the proposed durable handoff text inside the buster under `pending_durable_handoffs` and mark it `not_posted_no_latest_authority`.

When authorized handoff is needed, use the owning issue/project tool and current source route before the buster. Post or draft compact, issue-scoped comments. Then keep the buster as an index: name the issue, comment/status, short summary, and first verification checks rather than duplicating the full comment body.

## Mandatory ingress directive

Every session buster must include a top-level `mandatory_ingress` field telling the next session to process the block with `session-buster-ingress` after any required project bootstrap and before acting on next-task fields.

Use wording close to:

```yaml
mandatory_ingress: >
  Process this handoff with session-buster-ingress after any project bootstrap and before acting on next-task fields.
  Treat the block as fallback continuity until partitioned into verified, fallback-only, unverified, unavailable,
  contradicted, and blocked state.
```

## Required output shape

Use one YAML block unless the user asks otherwise. Include relevant sections:

- `artifact_type: session_buster`
- `purpose`
- `generated_by`
- `date_context`
- `suggested_next_session_name` when the handoff has a dominant next-step focus
- `continuity_mode`
- `handoff_chain`
- `verification_note`
- `source_routes_available`
- `verified_state`
- `fallback_or_session_derived_state`
- `durable_context_handoff` or `pending_durable_handoffs` when planning context was posted, drafted, or intentionally not offloaded
- `linear_codex_state` for coding work when relevant
- `staleness_check`
- `completeness_check`
- `current_running_work`
- `recent_completed_work`
- `open_queue`
- `pending_decisions`
- `next_session_first_checks`
- `do_not_assume`
- `mandatory_ingress`
- `recommended_next_action`

For Rooms, Mostly, consult `references/rooms-mostly-session-pattern.md` only when the session involves Rooms.

## Source route handoff

When repo, Linear, or connector state matters, record the route actually available:

- Linear issue/comment/attachment routes for task and Codex state.
- GitHub API routes for exact PRs, issues, comments, files, commits, statuses, compares, and authorized mutations.
- Indexed file search for uploaded/reference files only when applicable.
- Unavailable, unbound, or source-scope-limited routes.

Separate `verified_live`, `reported_not_verified`, `fallback_only`, `connector_unavailable`, `local_tree_unverified`, and `search_only_not_grounded` state.

## First-next-action clarity

`recommended_next_action` must name the first concrete next issue, PR, task, route, or check when known. Avoid vague endings such as `continue the work` when a Linear issue ID, PR number, queue item, source route, durable comment, or verification gate is known.

For coding work, a good first check is usually one of:

- `fetch Linear issue <ID> and list comments/attachments`;
- `if no PR exists but Codex returned, ask Harley to open the Codex task link and click Create PR`;
- `fetch GitHub PR <N> and verify diff/status/mergeability`;
- `verify merged PR/main head`.

## Lessons and followups

Include `lessons_or_followups` only when the session contains evidence supporting a durable lesson, follow-up issue, or skill update. Name the evidence basis. Do not force lessons from trivial housekeeping.

## Boundaries

Do not claim worker execution, repo mutation, Linear status, issue closure, publication, package installation, or local tree cleanliness without evidence.

Do not let old handoffs override current Linear, GitHub, repo, connector, or installed-skill evidence.

Do not load more skills after this one unless a named unresolved decision is outside this skill's ownership. For coding continuity, prefer Linear/GitHub checks over old dispatch-skill reads.

Do not use a buster as creative source material unless Harley explicitly asks for a status, continuity, or handoff artifact.
