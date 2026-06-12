using WildBunch.Application.Games.Commands;
using WildBunch.Application.Games.Exceptions;
using WildBunch.Application.Games.Models;

namespace WildBunch.Api.Games;

public static class InvestigationEndpoints
{
    public static IEndpointRouteBuilder MapInvestigationEndpoints(this IEndpointRouteBuilder games)
    {
        games.MapPost("{id:guid}/investigations/notice-board/inspect", InspectNoticeBoardAsync)
            .WithName("InspectNoticeBoard")
            .Produces<InvestigationActionResultDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        games.MapPost("{id:guid}/investigations/local-records/check", CheckLocalRecordsAsync)
            .WithName("CheckLocalRecords")
            .Produces<InvestigationActionResultDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        games.MapPost("{id:guid}/investigations/telegraph-leads/follow", FollowTelegraphLeadsAsync)
            .WithName("FollowTelegraphLeads")
            .Produces<InvestigationActionResultDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        games.MapPost("{id:guid}/investigations/local-gossip/gather", GatherLocalGossipAsync)
            .WithName("GatherLocalGossip")
            .Produces<InvestigationActionResultDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        games.MapPost("{id:guid}/investigations/saloon/look-around", LookAroundSaloonAsync)
            .WithName("LookAroundSaloon")
            .Produces<InvestigationActionResultDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        games.MapPost("{id:guid}/investigations/saloon/confront", ConfrontSaloonWantedSuspectAsync)
            .WithName("ConfrontSaloonWantedSuspect")
            .Produces<WantedSuspectConfrontationResultDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        return games;
    }

    private static async Task<IResult> InspectNoticeBoardAsync(
        Guid id,
        InspectNoticeBoardHandler handler,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await handler.HandleAsync(new InspectNoticeBoardCommand(id), cancellationToken);
            return Results.Ok(result);
        }
        catch (GameSessionNotFoundException)
        {
            return Results.NotFound();
        }
    }

    private static async Task<IResult> CheckLocalRecordsAsync(
        Guid id,
        CheckSheriffRecordsHandler handler,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await handler.HandleAsync(new CheckSheriffRecordsCommand(id), cancellationToken);
            return Results.Ok(result);
        }
        catch (GameSessionNotFoundException)
        {
            return Results.NotFound();
        }
    }

    private static async Task<IResult> FollowTelegraphLeadsAsync(
        Guid id,
        FollowTelegraphLeadsHandler handler,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await handler.HandleAsync(new FollowTelegraphLeadsCommand(id), cancellationToken);
            return Results.Ok(result);
        }
        catch (GameSessionNotFoundException)
        {
            return Results.NotFound();
        }
    }

    private static async Task<IResult> GatherLocalGossipAsync(
        Guid id,
        GatherLocalGossipHandler handler,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await handler.HandleAsync(new GatherLocalGossipCommand(id), cancellationToken);
            return Results.Ok(result);
        }
        catch (GameSessionNotFoundException)
        {
            return Results.NotFound();
        }
    }

    private static async Task<IResult> LookAroundSaloonAsync(
        Guid id,
        LookAroundSaloonHandler handler,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await handler.HandleAsync(new LookAroundSaloonCommand(id), cancellationToken);
            return Results.Ok(result);
        }
        catch (GameSessionNotFoundException)
        {
            return Results.NotFound();
        }
    }

    private static async Task<IResult> ConfrontSaloonWantedSuspectAsync(
        Guid id,
        ConfrontSaloonWantedSuspectHandler handler,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await handler.HandleAsync(new ConfrontSaloonWantedSuspectCommand(id), cancellationToken);
            return Results.Ok(result);
        }
        catch (GameSessionNotFoundException)
        {
            return Results.NotFound();
        }
    }
}
