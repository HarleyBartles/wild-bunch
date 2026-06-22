using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Exceptions;
using WildBunch.Application.Projections;
using WildBunch.Domain.Game;

namespace WildBunch.Api.Games;

public static class ProjectionEndpoints
{
    public static IEndpointRouteBuilder MapProjectionEndpoints(this IEndpointRouteBuilder games)
    {
        var projections = games.MapGroup("/{id:guid}/projections");

        projections.MapGet("/hud", GetHudProjectionAsync)
            .WithName("GetHudProjection")
            .Produces<HudProjection>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        projections.MapGet("/diary", GetDiaryProjectionAsync)
            .WithName("GetDiaryProjection")
            .Produces<DiaryProjection>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        projections.MapGet("/audit", GetAuditProjectionAsync)
            .WithName("GetAuditProjection")
            .Produces<FullAuditProjection>(StatusCodes.Status200OK)
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

    private static async Task<IResult> GetDiaryProjectionAsync(
        Guid id,
        IGameSessionRepository repository,
        DiaryProjector projector,
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

    private static async Task<IResult> GetAuditProjectionAsync(
        Guid id,
        IGameSessionRepository repository,
        FullAuditProjector projector,
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
