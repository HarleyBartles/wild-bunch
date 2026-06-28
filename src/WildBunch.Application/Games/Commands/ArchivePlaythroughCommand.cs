using WildBunch.Domain.Game;

namespace WildBunch.Application.Games.Commands;

public sealed record ArchivePlaythroughCommand(
    GameSessionId SessionId,
    string ArchiveReason);
