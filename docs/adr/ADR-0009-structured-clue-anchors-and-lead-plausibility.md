# ADR-0009 Structured Clue Anchors and Lead Plausibility

## Status

live

## Dated Status History

- 2026-06-01 - live: clues now carry structured anchors for subjects,
  locations, times, and directions where relevant.

## Decision Type

gameplay, domain

## Related ADRs

- `depends on`: ADR-0005, ADR-0006, ADR-0007
- `informs`: ADR-0010

## Context

The case board, read models, and seed content need clues that can point at a
person or place in a structured way instead of relying on freeform prose alone.
The repo already models clue anchors in the domain and uses them in mapping.

## Decision Drivers

- Clues should have readable, stable meaning beyond their prose text.
- The case board needs anchor data to render evidence plausibly.
- Investigative leads should be fair without requiring full movement simulation.
- The data model should remain flexible enough for different clue shapes.

## Decision Summary

Model clues with structured anchors for subjects, locations, times, and
directions. Use those anchors to support plausible leads and readable case-board
rendering.

## Detailed Decision Breakdown

Clues may carry linked suspects and anchor metadata. Subject anchors can point
to a suspect, alias, feature, or fact. Location anchors can point to a town or
route. Time anchors can capture recency. Direction anchors can capture movement
or route direction.

This gives the case board and other read surfaces enough structure to present a
lead without pretending the game simulates every underlying world movement in
full detail.

## Options Considered and Rejected

- Keep clues as plain text only.
- Force every lead to be backed by a fully simulated movement system.
- Collapse all anchor shapes into one generic “evidence pointer” blob.

## When a Rejected Option Would Have Been Better

Plain text would only be better for throwaway debug strings. Full movement
simulation would only be better if the game were already built around detailed
world-state tracing for every clue, which it is not.

## Benefits

- Read surfaces can explain a clue’s meaning more clearly.
- The case board can build evidence entries from stable anchor shapes.
- Seed content can author clues with more nuance.

## Accepted Tradeoffs

- Clue authoring is a little more verbose.
- The domain has to keep the anchor shapes coherent across serialization and
  mapping.

## Risks

- Anchor data can become inconsistent if seed content is sloppy.
- A future movement system could tempt the code to over-promise what the anchor
  data means.

## Consequences for Future Work

New clue or warrant content should prefer structured anchors when a lead needs
to point at a subject, town, route, or time.

## Implementation Status or Plan

Live. The domain, seed builders, and mapping layers already use structured clue
anchors.

## Related Stable Source Surfaces

- `src/WildBunch.Domain/Cases/CaseModels.cs`
- `src/WildBunch.Application/Games/Mapping/CaseBoardMapper.cs`
- `src/WildBunch.GameContent/NewGame/SeedCaseBuilder.cs`
- `tests/WildBunch.Domain.Tests/CaseFileTests.cs`
- `tests/WildBunch.Application.Tests/CaseBoardMapperTests.cs`

## Proof of Implementation or Explicit Non-Implementation

Clues in the domain carry anchor data, the seed builder authoring uses it, and
the case-board mapper consumes it for public evidence display.

## Review Triggers

- When a lead type needs a new anchor shape.
- When case-board rendering no longer matches the anchor semantics.
