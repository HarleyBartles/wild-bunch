# ADR-0026 Turn-In Outcome Semantics for Bounty and Murder-Case Separation

## Status

live

## Dated Status History

- 2026-06-10 - live: accepted the turn-in outcome vocabulary boundary so later sheriff work can separate bounty outcomes from murder-case resolution.

## Decision Type

gameplay, architecture, documentation

## Related ADRs

- `depends on`: ADR-0002, ADR-0005, ADR-0007, ADR-0010, ADR-0025

## Context

BUNCH-12C needs the smallest stable contract for future turn-in work. The repo already has the legal vocabulary boundary in ADR-0025, but it does not yet name the outcome split that a sheriff turn-in will need. Future implementation slices such as BUNCH-43, BUNCH-44, and BUNCH-45 should consume this contract rather than inventing their own result vocabulary.

The current source surfaces already separate several concerns:

- `WarrantDisposition` expresses alive-only versus dead-or-alive legal terms.
- `WarrantTerms` carries bounty amount, issuing source, known aliases, known features, source kind, and target kind.
- `CaseFile.TrueCulpritId` keeps hidden culprit truth inside the session-owned case model.
- `WantedPosterMapper` and `CaseBoardMapper` project legal facts into player-facing surfaces without exposing hidden culprit identity.

That shape is enough to name the turn-in outcome boundary now. A runtime turn-in result type is not needed yet because there is no sheriff turn-in command or payout flow in the repo.

## Decision Drivers

- Bounty success is not the same as murder-case success.
- Wanted/outlaw status is not proof of murder guilt.
- Alive and dead turn-in outcomes are different legal outcomes.
- Wrong-person alive turn-in is not the same as killing the wrong person.
- Killing the true killer may satisfy ordinary bounty terms, but it must not become murder-case success.
- Future murder-case success still requires the alive confession path unless a later issue changes that rule.
- Hidden culprit truth must stay internal.
- Payout, wallet, fine, and penalty settlement must remain separate from the outcome contract.

## Decision Summary

Use a five-part separation for future turn-in work:

1. legal and bounty eligibility;
2. turn-in outcome;
3. payout or penalty settlement;
4. murder-case resolution;
5. hidden culprit truth.

Turn-in outcome may report whether a target was accepted alive, accepted dead, rejected as the wrong person, or escalated into a fail state. It must not itself decide bounty payout, wallet mutation, penalty mutation, or murder-case win/loss.

## Detailed Decision Breakdown

### Legal and Bounty Eligibility

Eligibility is the warrant-side question: does this target meet the public legal terms for a turn-in?

- alive-only warrants require the target to be brought in alive;
- dead-or-alive warrants allow either form of delivery for ordinary bounty purposes;
- legal eligibility is independent of murder guilt;
- a wanted poster or warrant may identify an outlaw without proving that the outlaw is the murder culprit.

### Turn-In Outcome

The turn-in outcome is the immediate lawman-facing result of the handoff.

- accepted alive: the target was delivered alive and the legal terms were satisfied;
- accepted dead: the target was delivered dead and the legal terms were satisfied;
- wrong person alive: the handoff was lawful-looking but failed identity/target matching;
- wrong person dead: the wrong target was killed and the result is not equivalent to the alive wrong-person outcome;
- rejected or escalated: the turn-in did not complete cleanly and may feed a later follow-up rule.

This layer records what happened. It does not settle money, record the player's final case state, or reveal hidden culprit truth.

### Settlement

Settlement is a separate future concern.

- ordinary bounty payout is not the same as murder-case success;
- a future penalty or fine rule is not the same as outcome recognition;
- wallet mutation must be implemented in the economy slice, not in the turn-in outcome contract.

### Murder-Case Resolution

Murder-case resolution stays separate from ordinary bounty handling.

- a dead true killer may still satisfy a bounty term, but it does not satisfy murder-case success;
- killing the true killer removes the alive confession path and therefore cannot become a murder-case win;
- future murder-case success still requires the alive confession path unless a later issue explicitly changes that rule;
- killing the wrong person is a murder/fail outcome, not a simple bounty outcome.

### Hidden Culprit Truth

Hidden culprit truth remains internal to the case/session model.

- turn-in outcome surfaces may use public warrant facts;
- they must not expose `TrueCulpritId` or any equivalent hidden-truth marker;
- public-facing legal surfaces may show legal facts, aliases, and bounty terms, but not the internal murder solution.

## Options Considered and Rejected

- Collapse legal eligibility, turn-in result, payout, and murder-case success into one status field.
- Wait for the full sheriff loop before naming the outcome contract.
- Add wallet or fine mutation in the same slice as the outcome definition.
- Expose hidden culprit truth in the turn-in result so the future caller can decide everything in one place.

## When a Rejected Option Would Have Been Better

A single umbrella result would only be better if the game wanted one command to own legal handling, payment, and case resolution at once. That would blur the current architecture and make future sheriff work harder to separate concerns.

## Benefits

- Future sheriff work gets a small, stable contract instead of inventing outcome names on the spot.
- Ordinary bounty outcomes remain distinct from murder-case resolution.
- Hidden culprit truth stays protected by the existing case boundary.
- Economy work stays out of the turn-in contract until a later slice owns it.

## Accepted Tradeoffs

- The repo now records a turn-in contract before the command exists.
- The outcome vocabulary is deliberately narrower than the eventual game loop may become.
- Future implementation must align with the documented split instead of folding everything into one result type.

## Risks

- A later turn-in implementation could accidentally treat bounty success as case success.
- A future payout slice could leak into the turn-in contract if the boundary is ignored.
- A caller could try to infer hidden culprit truth from legal outcome alone.

## Consequences for Future Work

Future sheriff turn-in, arrest, bounty payout, confrontation follow-up, and case-resolution slices should treat this ADR as the outcome baseline.

If a dedicated runtime result type becomes necessary later, it should preserve the five-way separation above rather than collapsing legality, settlement, and case resolution into one value.

## Implementation Status or Plan

Live as documentation only. No sheriff turn-in command, arrest mechanic, payout logic, wallet mutation, or murder-case resolution behavior is introduced by this issue.

## Related Stable Source Surfaces

- `src/WildBunch.Domain/Cases/CaseWarrants.cs`
- `src/WildBunch.Domain/Cases/CaseFile.cs`
- `src/WildBunch.Application/Games/Mapping/WantedPosterMapper.cs`
- `src/WildBunch.Application/Games/Mapping/CaseBoardMapper.cs`
- `tests/WildBunch.Application.Tests/WantedPosterMapperTests.cs`
- `tests/WildBunch.Application.Tests/CaseBoardMapperTests.cs`
- `tests/WildBunch.Domain.Tests/CaseInvestigationFoundationTests.cs`
- `docs/adr/ADR-0025-suspect-legal-and-bounty-vocabulary-boundary.md`

## Proof of Implementation or Explicit Non-Implementation

The current repo does not yet contain a turn-in command, arrest flow, payout calculation, wallet mutation, or murder-case win/loss handler. This ADR therefore records the contract only. The live source surfaces prove the legal vocabulary split; the turn-in outcome split is intentionally not implemented yet.

## Review Triggers

- When sheriff turn-in work starts.
- When payout, penalty, or wallet mutation needs to be attached to the turn-in path.
- When a future case-resolution slice wants to reinterpret bounty success as murder-case success.
- When a runtime result type becomes necessary for the sheriff loop.
