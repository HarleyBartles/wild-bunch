namespace WildBunch.Application.Dev.Queries;

/// <summary>
/// Query to get the current town layout salts for a game session.
/// </summary>
public sealed record GetTownLayoutSaltsQuery(Guid GameId);
