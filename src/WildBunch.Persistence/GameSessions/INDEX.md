# GameSessions

EF entities, entity configurations, session repositories, unit-of-work, and read-store loader.

## Key files

- [GameSessionEntity.cs](GameSessionEntity.cs) / [GameSessionEntityConfiguration.cs](GameSessionEntityConfiguration.cs) - GameSession entity and EF configuration.
- [GameSessionComponentEntity.cs](GameSessionComponentEntity.cs) / [GameSessionComponentEntityConfiguration.cs](GameSessionComponentEntityConfiguration.cs) - Composed session component entity and configuration.
- [GameSessionComponentNames.cs](GameSessionComponentNames.cs) - Component name constants.
- [GameSessionDiaryDayEntity.cs](GameSessionDiaryDayEntity.cs) / [GameSessionDiaryDayEntityConfiguration.cs](GameSessionDiaryDayEntityConfiguration.cs) - Diary day entity and configuration.
- [StoredEventEntity.cs](StoredEventEntity.cs) / [StoredEventEntityConfiguration.cs](StoredEventEntityConfiguration.cs) - Event store entity and configuration.
- [EfGameSessionRepository.cs](EfGameSessionRepository.cs) - GameSession aggregate repository (event store).
- [EfGameSessionReadRepository.cs](EfGameSessionReadRepository.cs) - GameSession read-model repository.
- [EfGameJournalReadRepository.cs](EfGameJournalReadRepository.cs) - Game journal read repository.
- [EfGameSessionUnitOfWork.cs](EfGameSessionUnitOfWork.cs) - Unit-of-work implementation.
- [GameSessionReadStoreLoader.cs](GameSessionReadStoreLoader.cs) - Loads read models from the session store.

Back to [WildBunch.Persistence/](../INDEX.md)
