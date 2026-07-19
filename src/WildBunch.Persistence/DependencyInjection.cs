using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WildBunch.Application.Abstractions;
using WildBunch.Application.Projections;
using WildBunch.Persistence.GameSessions;
using WildBunch.Persistence.Serialization;
using WildBunch.Persistence.Versioning;

namespace WildBunch.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<GameSessionJsonSerializer>();

        // Event upcaster registry. No upcasters registered yet (greenfield repo, all events at v1).
        // When the first event shape change happens, write an IEventUpcaster and add it to
        // CreateDefaultUpcasters() below. The build-time completeness test
        // (UpcasterChainCompletenessTests) asserts every IEventUpcaster in the assembly
        // is returned by CreateDefaultUpcasters().
        services.AddSingleton<PayloadUpcasterRegistry>(_ => new PayloadUpcasterRegistry(CreateDefaultUpcasters()));

        services.AddSingleton<TravelDiaryDayProjector>();

        // PersistedPayloadLoader: the single funnel that turns persisted rows into
        // domain objects. The rebuild callback is used when a component's stored
        // version is stale — it rehydrates the full session from events and
        // extracts the component. See ADR-0028 and the event sourcing integrity
        // policy. The callback uses SessionRebuilder so the rebuild logic is
        // shared with LoadFromEventsAsync.
        services.AddSingleton<PersistedPayloadLoader>(sp =>
        {
            var eventUpcasters = sp.GetRequiredService<PayloadUpcasterRegistry>();
            var serializer = sp.GetRequiredService<GameSessionJsonSerializer>();
            var diaryDayProjector = sp.GetRequiredService<TravelDiaryDayProjector>();
            return new PersistedPayloadLoader(
                eventUpcasters,
                serializer,
                diaryDayProjector,
                rebuildSessionFromEvents: events => SessionRebuilder.RebuildFromEvents(events, serializer));
        });

        services.AddSingleton<GameSessionReadStoreLoader>();

        services.AddDbContext<WildBunchDbContext>((_, options) => PersistenceDbContextOptions.Configure(options, configuration));
        services.AddScoped<IGameSessionRepository, EfGameSessionRepository>();
        services.AddScoped<IGameSessionUnitOfWork, EfGameSessionUnitOfWork>();
        services.AddScoped<IGameSessionReadRepository, EfGameSessionReadRepository>();
        services.AddScoped<IGameJournalReadRepository, EfGameJournalReadRepository>();

        return services;
    }

    public static IServiceProvider ApplyWildBunchMigrations(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<WildBunchDbContext>();
        dbContext.Database.Migrate();
        return services;
    }

    /// <summary>
    /// Returns the list of upcasters that the DI registration uses to construct
    /// PayloadUpcasterRegistry. Extracted as a separate internal method so the
    /// build-time completeness test (UpcasterChainCompletenessTests) can call it
    /// directly and verify every IEventUpcaster in the assembly is registered.
    /// When adding a new upcaster, add it to this method's list.
    /// </summary>
    internal static IReadOnlyList<IPayloadUpcaster> CreateDefaultUpcasters()
    {
        var upcasters = new List<IPayloadUpcaster>();
        // No upcasters yet. Add upcasters here as they're written:
        // upcasters.Add(new GameStartedV1ToV2Upcaster());
        return upcasters;
    }
}
