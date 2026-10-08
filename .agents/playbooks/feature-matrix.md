# Feature Matrix Playbook

## When

Use this playbook when design, planning, implementation, review or release work adds, changes, splits, consolidates, defers or removes a product capability, changes its 0.1.0 disposition, or changes a cross-feature dependency or evidence assessment.

## Source of truth

The current capability promises, release dispositions and directed dependencies live in the [Wild Bunch Feature Matrix](../../docs/features.md). The stable 0.1.0 specification owns detailed settled gameplay and persistence rules. Source investigations preserve assessment evidence; plans sequence a bounded implementation; ADRs preserve durable decisions and their history; none is a second current feature matrix.

## Lifecycle procedure

1. Read the affected matrix entries and their linked specification, decision and source evidence before settling scope.
2. In design and planning, state the observable player or developer promise, actual entry and prerequisites, outcomes/failures/resume boundary, authority and history, directed dependencies, current assessment, evidence limits and release disposition. Mark unknown behavior unknown; do not infer a delivered feature from a type, endpoint, screen or test name.
3. In implementation, update the matrix in the same PR when the approved promise, dependency, release disposition or assessed evidence changes. Preserve stable IDs and link a separate future-addition record when retiring a capability that is intended to return.
4. Before requesting PR review, the author compares the actual diff with the affected matrix entries and stages a truthful update or records why no current product promise or dependency changed.
5. The reviewer independently compares the actual diff with the matrix and relevant settled specification, checks that dependencies name their meaning and conditions, and requires corrections for any stale or unsupported claim.
6. Update evidence to distinguish source review, existing behavior tests, executed runtime/browser proof and deployed behavior. Passing CI alone does not change a feature's assessment or establish a player journey.

## Boundaries

The matrix records current product truth and known limits. It is not a roadmap, execution plan, Linear backlog, ADR log, source inventory or test catalogue. Keep current truth in one row and link the proper owner for detail. Do not add structural tests for headings, table rows or file presence; maintain usefulness through semantic review at the lifecycle stages that read this playbook.
