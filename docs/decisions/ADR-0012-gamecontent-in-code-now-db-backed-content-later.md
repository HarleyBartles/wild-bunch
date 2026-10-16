# ADR-0012 Keep Game Content Code-Backed

## Status

`live`

## Dated History

- `2026-06-01` - Chose deterministic code-backed game content as the current content source.
- `2026-10-16` - Editorial clarification: deterministic code-backed content remains the current choice; database-backed authorable content remains the recorded future migration direction, not an implemented system or a delivery schedule.

## Decision Type

`content`, `architecture`

## Related ADRs

- `depends on`: ADR-0004
- `related to`: ADR-0021

## Context

New games require deterministic world and case content. The content source is distinct from mutable playthrough state.

## Decision

Keep authored game content code-backed and deterministic. Do not store mutable player-session state in the content source. Retain database-backed authorable content as the future migration direction, separate from mutable runtime session state. The migration remains future work; this record does not select its detailed topology or claim it is implemented.

## Rationale and Alternatives

Code-backed content keeps world and case setup explicit and reproducible. Database-backed content is the recorded long-term direction when authoring outgrows code edits. Its design must preserve deterministic setup and define authoring and consistency requirements before migration.

## Consequences

Content changes follow the code review and build process. A future database migration needs a separately scoped design and implementation; the recorded direction does not establish that the migration has occurred.
