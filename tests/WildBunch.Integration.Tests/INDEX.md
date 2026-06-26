# WildBunch.Integration.Tests

Integration and acceptance tests covering the API, persistence, event store, and projections.

## Subdirectories

- [Acceptance/](Acceptance/INDEX.md) - Acceptance test scenarios.
- [Dev/](Dev/INDEX.md) - Dev endpoint integration tests.
- [TestInfrastructure/](TestInfrastructure/INDEX.md) - Shared test harness, scenario builders, and PostgreSQL fixtures.

## Key files

- [WildBunch.Integration.Tests.csproj](WildBunch.Integration.Tests.csproj) - Project file.
- [GameApiTests.cs](GameApiTests.cs) - Core game API tests.
- [GameApiActionsTests.cs](GameApiActionsTests.cs) - Game API action tests.
- [GameApiValidationTests.cs](GameApiValidationTests.cs) - Game API validation tests.
- [GameApiInvestigationActionsTests.cs](GameApiInvestigationActionsTests.cs) - Investigation action API tests.
- [GameApiJournalTests.cs](GameApiJournalTests.cs) - Journal API tests.
- [GameApiPurchaseTests.cs](GameApiPurchaseTests.cs) - Purchase API tests.
- [GameApiStoreOffersTests.cs](GameApiStoreOffersTests.cs) - Store offers API tests.
- [GameApiWantedPostersTests.cs](GameApiWantedPostersTests.cs) - Wanted posters API tests.
- [GameApiHiddenTruthTests.cs](GameApiHiddenTruthTests.cs) - Hidden truth API guardrail tests.
- [GameSessionDifficultyPersistenceTests.cs](GameSessionDifficultyPersistenceTests.cs) - Difficulty persistence tests.
- [EfGameSessionRepositoryTests.cs](EfGameSessionRepositoryTests.cs) - EF GameSession repository tests.
- [EventSourcingEndToEndTests.cs](EventSourcingEndToEndTests.cs) - Event sourcing end-to-end tests.
- [EventStorePersistenceTests.cs](EventStorePersistenceTests.cs) - Event store persistence tests.
- [PostgreSqlPersistenceTests.cs](PostgreSqlPersistenceTests.cs) - PostgreSQL persistence tests.
- [MigrationTests.cs](MigrationTests.cs) - EF migration tests.
- [ProjectionEndpointTests.cs](ProjectionEndpointTests.cs) - Projection endpoint tests.

Back to [tests/](../INDEX.md)
