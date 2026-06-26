# NewGame

Seeded new-game factory, world/case/inventory builders, and the UUID seed codec.

## Key files

- [SeededNewGameFactory.cs](SeededNewGameFactory.cs) - Factory that builds a new game from a UUID seed.
- [GameSetupSeedCodec.cs](GameSetupSeedCodec.cs) - UUID ↔ world descriptor codec (both directions).
- [GameSetupSeed.cs](GameSetupSeed.cs) - Seed model and resolver (`StartingWorldDescriptorResolver`).
- [GameSetupSeedCodeValidator.cs](GameSetupSeedCodeValidator.cs) - Validates seed codes.
- [StartingWorldDescriptorSeedMixer.cs](StartingWorldDescriptorSeedMixer.cs) - Descriptor signature for round-trip encoding.
- [SeedWorldCatalog.cs](SeedWorldCatalog.cs) - Catalog of seed towns and trails.
- [SeedWorldBuilder.cs](SeedWorldBuilder.cs) - Builds the starting world from a descriptor.
- [SeedCaseBuilder.cs](SeedCaseBuilder.cs) - Builds the starting case from a descriptor.
- [SeedInventoryBuilder.cs](SeedInventoryBuilder.cs) - Builds the starting inventory from a descriptor.
- [GameSetupPackage.cs](GameSetupPackage.cs) / [GameSetupPackageBuilder.cs](GameSetupPackageBuilder.cs) - Composite game setup package.
- [GameSetupGenerationPlan.cs](GameSetupGenerationPlan.cs) - Generation plan for a setup package.
- [GameSetupDeterministicSource.cs](GameSetupDeterministicSource.cs) / [GameSetupDeterministicLabels.cs](GameSetupDeterministicLabels.cs) - Deterministic label/source helpers.
- [CaseCharacterRoster.cs](CaseCharacterRoster.cs) - Case character roster.
- [CaseSuspectFeaturePool.cs](CaseSuspectFeaturePool.cs) - Suspect feature pool.
- [RuntimeTravelRandomnessSource.cs](RuntimeTravelRandomnessSource.cs) - Runtime travel randomness source.

Back to [WildBunch.GameContent/](../INDEX.md)
