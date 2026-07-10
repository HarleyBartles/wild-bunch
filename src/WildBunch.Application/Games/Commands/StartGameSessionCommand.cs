namespace WildBunch.Application.Games.Commands;

/// <summary>
/// Command to start a prepped game session by generating the world with dev layout salts.
/// The session must be in Prepped status and have DevLayoutSalts set.
/// </summary>
public sealed record StartGameSessionCommand(Guid GameSessionId);
