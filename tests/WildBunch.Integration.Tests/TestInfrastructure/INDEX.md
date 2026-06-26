# TestInfrastructure

Shared test harness, scenario builders, deterministic randomness, and PostgreSQL fixtures.

## Key files

- [AcceptanceTestHarness.cs](AcceptanceTestHarness.cs) - Shared acceptance test harness.
- [BoringScenarioBuilder.cs](BoringScenarioBuilder.cs) - Deterministic boring scenario builder.
- [BoringScenarioBuilderTests.cs](BoringScenarioBuilderTests.cs) - Tests for the scenario builder.
- [DeterministicTravelRandomnessSource.cs](DeterministicTravelRandomnessSource.cs) - Deterministic travel randomness for tests.
- [ScenarioSeedCatalog.cs](ScenarioSeedCatalog.cs) - Scenario seed catalog for integration tests.
- [ScenarioSeedCatalogTests.cs](ScenarioSeedCatalogTests.cs) - Tests for the scenario seed catalog.
- [ScenarioSeedFixture.cs](ScenarioSeedFixture.cs) - Scenario seed fixture.
- [PostgreSqlApiFactory.cs](PostgreSqlApiFactory.cs) - PostgreSQL-backed API factory for integration tests.
- [PostgreSqlPersistenceFixture.cs](PostgreSqlPersistenceFixture.cs) - PostgreSQL persistence fixture.
- [PostgreSqlTestDatabase.cs](PostgreSqlTestDatabase.cs) - PostgreSQL test database helper.

Back to [WildBunch.Integration.Tests/](../INDEX.md)
