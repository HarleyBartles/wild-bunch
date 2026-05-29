namespace WildBunch.Application.Games.Queries;

public sealed record GetTownStoreOffersQuery(Guid GameSessionId, string TownId);
