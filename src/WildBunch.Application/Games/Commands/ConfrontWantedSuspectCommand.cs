using WildBunch.Domain.Cases;

namespace WildBunch.Application.Games.Commands;

public sealed record ConfrontWantedSuspectCommand(
    Guid GameSessionId,
    string TargetSuspectId,
    WantedSuspectConfrontationChoice Choice);
