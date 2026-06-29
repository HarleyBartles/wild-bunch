# Linear Worker State Machine

Read when checking worker-shaped issue status, resuming a session, handling a worker return, or deciding whether to verify a PR.

## Durable MARK state convention

Use this convention for MARK-style worker and tracker issues:

```text
Worker child send-ready = Todo + assigned to Harley + WORKER label + shaped DOD/validation + no running evidence.
Worker child active/running = In Progress + assigned to Harley + WORKER label + durable Linear comments, attachments, or links showing actual work evidence.
Parent/tracker planned = Todo when shaped but no child work is active yet.
Parent/tracker active = In Progress when at least one child is active/running or the parent itself is actively being worked.
```

Never infer active/running state from send-ready wording alone. Check Linear state, assignee, labels, child issue state, comments, attachments, links, and GitHub evidence where relevant.

## States

`planned`
: Linear issue exists but is not yet worker-ready. Next action: make it worker-ready or ask what should be clarified.

`worker-ready`
: Issue is shaped for a future worker, but no running evidence exists. In MARK-style child issues this normally means `Todo` + assigned to Harley + `WORKER` label + shaped DOD/validation. Next action: report that it is ready; do not claim it has been sent or started.

`active/running`
: Durable Linear state and event-log evidence indicate work is actually active. In MARK-style child issues this normally means `In Progress` + assigned to Harley + `WORKER` label + comments, attachments, or links showing work evidence. Next action: report observable state only.

`returned`
: A worker report, completion note, or validation summary exists. Next action: summarize claims and identify required proof or follow-up.

`pr-created`
: Linear has a PR attachment/comment/URL, or the user provides a PR. Next action: verify the GitHub PR diff, state, checks, and issue-goal conformance.

`landed`
: PR is merged and final main state is verified. Next action: report landed state; update or close Linear only when authorized by latest instruction or durable project policy.

`blocked`
: Required source, authority, access, validation, or publication proof is missing. Next action: name the blocker and the smallest safe next step.

## Parent/tracker state

A parent or tracker issue should be `Todo` when the track is shaped/planned and no child work is active. It should be `In Progress` when at least one child issue is active/running or the parent itself is actively being worked. Do not mark a parent active solely because children are worker-ready.

## Campaign issue state

A campaign issue is a chunky repo campaign shaped as one durable Linear parent issue. It may use Linear documents as lane/subtask packets instead of child issues by default. The campaign is `worker-ready` when the issue body includes the campaign shape, lane-document option, one-PR preference, split conditions, and the worktree isolation gate. It is `active/running` when the worker reports worktree setup and work evidence. It is `returned` when the worker reports a PR or completion note. Read `campaign-shape.md` for the full profile.

## Evidence order

For status checks, inspect Linear before GitHub:

1. issue fields: state, assignee, project, labels, links, attachments;
2. child issue state when judging parent/tracker state;
3. comments: worker reports, validation notes, PR links, blockers;
4. PR attachment/URL, if present;
5. GitHub PR only after a PR URL/number/branch/commit exists.

## Report shape

Use the smallest useful report:

- `Linear`: issue state and relevant comment/attachment signal.
- `Worker`: only what Linear or user-provided evidence supports.
- `GitHub`: PR/branch/commit state only when present.
- `Next gate`: exact next action.
