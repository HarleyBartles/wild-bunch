---
name: worker-dispatch-linear
description: Use for Linear-backed worker issue preparation and status handling: create or update worker-ready Linear issues, inspect Linear comments/attachments/state, prepare paste-ready worker handoffs when explicitly requested, and route GitHub PR proof after a PR exists. Do not launch, delegate, or assume any execution lane; treat worker-ready as issue-ready only.
metadata:
  source-id: worker-dispatch-linear-v2
  source-path: sources/first_party/skills/worker-dispatch-linear/SKILL.md
  provenance-name: "MARK-122 GPT-native update"
license: "MIT"
---
# Worker Dispatch Linear

Use this skill as the GPT-wide control plane for Linear-backed worker readiness and worker event-log handling.

This skill does not launch workers, delegate execution, assume a worker provider, or treat any execution lane as available. It shapes durable Linear issue contracts and reads Linear state. A `worker-ready` issue is ready for a future execution actor to pick up; it is not proof that a worker has been sent.

## Core rule

Linear is the durable issue/control plane. The boring default is:

1. create or update a worker-ready Linear issue;
2. inspect Linear comments, attachments, assignee, labels, and status when checking progress;
3. prepare a paste-ready worker handoff only when Harley explicitly asks for one;
4. switch to GitHub proof only after a GitHub PR, branch, commit, or URL exists;
5. never claim execution, publication, merge, or closeout unless the target system proves it.

## Linear Worker Issue Shaping Stack

When a Linear issue is intended to become worker-send-ready for repo or code execution, always compose this stack:

```text
worker-dispatch-linear -> boring-buster -> writing-plans -> worker-dispatch-linear
```

Use this skill first to fetch or create the durable Linear issue surface, classify the lane, and preserve the Linear state convention.

Use `boring-buster` to decide whether the issue is bounded, lawful, route-suitable, and boring enough for the selected worker lane.

Use `writing-plans` to check or repair the implementation-plan shape: one observable goal, likely files or source seams, small executable steps or chosen implementation route, explicit validation commands, no placeholders, and no hidden replanning requirement.

Return to this skill after those gates to write or update the Linear issue only when the latest user turn authorizes mutation.

Do not require this full stack for parent trackers, product notes, research/discovery issues, or planning-only issues unless Harley asks to make them worker-send-ready.

## Issue-type classification

Classify the request before shaping so it gets the right size and return contract. Read `references/devin-campaign-shape.md` for the full profile.

- `small worker-ready issue`: one bounded Linear issue with a compact DOD and standard worker return.
- `devin campaign issue`: one durable Linear parent issue with a clear repo target, Linear documents as lane/subtask packets where a chunky campaign needs multiple seams, one PR unless a split condition triggers, and stronger return evidence.
- `planning/tracker issue`: parent/tracker or planning-only issue, no execution yet.
- `gpt-native skillwork`: GPT-native skill author/edit/package work. Route to Devin only when the editable source is repo-backed and the issue explicitly targets that repo.
- `non-repo/manual work`: UI, connector, account, research, or manual action with no PR.

Do not route GPT-native skillwork to Devin merely because it touches skill text. Do not encode "Devin can do anything." Devin remains bounded by issue scope, repo access, protected surfaces, validation, PR proof, and publication rules.

## Devin worktree isolation gate

For any Devin-backed repo task, the issue body, launch handoff, resume nudge, and return contract must require Devin to work in a fresh dedicated worktree based on current `main` or the issue-specified base before mutation. Read `references/devin-campaign-shape.md` for the exact gate language and templates.

The gate requires Devin to report, before any file mutation:

- worktree path;
- branch name;
- base commit;
- `git status --short` before mutation;
- whether any pre-existing dirty state was present.

Pre-existing dirty state must be reported, not overwritten.

## Durable Linear state convention

Preserve this convention when shaping, updating, or interpreting MARK-style worker issues:

- Worker child send-ready: `Todo` + assigned to Harley + `WORKER` label + shaped DOD/validation + no running evidence.
- Worker child active/running: `In Progress` + assigned to Harley + `WORKER` label + durable Linear comments, attachments, or links showing actual work evidence.
- Parent/tracker planned: `Todo` when shaped but no child work is active yet.
- Parent/tracker active: `In Progress` when at least one child is active/running or the parent itself is actively being worked.

Do not infer active/running state from phrases such as `worker-send-ready`, `worker ready`, or `send ready`. Check Linear state, assignee, labels, child issue state, comments, attachments, links, and GitHub evidence where relevant.

## Route classification

Classify the latest request before acting:

- `issue_shape`: create or update a Linear issue so a future worker can execute it. Classify the issue type first (see Issue-type classification above).
- `devin_campaign_shape`: shape a chunky repo campaign as a Devin campaign issue with one PR preference, lane-document option, split conditions, and the worktree isolation gate. Read `references/devin-campaign-shape.md`.
- `worker_handoff_text`: draft a paste-ready worker handoff without mutating execution state. For Devin repo work, include the worktree isolation gate.
- `status_check`: inspect Linear issue state, comments, and attachments.
- `pr_verification`: inspect GitHub only after a PR URL/number, branch, commit, or merged state exists.
- `native_or_planning`: route to the relevant GPT-native, connector, planning, or skill-maintenance path.

Phrases such as `worker ready`, `worker send ready`, `send-ready issue`, `worker-ready`, `make it boring`, or `make it executable` authorize issue shaping only. They do not authorize launching, assigning to an execution lane, or claiming that a worker is running.

## Normal workflow

1. For issue creation or update, read `references/issue-readiness.md` and make the issue boring enough for a future worker. For a Devin campaign issue, also read `references/devin-campaign-shape.md` and include the campaign shape, lane-document option, one-PR preference, split conditions, and worktree isolation gate.
2. For status pickup, read `references/state-machine.md`, fetch Linear state first, then decide whether GitHub proof is available.
3. For paste-ready external handoff text, read `references/external-worker-handoff.md` and produce a compact handoff without mutating repo or issue state unless separately authorized. For Devin repo work, include the worktree isolation gate in the launch handoff and resume nudge.
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
- Devin campaign issue profile, worktree isolation gate, launch/resume templates, and self-checks: read `references/devin-campaign-shape.md`;
- implementation-plan shape for worker coding issues: use `writing-plans`;
- skill creation/update/package work: use the skill-maintenance stack;
- GitHub PR/repo proof: use GitHub verification tooling;
- validation choice after code/PR/package evidence exists: use validation guidance;
- project-specific domain constraints: use only the matching project wrapper.

If Harley says the route is too wide, wrong, or not boring, stop expanding the skill set and return to Linear issue state plus the smallest next safe action.
