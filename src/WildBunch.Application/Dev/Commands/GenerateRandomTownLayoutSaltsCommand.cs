namespace WildBunch.Application.Dev.Commands;

/// <summary>
/// Command to generate random town layout salts for exploration.
/// </summary>
public sealed record GenerateRandomTownLayoutSaltsCommand(Guid GameId);
