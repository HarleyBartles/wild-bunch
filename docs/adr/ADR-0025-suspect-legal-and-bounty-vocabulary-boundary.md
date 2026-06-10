# ADR-0025 Suspect Legal and Bounty Vocabulary Boundary

## Status

live

## Dated Status History

- 2026-06-10 - live: the current legal/bounty vocabulary is kept separate from hidden culprit truth, and the live source surfaces already use that split.

## Decision Type

gameplay, architecture, documentation

## Related ADRs

- `depends on`: ADR-0002, ADR-0005, ADR-0006, ADR-0007
- `informs`: ADR-0009, ADR-0010

## Context

BUNCH-12A needs the smallest boring seam for future bounty work. The current source already has a useful split:

- `WarrantDisposition` captures alive-only versus dead-or-alive legal terms.
- `WarrantTerms` carries bounty amount, issuing source, known aliases, known features, source kind, and target kind.
- `InvestigationTargetKind` distinguishes suspected, gang member, true culprit, and unrelated wanted criminal cases.
- `CaseFile.TrueCulpritId` keeps hidden culprit truth inside the session-owned case model.
- `WantedPosterMapper` projects public legal terms into player-facing wanted poster DTOs without exposing hidden culprit identity.

That shape is enough to document the vocabulary boundary now. A new runtime legal-status model would be redundant until turn-in and outcome rules actually need it.

## Decision Drivers

- Bounty success is not the same as murder-case success.
- Wanted/outlaw status is not proof of murder guilt.
- The true killer may also be a wanted outlaw with a bounty.
- Hidden culprit truth must stay internal to the case/session model.
- Player-facing wanted poster and journal surfaces should expose legal facts, not hidden solution truth.

## Decision Summary

Use the existing legal vocabulary in `WarrantDisposition`, `WarrantTerms`, and `InvestigationTargetKind` as the player-facing bounty/legal seam for now.

Do not introduce a separate public legal-status field that collapses wanted status, murder guilt, and case resolution into one value.

Keep `TrueCulpritId` and related culprit truth internal to the case model, and continue to project only player-safe legal facts through wanted poster and read-model mappers.

## Detailed Decision Breakdown

The current model already supports the minimum boring distinction needed for future bounty work:

- legal terms can say whether a warrant is alive-only or dead-or-alive;
- bounty amount is part of the warrant terms, not part of murder-case truth;
- target kind can identify a true culprit, a gang member, or an unrelated wanted criminal without turning that into case resolution;
- hidden culprit identity stays in `CaseFile`, not in player-facing DTOs.

This means the next bounty-related slices can build on a stable vocabulary without inventing a second naming system or leaking case truth into legal data.

## Options Considered and Rejected

- Add a new public `SuspectLegalStatus` model immediately.
- Collapse wanted status, bounty terms, and culprit truth into one field.
- Expose `TrueCulpritId` in player-facing DTOs for convenience.
- Wait for turn-in, arrest, and payout rules before naming the boundary at all.

## When a Rejected Option Would Have Been Better

A dedicated legal-status model would be better only when the game needs a concrete turn-in or outcome seam that is not already expressible by `WarrantTerms` and `InvestigationTargetKind`. That is a later child slice, not this one.

## Benefits

- The vocabulary stays boring and explicit.
- Player-facing surfaces remain separate from hidden culprit truth.
- Future turn-in and payout work has a stable naming boundary to extend.

## Accepted Tradeoffs

- The repo keeps using a split vocabulary instead of a single umbrella status.
- Some future legal/outcome logic may still need a new small model later if the warrant terms become too thin.

## Risks

- Future work could accidentally blur wanted status and murder guilt if it ignores this boundary.
- New read models could leak hidden truth if they bypass the established mapper surfaces.

## Consequences for Future Work

Any future sheriff turn-in, arrest, bounty payout, or kill/alive outcome work should treat this ADR as the vocabulary baseline.

If later work needs a dedicated legal-state type, it should be introduced from the turn-in/outcome slice, not by retrofitting the hidden culprit model.

## Implementation Status or Plan

Live as documentation. The source already implements the vocabulary split through existing domain and mapper shapes; this ADR records the boundary so future bounty work does not accidentally merge legal status with hidden culprit truth.

## Related Stable Source Surfaces

- `src/WildBunch.Domain/Cases/CaseWarrants.cs`
- `src/WildBunch.Domain/Cases/CaseFile.cs`
- `src/WildBunch.Domain/Game/GameSession.cs`
- `src/WildBunch.Application/Games/Mapping/WantedPosterMapper.cs`
- `src/WildBunch.Application/Games/Models/WantedPosterDtos.cs`
- `src/WildBunch.Persistence/Serialization/GameSessionJsonSerializer.Components.cs`
- `tests/WildBunch.Application.Tests/WantedPosterMapperTests.cs`
- `tests/WildBunch.Domain.Tests/CaseInvestigationFoundationTests.cs`
- `tests/WildBunch.Application.Tests/GetJournalHandlerTests.cs`

## Proof of Implementation or Explicit Non-Implementation

The current source already keeps hidden culprit identity in the session-owned case model and only exposes wanted poster legal terms through the public mapper. This ADR is the explicit documentation layer for that split; no new runtime model was required for this child slice.

## Review Triggers

- When a later child adds sheriff turn-in, arrest, or payout outcomes.
- When a future design needs a concrete runtime legal-status object instead of the current warrant-term vocabulary.
