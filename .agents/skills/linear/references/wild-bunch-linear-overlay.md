# Wild Bunch Linear overlay rules

Read this reference before using Linear to coordinate Wild Bunch repo issues, roadmap work, dispatch prep, closure posture, or retirement of the GitHub label taxonomy.

## Source-truth separation

Wild Bunch repo/source truth is the current `main` branch of `HarleyBartles/wild-bunch` unless Harley explicitly says otherwise.

Linear can be used as planning/control-plane truth only for objects that are actually present and verified in Linear. Session busters, worker reports, issue comments, and chat summaries remain fallback context only.

## GitHub and Linear split

Use GitHub/live repo routes for:

- current source state;
- commits, files, branches, PRs, and validation evidence;
- proof that code landed;
- issue closure evidence when GitHub remains the authoritative issue surface.

Use Linear for:

- queue shape;
- issue planning and prioritization;
- roadmap/project/initiative grouping;
- issue relationship exploration;
- durable control-plane notes;
- making work boring enough to dispatch or implement.

## Dispatch posture

Do not dispatch from Linear state alone when repo/source truth matters. A Linear issue can define the work, but the dispatch must still preserve Wild Bunch mainline posture and require repo validation.

If a worker is already running, do not issue more instructions unless Harley asks for a bounded nudge or live worker thoughts show material drift.

## Taxonomy migration caution

The old GitHub labels (`must`, `should`, `could`, `now`, `next`, `later`, `feature`, `system`, `tooling`, `boring`) may become unnecessary if Linear projects, initiatives, priorities, statuses, milestones, cycles, and views cover the same decisions.

Do not retire the label taxonomy globally until:

1. imported issue behavior is stable;
2. GitHub sync/writeback is understood;
3. Linear-native mappings have been tested on a bounded sample;
4. Harley approves the cleanup/migration posture.

Mirror first, then remove.

## Experiment safety

For Linear experiments touching imported GitHub issues:

- use clearly visible test names or prefixes;
- keep the sample small;
- do not delete or archive imported issues;
- report exact undo paths;
- verify after mutation;
- preserve GitHub backlinks.

## Failure-sidequest control

Linear may be used to park GPT/control-plane failure modes without turning them into immediate process-repair side quests. Capture significant failures durably, decide whether the active task is unsafe or blocked, and return to feature work when safe.
