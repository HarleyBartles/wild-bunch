# ADR-0005 CaseFile Is a Session-Owned Aggregate/Subaggregate

## Status

live

## Dated Status History

- 2026-06-01 - live: `CaseFile` remains owned by `GameSession` and is persisted as part of the session model rather than as a separate aggregate root.
- 2026-06-01 - clarified: `CaseFile` is a session-owned aggregate/subaggregate boundary with its own internal invariants, not just a vague helper component.

## Decision Type

architecture, gameplay

## Related ADRs

- `depends on`: ADR-0002, ADR-0003
- `informs`: ADR-0006, ADR-0007, ADR-0008, ADR-0009, ADR-0010

## Context

The case model holds durable suspect, clue, warrant, and turf state that lives inside a running session. It behaves like a cohesive component of the live game, not as a separate command boundary.

## Decision Drivers

- Case state must stay consistent with the live session.
- Investigation and case updates flow through the session model.
- The case data needs to persist with the rest of the session snapshot.
- Separating the case into its own root would duplicate command authority.

## Decision Summary

Keep `CaseFile` as a session-owned aggregate/subaggregate inside `GameSession`. It owns case-local invariants and durable evidence, but it does not become a separate command aggregate root or repository.

## Detailed Decision Breakdown

`CaseFile` owns the suspect roster, known and public clues, known and public warrants, discovered suspects, turf assignments, and the case-local rules that go with them. `GameSession` uses it to reveal knowledge, drive case updates, and preserve the hidden culprit boundary.

The object is still a plain domain model, but it belongs to the session's live state rather than to an independent case persistence model. The subordinate boundary is real: it carries its own invariants, but it does not get separate command entry or repository ownership.

## Options Considered and Rejected

- Split `CaseFile` into its own aggregate root and repository.
- Treat clues and warrants as unrelated collections outside the session.
- Push case-state mutation into infrastructure or application services.
- Flatten case state into a DTO-style record with no internal boundary.

## When a Rejected Option Would Have Been Better

A separate root would only be better if a case could be independently owned, commanded, and persisted apart from the live session. That is not the current product shape.

Flattening would only be better if `CaseFile` were purely transport data. The current model is not transport-only; it owns case invariants.

## Benefits

- Case invariants stay tied to the session that owns them.
- Persistence can round-trip the case alongside the rest of the session.
- The model is easier to reason about because the case data lives where the gameplay happens.

## Accepted Tradeoffs

- `GameSession` remains the orchestration point for case-related actions.
- The case component must stay coherent so it does not become a hidden root.
- Case-local invariants are subordinate to the session root, so they need to be kept explicit in the domain wording.

## Risks

- If case behavior expands too far, the session root could become overloaded.
- A future independent case lifecycle would require a careful boundary review.

## Consequences for Future Work

New case-related gameplay should assume session ownership unless a new ADR creates a genuinely separate boundary.

## Implementation Status or Plan

Live. `CaseFile` is already a field of `GameSession` and is serialized through the session persistence layer.

## Related Stable Source Surfaces

- `src/WildBunch.Domain/Cases/CaseFile.cs`
- `src/WildBunch.Domain/Game/GameSession.cs`
- `src/WildBunch.GameContent/NewGame/SeedCaseBuilder.cs`
- `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.SessionSnapshot.cs`
- `tests/WildBunch.Domain.Tests/CaseFileTests.cs`
- `tests/WildBunch.Domain.Tests/GameSessionInvestigationActionsTests.cs`

## Proof of Implementation or Explicit Non-Implementation

`GameSession` owns the `CaseFile` instance, the case file participates in session serialization, and the tests exercise case behavior through the session-owned model.

## Review Triggers

- When a new case lifecycle would need to survive independently of a session.
- When `CaseFile` starts absorbing responsibilities that are not actually case ownership.
