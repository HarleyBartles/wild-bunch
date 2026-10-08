# ADR-0022 Browser Checks Are a Manual Evidence Lane

## Status

`live`

## Dated History

- `2026-06-01` - Distinguished manual browser checks from automated test lanes for changes that affect visible game behavior.
- `2026-10-08` - Editorial clarification: this record sets the evidence distinction, not current route names or a requirement to manually repeat every automated check.

## Decision Type

`testing`

## Related ADRs

- `related to`: ADR-0017

## Context

Automated tests do not always expose visual, interaction, or browser-level behavior that matters to a player. Browser observations also do not replace behavior tests or provider validation.

## Decision

Use manual browser checks as a distinct evidence lane when a change affects a visible player flow or when a task requires them. Report the observed behavior and limits separately from automated test results.

## Rationale and Alternatives

A browser observation can reveal interaction and rendering problems that a test suite does not cover. Treating it as a substitute for automated tests would make repeatable behavior harder to protect.

## Consequences

Manual browser checks are selected for relevant visible changes, not used as a blanket ritual. Their operational steps belong in the browser playbook and do not belong in this decision record.
