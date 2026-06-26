# Migrations

EF Core migrations for the Wild Bunch persistence schema.

## Key files

- [20260529130641_InitialCreate.cs](20260529130641_InitialCreate.cs) - Initial create migration.
- [20260531081955_ComposedSessionPersistence.cs](20260531081955_ComposedSessionPersistence.cs) - Composed session persistence migration.
- [20260531154230_PostgresCutoverSync.cs](20260531154230_PostgresCutoverSync.cs) - PostgreSQL cutover sync migration.
- [20260531161409_JsonbPayloadStorage.cs](20260531161409_JsonbPayloadStorage.cs) - JSONB payload storage migration.
- [20260622154258_EventStore.cs](20260622154258_EventStore.cs) - Event store migration.
- [20260624104903_DropGameSessionLogEntries.cs](20260624104903_DropGameSessionLogEntries.cs) - Drop legacy game log entries migration.
- [WildBunchDbContextModelSnapshot.cs](WildBunchDbContextModelSnapshot.cs) - Current model snapshot.

Back to [WildBunch.Persistence/](../INDEX.md)
