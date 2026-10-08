# ADR-0018 Target .NET 10 with Nullable-Enabled SDK-Style Projects

## Status

`live`

## Dated History

- `2026-06-01` - Chose a uniform .NET 10 SDK-style project baseline with nullable reference types and implicit usings enabled.
- `2026-10-08` - Editorial clarification: the decision describes the repository baseline; project and test inventories are not part of the record.

## Decision Type

`architecture`, `tooling`

## Related ADRs

- `depends on`: ADR-0001
- `informs`: ADR-0014, ADR-0015, ADR-0017, ADR-0019

## Context

The application and its tests need a consistent framework and project configuration so that nullable contracts and build behavior do not vary by layer without an explicit reason.

## Decision

Use .NET 10 SDK-style projects with nullable reference types and implicit usings enabled as the repository baseline. This decision does not establish a longer-term framework support policy beyond the selected target.

## Rationale and Alternatives

A uniform target avoids accidental differences between application layers and their tests. Mixed targets or disabled nullable analysis require an explicit compatibility or migration reason.

## Consequences

New projects follow this baseline unless a later decision changes it. Framework upgrades remain deliberate repository changes rather than silent project-by-project drift.
