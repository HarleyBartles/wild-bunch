namespace WildBunch.Application.Dev.Commands;

public sealed record ForceDevSaltSourceCommand(
    Guid GameSessionId,
    string? Salt);
