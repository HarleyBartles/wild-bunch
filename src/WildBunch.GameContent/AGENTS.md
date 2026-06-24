# WildBunch.GameContent AGENTS.md

This project contains the UUID seed codec — the single encoding of all starting world state.

## UUID ↔ World Descriptor Codec

- `StartingWorldDescriptorResolver.Resolve(Guid)` — UUID → world descriptor (used at game start)
- `StartingWorldDescriptorResolver.CreateRepresentativeSeedCode(descriptor)` — world descriptor → UUID (used by tests and future start-surface randomizer)
- Both directions must stay in sync. See the root `AGENTS.md` "UUID Seed Codec" section for the full checklist when adding new starting-world fields.

## When to update this project

- **New town or trail**: add to `SeedWorldCatalog.cs`, update `SeedWorldBuilderTests` snapshot assertions, update `SeededNewGameFactoryTests` count assertions.
- **New world variant**: add to `SeedWorldVariant` enum, add variant-specific terrain/water/services to existing town/trail definitions, update `ResolveWorldVariant` in `GameSetupSeedCodec.cs`, update snapshot tests.
- **New loadout profile**: add to `StartingLoadoutProfile` enum, add counts to `ResolveLoadoutCounts`, update `CreateDescriptorSignature` if the profile name changes semantics.
- **New difficulty or entropy level**: update enums, update `ResolveDifficulty`/`ResolveAdventureRandomnessPolicy`, update `CreateCanonicalDescriptorShape`, update descriptor signature.
- **Any new starting-world field**: add to `StartingWorldDescriptor`, add to `GameSetupSeedCodec.Resolve`, add to `StartingWorldDescriptorSeedMixer.CreateDescriptorSignature`, add a round-trip guardrail test.

## Do NOT

- Do NOT store UUIDs in test fixtures. Store descriptors and derive UUIDs via `CreateRepresentativeSeedCode`.
- Do NOT bypass the seed system for encounter/journey tests. Use the seed system + `SeededNewGameFactory`.
- Do NOT add compatibility shims for old UUIDs when the codec changes. In this greenfield repo, current codec correctness wins.
- Do NOT mark gang members as `IsTrueCulpritEligible: false`. The culprit is always a gang member, and any gang member can be the culprit. The `IsTrueCulpritEligible` flag exists for associated characters who are not part of the gang, not for restricting which gang members can be the culprit.
