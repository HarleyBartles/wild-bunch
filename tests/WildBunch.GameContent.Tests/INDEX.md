# WildBunch.GameContent.Tests

Unit tests for the GameContent project (seed codec, world builder, new-game factory, flavours).

## Key files

- [WildBunch.GameContent.Tests.csproj](WildBunch.GameContent.Tests.csproj) - Project file.
- [StartingWorldDescriptorResolverTests.cs](StartingWorldDescriptorResolverTests.cs) - UUID ↔ descriptor round-trip tests.
- [StartingWorldDescriptorSeedCodeFactory.cs](StartingWorldDescriptorSeedCodeFactory.cs) - Helper that derives UUIDs from descriptors for tests.
- [SeedWorldBuilderTests.cs](SeedWorldBuilderTests.cs) - Seed world builder snapshot tests.
- [SeededNewGameFactoryTests.cs](SeededNewGameFactoryTests.cs) - Seeded new-game factory count and shape tests.
- [GameSetupPackageBuilderTests.cs](GameSetupPackageBuilderTests.cs) - Game setup package builder tests.
- [CaseCharacterRosterTests.cs](CaseCharacterRosterTests.cs) - Case character roster tests.
- [TravelDiaryFlavourCatalogTests.cs](TravelDiaryFlavourCatalogTests.cs) - Travel diary flavour catalog tests.
- [TravelTestSeedCatalog.cs](TravelTestSeedCatalog.cs) - Deterministic travel test seed catalog.
- [TravelTestSeedCatalogGuardrailTests.cs](TravelTestSeedCatalogGuardrailTests.cs) - Guardrail tests for the travel test seed catalog.

Back to [tests/](../INDEX.md)
