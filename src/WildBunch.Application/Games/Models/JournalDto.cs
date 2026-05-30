using WildBunch.Domain.Game;

namespace WildBunch.Application.Games.Models;

public sealed record JournalDto(
    Guid Id,
    GameStatus Status,
    GameClockDto Clock,
    JournalTownDto CurrentTown,
    JournalCaseFileDto CaseFile,
    IReadOnlyList<GameLogEntryDto> LogEntries);

public sealed record JournalTownDto(
    string Id,
    string Name);

public sealed record JournalCaseFileDto(
    string OpeningLead,
    KillerReleaseStateDto KillerReleaseState,
    string CaseSummary,
    IReadOnlyList<DiscoveredSuspectDto> DiscoveredSuspects,
    IReadOnlyList<ClueDto> KnownClues);
