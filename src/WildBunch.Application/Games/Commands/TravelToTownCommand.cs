namespace WildBunch.Application.Games.Commands;

public sealed record TravelToTownCommand(Guid GameSessionId, string DestinationTownId);
