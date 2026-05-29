using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;
using WildBunch.Domain.World;

namespace WildBunch.Domain.Journal;

public sealed record JournalSnapshot(
    Guid SessionId,
    GameStatus Status,
    int Day,
    int Turn,
    TownId CurrentTownId,
    string CurrentTownName,
    string? AccusationId,
    string CaseSummary,
    IReadOnlyList<Suspect> Suspects,
    IReadOnlyList<Clue> KnownClues,
    IReadOnlyList<GameLogEntry> LogEntries);
