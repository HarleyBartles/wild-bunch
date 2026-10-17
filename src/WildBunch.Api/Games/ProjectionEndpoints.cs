using WildBunch.Application.Games.Exceptions;
using WildBunch.Application.Games.Queries;
using WildBunch.Application.Projections;

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
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        return games;
    }

    private static async Task<IResult> GetHudProjectionAsync(
        Guid id,
        GetHudProjectionHandler handler,
        CancellationToken cancellationToken)
    {
        try
        {
            var projection = await handler.HandleAsync(new GetHudProjectionQuery(id), cancellationToken)
                .ConfigureAwait(false);
            return projection is null ? Results.NoContent() : Results.Ok(projection);
        }
        catch (GameSessionNotFoundException)
        {
            return Results.NotFound();
        }
    }
}
