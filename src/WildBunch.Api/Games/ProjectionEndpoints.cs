using WildBunch.Application.Abstractions;
using WildBunch.Application.Projections;
using WildBunch.Domain.Game;

namespace WildBunch.Api.Games;

public static class ProjectionEndpoints
{
    /// <summary>
    /// Player-facing safe projection endpoints. The journal has its dedicated
    /// read route; HUD remains a focused projection for the command surface.
    /// Full audit is a developer/replay surface and is NOT exposed here.
    /// </summary>
    public static IEndpointRouteBuilder MapProjectionEndpoints(this IEndpointRouteBuilder games)
    {
        var projections = games.MapGroup("/{id:guid}/projections");

        projections.MapGet("/hud", GetHudProjectionAsync)
            .WithName("GetHudProjection")
            .Produces<HudProjection>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        return games;
    }

    private static async Task<IResult> GetHudProjectionAsync(
        Guid id,
        IGameSessionRepository repository,
        HudProjector projector,
        CancellationToken cancellationToken)
    {
        var sessionId = new GameSessionId(id);
        var session = await repository.GetByIdAsync(sessionId, cancellationToken).ConfigureAwait(false);
        if (session is null)
        {
            return Results.NotFound();
        }

        var events = await repository.GetEventStreamAsync(sessionId, 0, cancellationToken).ConfigureAwait(false);
        var projection = projector.Project(events);
        return Results.Ok(projection with { SessionId = id });
    }
}
