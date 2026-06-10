namespace WildBunch.Application.Games.Commands;

public sealed record TurnInToSheriffCommand(Guid GameSessionId, string TargetSuspectId, bool IsAlive);
