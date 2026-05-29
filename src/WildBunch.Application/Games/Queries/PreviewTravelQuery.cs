namespace WildBunch.Application.Games.Queries;

public sealed record PreviewTravelQuery(Guid GameSessionId, string DestinationTownId);
