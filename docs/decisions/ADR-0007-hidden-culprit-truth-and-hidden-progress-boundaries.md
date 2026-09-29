# ADR-0007 Hidden Culprit Truth and Hidden Progress Boundaries

## Status

live

## Dated Status History

- 2026-06-01 - live: hidden culprit identity and related progress boundaries are
  kept internal instead of being exposed through public DTOs or read surfaces.

## Decision Type

gameplay, architecture

## Related ADRs

- `depends on`: ADR-0002, ADR-0005
- `informs`: ADR-0006, ADR-0009, ADR-0010, ADR-0011

## Context

The game’s mystery works only if the player-facing API does not leak hidden
truths such as the culprit identity, exact release thresholds, or other
internal progress markers. The repo already contains public DTO mapping and
integration tests that check this boundary.

## Decision Drivers

- The hidden culprit identity must stay internal.
- Public DTOs should expose player knowledge, not debugging truth.
- Hidden progress markers should stay out of public read models.
- The boundary needs to be testable.

## Decision Summary

Keep hidden culprit truth and hidden progress boundaries internal to the domain
and out of public DTO/read surfaces.

## Detailed Decision Breakdown

The domain may track hidden case progress internally, but public API responses
and read models should only show what the player has legitimately discovered.
This applies to culprit identity, hidden roster details, and any internal
progress counters that would spoil the case.

The mapping layer and tests are the right place to prove that the public surface
does not reveal these internals.

## Options Considered and Rejected

- Expose the culprit and hidden progress values in public DTOs for convenience.
- Allow read models to include raw internal case state.
- Use the client UI as the only boundary protection.

## When a Rejected Option Would Have Been Better

Revealing hidden truth would only be better for debugging surfaces or test
harnesses that are explicitly not player-facing. It would not be appropriate for
the game’s public API.

## Benefits

- The mystery structure stays intact.
- API consumers only receive legitimate player knowledge.
- Tests can detect accidental leaks.

## Accepted Tradeoffs

- Public mapping has to remain intentionally selective.
- Some internal state must stay undocumented outside the domain boundaries.

## Risks

- New DTOs could accidentally expose hidden values if they bypass the established
  mapping surfaces.
- Future refactors could blur the line between public knowledge and secret state
  if this boundary is not kept explicit.

## Consequences for Future Work

Any new read model, mapper, or UI surface must treat hidden culprit truth and
internal progress as private domain data unless a new ADR explicitly changes the
rule.

## Implementation Status or Plan

Live. The current mapping and tests already enforce the public/private split.

## Related Stable Source Surfaces

- `src/WildBunch.Domain/Cases/CaseFile.cs`
- `src/WildBunch.Domain/Game/GameSession.cs`
- `src/WildBunch.Application/Games/Mapping/CaseReadMapper.cs`
- `src/WildBunch.Application/Games/Models/GameSessionDto.cs`
- `tests/WildBunch.Application.Tests/GetGameSessionHandlerTests.cs`
- `tests/WildBunch.Integration.Tests/GameApiHiddenTruthTests.cs`

## Proof of Implementation or Explicit Non-Implementation

The mapper and tests keep hidden culprit details out of public DTO payloads and
verify that anchors and read surfaces do not leak the internal fields.

## Review Triggers

- When a new public read model is added for case data.
- When hidden state starts appearing in DTOs or API responses.
