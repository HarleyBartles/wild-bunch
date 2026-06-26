# WildBunch.Persistence

Persistence layer: EF DbContext, event store, session repositories, migrations, and JSON serialization.

## Subdirectories

- [GameSessions/](GameSessions/INDEX.md) - EF entities, configurations, repositories, and read-store loader.
- [Migrations/](Migrations/INDEX.md) - EF Core migrations.
- [Serialization/](Serialization/INDEX.md) - JSON snapshot codecs and session rehydrator.

## Key files

- [WildBunch.Persistence.csproj](WildBunch.Persistence.csproj) - Project file.
- [WildBunchDbContext.cs](WildBunchDbContext.cs) - EF Core DbContext.
- [WildBunchDbContextFactory.cs](WildBunchDbContextFactory.cs) - DbContext design-time factory.
- [PersistenceDbContextOptions.cs](PersistenceDbContextOptions.cs) - DbContext options helpers.
- [DependencyInjection.cs](DependencyInjection.cs) - Service registration for persistence.

Back to [src/](../INDEX.md)
