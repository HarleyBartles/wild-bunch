# ADR-0025 Legal Warrant Terms Do Not Establish Murder Guilt

## Status

`live`

## Dated History

- `2026-06-10` - Chose to keep public legal and bounty facts separate from hidden culprit truth while defining the first bounty vocabulary.
- `2026-10-08` - Clarified that the earlier unrelated-wanted-target vocabulary did not establish a shipped unrelated-criminal feature; its incomplete 0.1.0 shell is retired under the baseline specification.

## Decision Type

`gameplay`, `architecture`

## Related ADRs

- `depends on`: ADR-0002, ADR-0005, ADR-0007
- `informs`: ADR-0009, ADR-0010, ADR-0026

## Context

The initial bounty design needed a small public vocabulary without merging a person's legal status, bounty terms, and the hidden murder solution. A wanted person may also be the culprit, but the public legal notice does not establish that guilt.

## Decision

Keep warrant terms, target identity, and murder-case truth distinct. Public legal surfaces may communicate the wanted person's public name, aliases, disposition, bounty, issuing authority, and known features. They must not expose hidden culprit identity or imply that a bounty establishes murder guilt.

The original design considered unrelated wanted targets as one possible legal target kind. That consideration was not a decision to ship an unrelated-criminal gameplay loop. This vocabulary consideration did not establish the feature as implemented behavior.

## Rationale and Alternatives

Combining legal status with murder guilt would turn a public warrant into a solution reveal and make ordinary bounty success indistinguishable from case resolution. Keeping the concepts separate preserves player knowledge boundaries and allows one person to have both a warrant and an independent role in the case.

## Consequences

Wanted notices and case records may repeat the same public legal facts while retaining their distinct player purposes. Player-facing projections expose only facts the player could know; developer diagnostics remain a separately controlled surface under ADR-0007 and ADR-0030.
