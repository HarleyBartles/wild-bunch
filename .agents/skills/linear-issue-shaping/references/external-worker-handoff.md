# External Worker Handoff

Read only when your human partner explicitly asks for paste-ready worker instructions, a worker dispatch text, or a handoff that will be copied outside this chat.

This reference prepares text only. It does not launch a worker, assign a task, publish a branch, or mutate repository state.

## Handoff shape

Create the smallest durable handoff that names:

- target repo or surface;
- issue identifier or source context;
- goal as observable state;
- in-scope files or areas;
- out-of-scope/protected surfaces;
- validation expectations;
- required return evidence;
- publication expectation, if any.

Avoid large YAML packets unless the target worker requires that exact format. Ordinary markdown is preferred.

## Campaign launch handoff

For a repo campaign, read `campaign-shape.md` for the full template. The launch handoff must include the worktree isolation gate before mutation:

```text
Worktree isolation gate (before mutation):
1. Create a fresh dedicated worktree from current main (or the issue-specified base).
2. Report before any file mutation:
   - worktree path
   - branch name
   - base commit
   - git status --short before mutation
   - whether any pre-existing dirty state was present
3. Do not overwrite pre-existing dirty state. Report it.
```

For a campaign issue, also include the campaign shape note (one PR preference, lane-document option) and at least one split condition.

## Campaign resume nudge

When resuming a paused repo campaign, the resume nudge must repeat the worktree isolation gate before any new mutation. Read `campaign-shape.md` for the full template. If the existing worktree is dirty or on the wrong base, the nudge must tell the worker to report it before continuing and not overwrite dirty state.

## Return evidence wording

Require the worker to return:

- branch name and commit SHA, if code changed;
- PR URL, if published;
- files changed summary;
- validation commands run and outputs;
- skipped validation with reason;
- known blockers or ambiguity.

For a repo task, also require:

- worktree path, branch name, base commit, and `git status --short` before mutation;
- whether any pre-existing dirty state was present;
- final head SHA;
- generated-artifact explanation when generated artifacts changed;
- blockers/readiness statement.
