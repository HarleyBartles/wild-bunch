namespace WildBunch.Application.Dev.Commands;

public sealed record ForceSaloonOverrideCommand(
    Guid GameSessionId,
    string ForcedKind,
    string? ForcedSuspectId,
    string? ForcedCitizenRoleKey);
