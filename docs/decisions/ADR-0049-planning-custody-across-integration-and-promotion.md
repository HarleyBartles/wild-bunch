# ADR-0049 Planning custody across integration and promotion

## Status

`live`

## Dated History

- `2026-10-16` - Keep live planning on develop, retire completed artifacts before a cut, exclude planning from main, and restore the exact current integration planning snapshot during reconciliation.

## Decision Type

`operations`, `process`

## Related ADRs

- `related to`: [ADR-0037](ADR-0037-ambient-capabilities-opt-in-standards-and-decision-documentation.md)
- `clarifies`: [ADR-0048](ADR-0048-gitflow-and-semver-release-lifecycle.md)

## Context

Release cuts combine application changes and repository tooling with planning that belongs to continuing integration work. Next-slice retirement already prevents completed artifacts accumulating on develop. Main needs current product documentation and operating guidance without carrying active or completed planning documents.

## Decision

Plans, specifications, roadmaps and investigations live on develop while their scope remains live. Before cutting a promotion to main, read each artifact against its complete scope and current implementation/delivery evidence. Ask the maintainer about each unclear completion classification. Promote enduring decisions and operating/product knowledge to their owning ADRs, guidance and current explanations; consolidate useful deferred feature stories and open choices in the feature matrix. Commit eligible retirement to develop before the cut. Preserve the pinned semantic next-slice completed-artifact-custody lifecycle. Do not keep a tracked completion archive or substitute age, a marker, checkboxes or PR status for judgment.

Main's current tree contains no planning artifacts, including any already present before this decision. History is retained. Main-bound candidates omit live planning without declaring it abandoned on develop, and repair links to current durable owners or immutable historical sources. The executable guard checks all indexed paths under `.agents/plans/`, `.agents/specs/`, `.agents/roadmaps/` and `.agents/investigations/` for the actual target main. Review also rejects planning disguised under other names or homes. The guard cannot decide semantic completion.

Immediately before main-to-develop reconciliation, snapshot the current develop commit. Restore exactly its four planning homes into the merge result, including additions, edits and deletions since the cut. Compare the indexed result to that snapshot before committing. If develop advances, refresh the snapshot and equality verification. Do not resurrect an older cut's planning or rerun completion classification as merge conflict resolution.

Agent and repository tooling may be promoted separately from application behavior. The initial tooling promotion carries the command bus and required infrastructure together, uses a reviewed main-targeted PR and merge commit, then reconciles by merge commit. It creates no named product release, tag or GitHub Release. Product releases retain ADR-0048's qualification, identity and publication requirements.

## Consequences

Develop keeps usable live planning, main keeps product and operating truth, and Git preserves retired work. Promotions require semantic preflight and link repair. Reconciliation must account for planning work since the cut rather than restoring a stale filename list. Automated exclusion supports review without pretending to understand whether a programme is complete.
