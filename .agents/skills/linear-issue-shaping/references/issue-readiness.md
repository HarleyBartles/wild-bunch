# Linear Issue Readiness

Read when creating or updating a Linear issue for future worker execution.

A Linear issue is worker-ready when a future execution actor can read it and know:

- repository or implementation surface;
- exact goal as observable state;
- in-scope changes;
- out-of-scope/protected surfaces;
- validation commands or acceptable validation evidence;
- expected return evidence;
- publication or PR expectations, if any;
- GREEN/AMBER/RED/BLOCKED criteria when useful.

Do not require YAML unless the target worker or user explicitly asks for it. Boring means executable, bounded, and falsifiable, not verbose.

## Issue-type classification

Classify before shaping so the issue gets the right size and return contract. Read `campaign-shape.md` for the full profile.

- `small worker-ready issue`: one bounded Linear issue with a compact DOD and standard worker return.
- `campaign issue`: one durable Linear parent issue, Linear documents as lane/subtask packets where a chunky campaign needs multiple seams, one PR unless a split condition triggers, and stronger return evidence.
- `planning/tracker issue`: parent/tracker or planning-only issue, no execution yet.
- `gpt-native skillwork`: GPT-native skill author/edit/package work. Route to a worker execution lane only when the editable source is repo-backed and the issue explicitly targets that repo.
- `non-repo/manual work`: UI, connector, account, research, or manual action with no PR.

Do not overgrow a small worker issue into campaign shape. Do not route GPT-native skillwork to a worker merely because it touches skill text.

## Durable MARK worker issue convention

For MARK-style worker child issues, preserve this durable Linear shape:

```text
Worker child send-ready = Todo + assigned to Harley + WORKER label + shaped DOD/validation + no running evidence.
Worker child active/running = In Progress + assigned to Harley + WORKER label + durable Linear comments, attachments, or links showing actual work evidence.
Parent/tracker planned = Todo when shaped but no child work is active yet.
Parent/tracker active = In Progress when at least one child is active/running or the parent itself is actively being worked.
```

`worker-send-ready`, `worker ready`, `send ready`, and similar wording are prose hints only. They do not prove that work has started. Verify worker readiness or activity from Linear state, assignee, labels, child issue state, comments, attachments, links, and GitHub evidence where relevant.

## Compact issue shape

Use ordinary markdown headings:

- Problem
- Goal
- Scope
- Guardrails
- Validation
- Return evidence
- Success criteria

For small tasks, collapse headings into concise paragraphs.

## Worker lane wording

Use lightweight lane wording only when it changes execution:

- `worker-ready`: issue is clear enough for a future worker.
- `planning-only`: do not implement yet.
- `native-gpt-route`: belongs to ChatGPT skill, connector, UI, research, or packaging work rather than repo work.
- `external-worker-handoff`: user wants paste-ready text for a worker outside this chat.

Do not name or imply an execution provider unless the user explicitly names one.

## Publication wording

When PR publication is expected, include:

`When implementation is complete, return evidence in Linear, including validation output and any PR/branch/commit link if one is created. Do not require hidden credentials or unmentioned publication routes.`

## Campaign issue shape

When the issue is a campaign issue (chunky repo campaign), read `campaign-shape.md` and add to the issue body:

- campaign shape note: one PR preference and lane-document option;
- at least one split condition;
- the worktree isolation gate (see below);
- stronger return evidence: branch, PR URL, final head SHA, changed files, validation output, generated-artifact explanation, and blockers/readiness.

Prefer one durable Linear parent issue with Linear documents as lane/subtask packets over spawning many child issues by default.

## Worktree isolation gate

For any repo-backed task, the issue body must include the worktree isolation gate. Before mutation, the worker must work in a fresh dedicated worktree based on current `main` or the issue-specified base, and report:

- worktree path;
- branch name;
- base commit;
- `git status --short` before mutation;
- whether any pre-existing dirty state was present.

Pre-existing dirty state must be reported, not overwritten.

Suggested issue-body wording:

`Before mutation, work in a fresh dedicated worktree based on current main (or the issue-specified base). Report worktree path, branch name, base commit, git status --short before mutation, and whether any pre-existing dirty state was present. Do not overwrite pre-existing dirty state.`
