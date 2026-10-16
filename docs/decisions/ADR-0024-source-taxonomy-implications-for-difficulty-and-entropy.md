# ADR-0024 Fair Variation Must Preserve Source Meaning

## Status

`live`

## Dated History

- `2026-06-10` - Accepted the source-specific lawful variation map under the separate difficulty and entropy axes in ADR-0023, including future sources.
- `2026-10-16` - Editorial clarification: retain the accepted map as a design constraint without treating it as evidence that every source or variation mechanic is implemented.

## Decision Type

`gameplay`, `content`

## Related ADRs

- `depends on`: ADR-0023, ADR-0008, ADR-0009
- `related to`: ADR-0010, ADR-0020, ADR-0021

## Context

Investigative sources can vary in presentation or availability, but the player must be able to reason from established facts. The source map was accepted before all source behaviors existed so later implementation would follow one fairness boundary.

## Decision

Difficulty may vary pressure, assistance, corroboration and readability. Entropy may vary volatility, presentation, ordering, freshness and noise. Neither axis may rewrite settled truth, invent impossible facts or make an established case unknowable. The axes remain distinct under ADR-0023.

| Source | Lawful difficulty variation | Lawful entropy variation | Stable boundary |
| --- | --- | --- | --- |
| Sheriff warrants | Corroboration, prominence and help connecting the warrant to the case | Ordering, presentation and ancillary flavor | Wanted identity, warrant truth and legal basis remain fixed after setup. |
| Local records | Depth, legibility and cross-reference help | Surfaced copy, ordering and older/newer wording | Records may omit, condense or reorder without contradicting settled facts. |
| Newspaper | Direct investigative value, lead clarity and context | Issue mix, headlines, article rotation and published version | Selectivity, noise or sensationalism cannot contradict or rewrite settled world truth. |
| Town noticeboard | Locality context, assistance and actionable density | Ordering, clustering and incidental clutter between visits | The board remains town-local, readable and recognizably the town board, refreshing on town return. |
| Telegraph leads | Specificity, delay tolerance and assistance | Dispatch timing, surface noise and variant order | Delay or noise cannot change past events or point at impossible truth. |
| Local gossip | Reliability, required corroboration and visible uncertainty | Rumor fragments, chatter and noise | Incomplete or low-confidence gossip cannot rewrite truth or create an unsolvable contradiction. |

Town-visit sources retain visit-scoped use and refresh on town return. Public sources preserve authored identity and truth while allowing the lawful presentation variation above. Newspaper and other unimplemented source mechanics remain future work; their inclusion here records a design constraint, not implementation completion or a delivery promise.

## Rationale and Alternatives

Locking the map before implementation prevents each source slice from inventing incompatible meanings for difficulty and entropy. A combined slider or randomness that changes established facts would break fair solvability. Public sources and town-visit sources must retain their distinct repeatability rules.

## Consequences

Future source and content work must conform to this map unless a later explicit decision changes it. Each feature still needs its own implementation scope; the decision does not create an execution backlog or report source availability.
