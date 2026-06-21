# Devin Campaign Issue Shape

Read when shaping a Linear issue for a Devin-backed repo campaign, drafting a paste-ready Devin launch handoff or resume nudge, or deciding whether a request is a small worker issue versus a Devin campaign.

Devin is not the old narrow single-worker lane. Devin can handle chunky repo campaigns, many related subtasks in one PR, and subagent-style work where available. Linear issues meant for Devin should be shaped for Devin: one coherent parent issue, Linear documents as lane packets where useful, and one PR unless there is a real size/risk reason to split.

Do not encode "Devin can do anything." Devin remains bounded by issue scope, repo access, protected surfaces, validation, PR proof, and publication rules.

## Issue-type classification

Classify before shaping so the issue gets the right size and return contract:

| Issue type | Shape | Default PR posture | Return evidence |
|---|---|---|---|
| small worker-ready issue | one bounded Linear issue, compact DOD | one PR | standard worker return |
| Devin campaign issue | one durable Linear parent issue, Linear documents as lane/subtask packets where a chunky campaign needs multiple seams | one PR unless a split condition triggers | stronger return evidence (see below) |
| planning/tracker issue | parent/tracker or planning-only issue, no execution yet | no PR expected until children are shaped | planning note, no GREEN |
| GPT-native skillwork | GPT-native skill author/edit/package work | one PR only when the editable source is repo-backed and the issue explicitly targets that repo | skill-source return evidence |
| non-repo/manual work | UI, connector, account, research, or manual action | no PR | observable completion note |

Do not route GPT-native skillwork to Devin merely because it touches skill text. Route it to Devin only when the editable source is repo-backed and the issue explicitly targets that repo.

## Devin campaign issue profile

A Devin campaign issue is a chunky repo campaign that Devin can execute as one coherent unit. Prefer:

- one durable Linear parent issue with a clear repo target and issue goal;
- Linear documents as lane/subtask packets where a chunky campaign needs multiple seams, instead of spawning many child issues by default;
- one coherent PR unless a split condition triggers;
- stronger return evidence than a small task;
- explicit split conditions if the campaign becomes too broad or risky.

### Required campaign issue body sections

A Devin campaign issue body should include:

- Repo target and issue goal as observable state.
- In-scope seams and out-of-scope/protected surfaces.
- Campaign shape note: one PR preference and lane-document option.
- Worktree isolation gate (see below).
- Validation expectations.
- Stronger return evidence: branch, PR URL, final head SHA, changed files, validation output, generated-artifact explanation, and blockers/readiness.
- Split conditions: when the campaign must split into multiple PRs or follow-up issues.

### Split conditions

Split a Devin campaign into multiple PRs or follow-up issues only when at least one is true:

- the single PR diff is too large for meaningful review;
- unrelated concerns are bundled that could land independently;
- a protected surface requires a separate review lane;
- validation for one seam depends on another seam landing first;
- the campaign crosses repo boundaries that require separate publication.

Record the split reason in the parent issue and link the follow-up issues.

## Mandatory Devin worktree isolation gate

Before mutation, Devin must work in a fresh dedicated worktree based on current `main` or the issue-specified base. This is a hard gate, not a preference.

The launch or resume handoff must require Devin to report, before any file mutation:

- worktree path;
- branch name;
- base commit;
- `git status --short` before mutation;
- whether any pre-existing dirty state was present.

If pre-existing dirty state is present, Devin must report it and not overwrite it.

This gate must appear in:

- Devin-ready Linear issue bodies or issue-readiness templates;
- paste-ready Devin launch handoffs;
- paste-ready Devin resume nudges;
- any GPT-produced dispatch-like text for Devin repo work;
- return-contract expectations.

## Devin launch handoff template

Use when Harley asks for a paste-ready Devin launch handoff for a repo campaign.

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

## Devin resume nudge template

Use when resuming a paused Devin repo campaign. The worktree isolation gate runs before any new mutation.

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

Before claiming a Devin campaign issue or handoff is ready, verify:

1. Campaign shape present: the issue body includes a one-PR preference, a lane-document option, and at least one split condition.
2. Worktree isolation gate present in the issue body or issue-readiness template.
3. Worktree isolation gate present in any paste-ready Devin launch handoff.
4. Worktree isolation gate present in any paste-ready Devin resume nudge, before mutation.
5. Small worker issue shaping still works and is not overgrown into campaign shape: a small bounded issue keeps the compact DOD and standard return contract.
6. GPT-native skillwork is not routed to Devin merely because it touches skill text, unless the editable source is repo-backed and the issue explicitly targets that repo.

If any check fails, fix the issue or handoff text before returning it as ready.
