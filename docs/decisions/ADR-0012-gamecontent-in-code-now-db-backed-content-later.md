# ADR-0012 Keep Game Content Code-Backed

## Status

`live`

## Dated History

- `2026-06-01` - Chose deterministic code-backed game content as the current content source.
- `2026-10-08` - Editorial clarification: this record decides the current content boundary; it does not establish a database migration as a committed future requirement or report external issue status.

## Decision Type

`content`, `architecture`

## Related ADRs

- `depends on`: ADR-0004
- `related to`: ADR-0021, ADR-0039

## Context

New games require deterministic world and case content. The content source is distinct from mutable playthrough state.

## Decision

Keep authored game content code-backed and deterministic. Do not store mutable player-session state in the content source. This decision does not choose a database-backed content system.

## Rationale and Alternatives

Code-backed content keeps world and case setup explicit and reproducible. A database content store would be a separate decision with its own authoring and consistency requirements, not an implied next step of this record.

## Consequences

Content changes follow the code review and build process. A future change in content ownership requires an explicit decision rather than an assumption that the current record already approved it.
