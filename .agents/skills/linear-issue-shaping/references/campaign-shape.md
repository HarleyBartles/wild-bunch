# Campaign-Shaped Repo Issue Shape

Read when shaping a Linear issue for a chunky repo campaign, drafting a paste-ready launch handoff or resume nudge, or deciding whether a request is a small worker issue versus a campaign issue.

Campaign-shaped repo work is not the old narrow single-worker lane. A worker can handle chunky repo campaigns, many related subtasks in one PR, and subagent-style work where available. Linear issues meant for campaigns should be shaped for campaigns: one coherent parent issue, Linear documents as lane packets where useful, and one PR unless there is a real size/risk reason to split.

Do not encode "a worker can do anything." Workers remain bounded by issue scope, repo access, protected surfaces, validation, PR proof, and publication rules.

## Issue-type classification

Classify before shaping so the issue gets the right size and return contract:

| Issue type | Shape | Default PR posture | Return evidence |
|---|---|---|---|
| small worker-ready issue | one bounded Linear issue, compact DOD | one PR | standard worker return |
| campaign issue | one durable Linear parent issue, Linear documents as lane/subtask packets where a chunky campaign needs multiple seams | one PR unless a split condition triggers | stronger return evidence (see below) |
| planning/tracker issue | parent/tracker or planning-only issue, no execution yet | no PR expected until children are shaped | planning note, no GREEN |
| GPT-native skillwork | GPT-native skill author/edit/package work | one PR only when the editable source is repo-backed and the issue explicitly targets that repo | skill-source return evidence |
| non-repo/manual work | UI, connector, account, research, or manual action | no PR | observable completion note |

Do not route GPT-native skillwork to a worker merely because it touches skill text. Route it to a worker only when the editable source is repo-backed and the issue explicitly targets that repo.

## Campaign issue profile

A campaign issue is a chunky repo campaign that a worker can execute as one coherent unit. Prefer:

- one durable Linear parent issue with a clear repo target and issue goal;
- Linear documents as lane packets where a chunky campaign needs multiple seams, instead of spawning many child issues by default;
- one coherent PR unless a split condition triggers;
- stronger return evidence than a small task;
- explicit split conditions if the campaign becomes too broad or risky.

### Required campaign issue body sections

A campaign issue body should include:

- Repo target and issue goal as observable state.
- In-scope seams and out-of-scope/protected surfaces.
- Campaign shape note: one PR preference and lane-document option.
- Worktree isolation gate (see below).
- Validation expectations.
- Stronger return evidence: branch, PR URL, final head SHA, changed files, validation output, generated-artifact explanation, and blockers/readiness.
- Split conditions: when the campaign must split into multiple PRs or follow-up issues.

### Split conditions

Split a campaign into multiple PRs or follow-up issues only when at least one is true:

- the single PR diff is too large for meaningful review;
- unrelated concerns are bundled that could land independently;
- a protected surface requires a separate review lane;
- validation for one seam depends on another seam landing first;
- the campaign crosses repo boundaries that require separate publication.

Record the split reason in the parent issue and link the follow-up issues.

## Mandatory worktree isolation gate

Before mutation, the worker must work in a fresh dedicated worktree based on current `main` or the issue-specified base. This is a hard gate, not a preference.

The launch or resume handoff must require the worker to report, before any file mutation:

- worktree path;
- branch name;
- base commit;
- `git status --short` before mutation;
- whether any pre-existing dirty state was present.

If pre-existing dirty state is present, the worker must report it and not overwrite it.

This gate must appear in:

- campaign-ready Linear issue bodies or issue-readiness templates;
- paste-ready launch handoffs;
- paste-ready resume nudges;
- any GPT-produced dispatch-like text for repo work;
- return-contract expectations.

## Launch handoff template

Use when a paste-ready launch handoff is needed for a repo campaign.

```text
Repo: <owner/repo>
Issue: <MARK-###> <title>
Base: current origin/main (or issue-specified base)

Worktree isolation gate (before mutation):
1. Create a fresh dedicated worktree from the base.
2. Report before any file mutation:
   - worktree path
   - branch name
   - base commit
   - git status --short before mutation
   - whether any pre-existing dirty state was present
3. Do not overwrite pre-existing dirty state. Report it.

Goal: <observable state>
Scope: <in-scope seams>
Protected: <out-of-scope/protected surfaces>
Campaign shape: one PR unless a split condition triggers. Use Linear documents as lane packets if the campaign needs multiple seams.

Validation: <commands>
Return evidence: branch name, PR URL, final head SHA, changed files, validation commands/results, generated-artifact explanation, blockers/readiness.
```

## Resume nudge template

Use when resuming a paused repo campaign. The worktree isolation gate runs before any new mutation.

```text
Resuming <MARK-###> in <owner/repo>.

Before any new mutation, confirm the worktree isolation gate:
- worktree path
- branch name
- base commit
- git status --short before mutation
- whether any pre-existing dirty state was present

If the existing worktree is dirty or on the wrong base, report it before continuing. Do not overwrite dirty state.

Continue toward: <remaining goal>
Then return: branch, PR URL, final head SHA, changed files, validation output, generated-artifact explanation, blockers/readiness.
```

## Self-checks

Before claiming a campaign issue or handoff is ready, verify:

1. Campaign shape present: the issue body includes a one-PR preference, a lane-document option, and at least one split condition.
2. Worktree isolation gate present in the issue body or issue-readiness template.
3. Worktree isolation gate present in any paste-ready launch handoff.
4. Worktree isolation gate present in any paste-ready resume nudge, before mutation.
5. Small worker issue shaping still works and is not overgrown into campaign shape: a small bounded issue keeps the compact DOD and standard return contract.
6. GPT-native skillwork is not routed to a worker merely because it touches skill text, unless the editable source is repo-backed and the issue explicitly targets that repo.
