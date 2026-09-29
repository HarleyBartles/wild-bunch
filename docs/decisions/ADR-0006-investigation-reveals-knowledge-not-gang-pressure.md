# ADR-0006 Investigation Reveals Knowledge, Not Gang Pressure

## Status

live

## Dated Status History

- 2026-06-01 - live: investigation actions reveal clues, warrants, and public
  knowledge while keeping gang-pressure style outcomes out of the current
  player-facing loop.

## Decision Type

gameplay, domain

## Related ADRs

- `depends on`: ADR-0005
- `informs`: ADR-0007, ADR-0008, ADR-0009, ADR-0010

## Context

The current case flow is about uncovering information: clues, warrants, suspect
links, and public leads. The game should surface knowledge to the player
without smuggling a separate gang-pressure system into the same interaction.

## Decision Drivers

- Investigation should feel like discovering facts and leads.
- Public actions need to reveal information, not hidden meta pressure.
- Current source surfaces already model clue and warrant revelation cleanly.
- Future justice/disruption systems should not be implied before they exist.

## Decision Summary

Investigation actions reveal knowledge only. Any future gang-pressure or
disruption model should be a separate decision and should not be conflated with
the present clue/warrant reveal loop.

## Detailed Decision Breakdown

Current investigation entry points reveal public clues or warrants from the case
file and then record a case update. That keeps the player loop centered on what
the detective knows and can act on, rather than on an opaque pressure meter.

The repo does have hidden internal progress for case-specific logic, but that is
not the same thing as a public gang-pressure system and should not be documented
as such.

## Options Considered and Rejected

- Expose a gang-pressure meter as part of the current investigation loop.
- Treat investigation as a generic resource-exhaustion mechanic.
- Make every public lead advance a broader gang-disruption score.

## When a Rejected Option Would Have Been Better

A public gang-pressure track would only be better if the product had a separate
player-facing justice or disruption loop that was already implemented and needed
to be represented directly. That is not the current state.

## Benefits

- The current case flow stays readable and direct.
- Public lead discovery remains focused on knowledge acquisition.
- The game avoids implying a mechanic that does not yet exist.

## Accepted Tradeoffs

- Some future narrative pressure systems will need their own ADR and UI/API
  surfaces.
- The case model does not try to solve every future justice mechanic now.

## Risks

- Future work could accidentally blend public knowledge with meta pressure if
  the boundary is not restated.
- A hidden progress field could be misread as a public gang-pressure system if
  documentation becomes sloppy.

## Consequences for Future Work

Any future gang-pressure, retaliation, or disruption feature should be
specified separately and should not reuse this ADR as proof of implementation.

## Implementation Status or Plan

Live for knowledge-reveal behavior. Gang-pressure style outcomes remain a
separate future decision.

## Related Stable Source Surfaces

- `src/WildBunch.Domain/Game/GameSession.cs`
- `src/WildBunch.Domain/Cases/CaseFile.cs`
- `tests/WildBunch.Domain.Tests/GameSessionInvestigationActionsTests.cs`
- `tests/WildBunch.Domain.Tests/GameSessionWantedPostersTests.cs`
- `src/WildBunch.Application/Games/Mapping/CaseBoardMapper.cs`

## Proof of Implementation or Explicit Non-Implementation

The investigation methods reveal clues and warrants from the case file and
record case updates. There is no current player-facing gang-pressure meter to
confuse with that behavior.

## Review Triggers

- When a real gang-pressure or disruption system is added.
- When investigation actions start driving outcomes beyond knowledge reveal.
