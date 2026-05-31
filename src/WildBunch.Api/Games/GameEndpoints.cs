namespace WildBunch.Api.Games;

public static class GameEndpoints
{
    public static IEndpointRouteBuilder MapGameEndpoints(this IEndpointRouteBuilder app)
    {
        var games = app.MapGroup("/api/games");

        games.MapGameSessionEndpoints();
        games.MapActionEndpoints();
        games.MapInvestigationEndpoints();
        games.MapJournalEndpoints();
        games.MapTownStoreEndpoints();
        games.MapWantedPosterEndpoints();
        games.MapTravelEndpoints();

        return app;
    }
}
