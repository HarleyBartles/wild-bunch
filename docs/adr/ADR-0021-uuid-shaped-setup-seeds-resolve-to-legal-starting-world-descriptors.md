# ADR-0021 UUID-Shaped Setup Seeds Resolve to Legal Starting-World Descriptors

## Status

live

## Dated Status History

- 2026-06-01 - live: setup seeds now resolve from UUID-shaped seed codes into validated starting-world descriptors.

## Decision Type

architecture, gameplay, testing

## Related ADRs

- `depends on`: ADR-0002, ADR-0007, ADR-0012, ADR-0013, ADR-0014
- `informs`: ADR-0010

## Context

The old setup seed model used a WB1-prefixed mixed format that bundled explicit options with entropy. That shape was too close to a temporary codec and too far from the new setup architecture needed for deterministic scenario generation and future descriptor-driven work.

Issue #42 requires a seed architecture that can be consumed by future scenario builders without exposing hidden culprit truth, while still allowing the starting world, starting loadout, and whole-adventure randomness posture to be resolved deterministically from a player-facing seed code.

## Decision

Setup seeds are now UUID-shaped seed codes. The player-facing code is just a legal UUID string; it is not a WB1-format token and it is not an option bundle.

The code resolves into a hierarchical `StartingWorldDescriptor` that owns:

- travel difficulty
- `AdventureRandomnessPolicy`
- world variant and starting-town selection key
- starting player loadout posture
- starting wallet and inventory counts
- accusation index for setup-time case shaping

The resolver is deterministic for all valid UUIDs and the descriptor is validated before package generation. Invalid manual descriptor edits are rejected rather than normalized.

## Decision Drivers

- Keep the setup seed contract simple and future-friendly.
- Make the seed shape suitable for future deterministic scenario construction.
- Preserve hidden culprit truth boundaries.
- Avoid a compatibility layer for the retired WB1 format.
- Distinguish whole-adventure randomness from journey-only naming.

## Decision Summary

The new setup seed architecture is UUID-shaped, descriptor-driven, and validated. Legal UUIDs always resolve to legal descriptors, and the descriptor is the place where setup meaning lives.

## Detailed Decision Breakdown

The resolver maps seed code bytes into bounded descriptor fields. Multiple UUIDs may resolve to the same descriptor, and the descriptor can later be turned into a representative seed code for tests or fixture setup.

`AdventureRandomnessPolicy` is a first-class descriptor concept with named bands, including `Boring`, `Standard`, `Adventurous`, and `Wild`. Wild mode is legal and high-variance, but it still stays inside domain invariants.

The descriptor does not expose hidden culprit identity, hidden culprit markers, or internal solution truth. Those remain internal to the case-building path and the game/session surfaces that already own them.

## Options Considered and Rejected

- Keep the WB1 codec and translate it internally.
- Use a boolean or journey-only randomness flag.
- Treat player-facing setup codes as free-form entropy strings.

## When a Rejected Option Would Have Been Better

The old WB1 codec would only have been preferable if the repo needed live compatibility with existing player saves or a published external seed contract. This repo does not have that product constraint.

## Benefits

- Clear separation between seed code, descriptor, resolver, and validation.
- Deterministic, legal setup resolution for every valid UUID-shaped seed.
- Better support for future deterministic scenario builders.
- More explicit adventure randomness semantics.

## Accepted Tradeoffs

- The old WB1 seed strings are no longer supported as product behavior.
- The seed no longer encodes every tiny setup detail directly.
- Some different UUIDs now intentionally collide onto the same legal descriptor.

## Risks

- The descriptor can become too broad if future setup fields are stuffed into it without care.
- Wild semantics could drift if later runtime systems start ignoring the policy boundary.

## Consequences for Future Work

Future deterministic scenario builders can consume the descriptor directly instead of reverse-engineering a codec. Future gameplay work can branch on `AdventureRandomnessPolicy` without pretending it is only about journeys.

## Implementation Status or Plan

Live.

## Related Stable Source Surfaces

- `src/WildBunch.GameContent/NewGame/GameSetupSeedCodec.cs`
- `src/WildBunch.GameContent/NewGame/GameSetupSeed.cs`
- `src/WildBunch.GameContent/NewGame/GameSetupGenerationPlan.cs`
- `src/WildBunch.GameContent/NewGame/SeededNewGameFactory.cs`
- `tests/WildBunch.GameContent.Tests/StartingWorldDescriptorResolverTests.cs`
- `tests/WildBunch.Integration.Tests/TestInfrastructure/ScenarioSeedCatalog.cs`

## Proof of Implementation or Explicit Non-Implementation

The live implementation resolves UUID-shaped seed codes into validated starting-world descriptors, uses `AdventureRandomnessPolicy` as a descriptor-level concept, and no longer relies on the retired WB1 product format.

## Review Triggers

- If setup needs additional first-class descriptor dimensions.
- If a future feature requires a public compatibility surface for old seed strings.
- If Wild mode starts bypassing hard domain invariants.
