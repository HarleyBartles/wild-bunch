using WildBunch.Api.Dev;
using WildBunch.Api.Games;
using WildBunch.Application.Abstractions;
using WildBunch.Application.Dev.Commands;
using WildBunch.Application.Dev.Queries;
using WildBunch.Application.Games.Commands;
using WildBunch.Application.Games.Queries;
using WildBunch.Application.Projections;
using WildBunch.Domain.Actions;
using WildBunch.Domain.Economy;
using WildBunch.Domain.Journal;
using WildBunch.Domain.Travel;
using WildBunch.Persistence;
using WildBunch.GameContent;

namespace WildBunch.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddWildBunchServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("ViteDevClient", policy =>
            {
                policy.WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });
        services.AddPersistence(configuration);
        services.AddGameContent();
        services.AddSingleton<TravelResolver>();
        services.AddSingleton<ActionAvailabilityResolver>();
        services.AddSingleton<JournalResolver>();
        services.AddSingleton<TownStoreCatalogResolver>();
        services.AddScoped<StartNewGameHandler>();
        services.AddScoped<GetGameSessionHandler>();
        services.AddScoped<GetStartingTownsHandler>();
        services.AddScoped<GetPrologueHandler>();
        services.AddScoped<GetAvailableActionsHandler>();
        services.AddScoped<GetJournalHandler>();
        services.AddScoped<GetTownStoreOffersHandler>();
        services.AddScoped<PurchaseStoreItemHandler>();
        services.AddScoped<ReadWantedPostersHandler>();
        services.AddScoped<InspectNoticeBoardHandler>();
        services.AddScoped<CheckSheriffRecordsHandler>();
        services.AddScoped<FollowTelegraphLeadsHandler>();
        services.AddScoped<GatherLocalGossipHandler>();
        services.AddScoped<LookAroundSaloonHandler>();
        services.AddScoped<ConfrontWantedSuspectHandler>();
        services.AddScoped<ConfrontSaloonPersonOfInterestHandler>();
        services.AddScoped<PreviewTravelHandler>();
        services.AddScoped<TravelToTownHandler>();
        services.AddScoped<AdvanceTravelDayHandler>();
        services.AddScoped<AcknowledgeJourneyArrivalHandler>();
        services.AddScoped<ResolveJourneyEncounterHandler>();
        services.AddScoped<TurnInToSheriffHandler>();
        services.AddScoped<ArchivePlaythroughHandler>();

        // Projection projectors (safe read-model derivations from event stream)
        // Only HUD and diary are exposed through the player-facing API.
        // FullAuditProjector is a developer/replay surface, not player-facing.
        services.AddSingleton<HudProjector>();
        services.AddSingleton<DiaryProjector>();
        services.AddSingleton<FullAuditProjector>();

        // Dev-only services (gated by DevRoleGuard, separated from player-facing APIs)
        services.AddScoped<DevRoleGuard>();
        services.AddScoped<GetSessionAuditHandler>();
        services.AddScoped<GetTravelDevContextHandler>();
        services.AddScoped<ForceTravelOverrideHandler>();
        services.AddScoped<ClearTravelOverrideHandler>();
        services.AddScoped<GetSaloonDevContextHandler>();
        services.AddScoped<ForceSaloonOverrideHandler>();
        services.AddScoped<ClearSaloonOverrideHandler>();

        return services;
    }

    public static IEndpointRouteBuilder MapWildBunchApi(this IEndpointRouteBuilder app)
    {
        app.MapGameEndpoints();
        app.MapDevEndpoints();
        return app;
    }
}
