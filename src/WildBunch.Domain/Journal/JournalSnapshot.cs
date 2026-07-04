using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;
using WildBunch.Domain.World;

namespace WildBunch.Domain.Journal;

public sealed record JournalSnapshot(
    Guid SessionId,
    GameStatus Status,
    int Day,
    int Turn,
    TownId? CurrentTownId,
    string? CurrentTownName,
    string? AccusationId,
    string OpeningLead,
    KillerReleaseState KillerReleaseState,
    string CaseSummary,
    IReadOnlyList<Suspect> DiscoveredSuspects,
    IReadOnlyList<Clue> KnownClues,
    IReadOnlyList<Warrant> KnownWarrants,
    IReadOnlyList<SheriffTurnInSettlementState> SheriffTurnInSettlements,
    IReadOnlyList<GameLogEntry> LogEntries);
