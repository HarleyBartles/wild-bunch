# ADR-0024 Source Taxonomy Implications for Difficulty and Entropy

## Status

live

## Dated Status History

- 2026-06-10 - live: accepted the source-taxonomy implication map for the
  difficulty and entropy axes.

## Decision Type

architecture, gameplay, content

## Related ADRs

- `depends on`: ADR-0023, ADR-0008, ADR-0009
- `related to`: ADR-0010, ADR-0020, ADR-0021

## Context

BUNCH-6 separated difficulty from entropy as two different game-system axes.
ADR-0023 locked the vocabulary and fairness contract. This ADR records how
those axes apply to the current intended source taxonomy so later source,
clue, and case-file work can use one durable map instead of rediscovering the
same rules in each implementation slice.

The live repo currently splits source surfaces across two families:

- town-visit investigation sources such as town noticeboards, local records,
  telegraph leads, and local gossip;
- public case sources such as sheriff warrants.

The intended source taxonomy also includes newspaper as a future public source
surface. It is part of the decision map here even though no newspaper gameplay
is implemented yet.

## Decision Drivers

- Keep difficulty and entropy separate even when both shape source behavior.
- Preserve fair solvability and fixed truth once a case has been set up.
- Make source-specific variation predictable for later content and code work.
- Avoid turning entropy into a hidden difficulty slider.
- Keep live source state and future source categories in one durable source
  document.

## Decision Summary

Difficulty may lawfully affect source pressure, assistance, corroboration, and
readability.

Entropy may lawfully affect source volatility, surface variation, ordering,
freshness, and noise.

Neither axis may rewrite settled truth, invent impossible facts, or make an
established case unknowable.

Town-visit sources may vary within those rules while staying visit-scoped and
refreshing on town return. Public sources may vary within those rules while
keeping their authored identity and truth stable. Newspaper remains future
work, but it follows the same fairness boundary.

## Detailed Decision Breakdown

### Sheriff Warrants

- Difficulty may lawfully affect how much corroboration is shown, how
  prominently a warrant is framed, and how much assistance the player gets in
  connecting the warrant to the case.
- Entropy may lawfully affect ordering, presentation, and ancillary flavor
  around warrant exposure.
- Must remain stable and fair: the wanted identity, warrant truth, and the
  legal basis for the warrant cannot be rewritten after setup.
- Follow-on implementation issue: the future warrant/source-content slice owns
  any real warrant presentation or pressure behavior.

### Local Records

- Difficulty may lawfully affect record depth, legibility, and how much
  cross-reference help is exposed when a record is relevant.
- Entropy may lawfully affect which record copy is surfaced, the order in which
  records appear, and whether older or newer wording is presented first.
- Must remain stable and fair: records may omit, condense, or reorder, but they
  may not contradict settled facts or alter established truth.
- Follow-on implementation issue: the future records/content slice owns real
  record shaping or refresh behavior.

### Newspaper

- Difficulty may lawfully affect how much direct investigative value an article
  exposes, how clearly the article points at a lead, and how much context is
  bundled with the publication.
- Entropy may lawfully affect issue mix, headline framing, article rotation,
  and which published story version is surfaced.
- Must remain stable and fair: newspaper may be noisy, selective, or
  sensational, but it may not invent a contradiction or rewrite already-settled
  world truth.
- Follow-on implementation issue: none yet; newspaper is future source work and
  should get its own implementation slice when the feature is started.

### Town Noticeboard

- Difficulty may lawfully affect how much locality context is attached to a
  notice, how much the notice helps the player, and how dense the board is with
  actionable material.
- Entropy may lawfully affect notice ordering, clustering, and incidental
  clutter from one town visit to the next.
- Must remain stable and fair: the noticeboard must remain town-local, readable,
  and refresh on town return without hiding the fact that it is the town board
  rather than a different source.
- Follow-on implementation issue: the town-source content slice owns any real
  noticeboard variation beyond the current visit-refresh behavior.

### Telegraph Leads

- Difficulty may lawfully affect lead specificity, delay tolerance, and how
  much help the player gets when a telegraph lead is relevant.
- Entropy may lawfully affect dispatch timing, surface noise, and which lead
  variant is shown first.
- Must remain stable and fair: a telegraph lead may be delayed or noisy, but it
  may not retroactively change what happened in the world or point at impossible
  truth.
- Follow-on implementation issue: the future telegraph-lead slice owns any real
  delay, noise, or delivery behavior.

### Local Gossip

- Difficulty may lawfully affect rumor reliability, the amount of corroboration
  needed, and how much uncertainty is visible to the player.
- Entropy may lawfully affect which rumor fragment surfaces, how much chatter is
  attached, and how noisy the gossip cloud feels.
- Must remain stable and fair: gossip may be incomplete, contradictory in
  flavor, or low confidence, but it may not rewrite settled truth or create an
  unsolvable contradiction.
- Follow-on implementation issue: the future gossip-source slice owns any real
  rumor volatility or mutation behavior.

## Options Considered and Rejected

- Collapse all source variation into one difficulty slider.
- Let entropy rewrite identity truth for the sake of surprise.
- Defer the source taxonomy until runtime implementation starts.
- Treat public sources and town-visit sources as if they used the same
  repeatability rules.

## When a Rejected Option Would Have Been Better

Collapsing the axes would only help if the game wanted one blended pressure
knob. That is not the current design.

Letting entropy rewrite truth would only help if the mystery were supposed to
be unstable in principle. That would break the fairness contract already set by
ADR-0023.

## Benefits

- Later source work gets a single map for lawful variation.
- Difficulty and entropy remain distinct at the source level.
- The fairness boundary is explicit for both live and future source categories.
- Town-local and public source surfaces stay distinguishable.

## Accepted Tradeoffs

- The taxonomy is now locked before all source behaviors exist.
- Future source slices must conform to this map instead of inventing their own
  rules.
- Newspaper is documented before implementation, which is useful for planning
  but means the note carries a future-only source category.

## Risks

- A future source slice could treat entropy as permission to change facts.
- A later implementation could collapse public and town-visit source rules by
  accident.
- A source category could drift if future work ignores the stable/fair
  boundaries in this ADR.

## Consequences for Future Work

Future source and content slices should use this map when deciding whether a
change is lawful under difficulty, entropy, both, or neither.

Town-visit source work should keep the current visit-scoped refresh contract.
Public source work should keep authored truth stable while allowing variation
in presentation, order, and assistance.

## Implementation Status or Plan

Live as repository doctrine and planning guidance. No newspaper gameplay or
entropy-driven source mutation is introduced by this issue.

## Related Stable Source Surfaces

- `src/WildBunch.Domain/World/TownSourceModels.cs`
- `src/WildBunch.Domain/Actions/InvestigationSources.cs`
- `src/WildBunch.Domain/Game/TownAggregate.cs`
- `src/WildBunch.Domain/Game/TownVisitState.cs`
- `src/WildBunch.Domain/Cases/CaseFile.cs`
- `docs/adr/ADR-0023-difficulty-and-entropy-vocabulary-and-fairness-contract.md`

## Proof of Implementation or Explicit Non-Implementation

This ADR does not implement source-noise behavior, newspaper gameplay,
telegraph delays, gossip mutation, decoys, or source-driven truth changes. It
records the lawful implication map only.

The current repo already has live town-visit source refresh behavior and public
warrant reveal behavior. Newspaper remains future work.

## Review Triggers

- When a source slice tries to use entropy to rewrite facts.
- When newspaper gameplay is introduced.
- When a new source category needs a different lawful variation rule.
- When public and town-visit source repeatability start to blur together.
