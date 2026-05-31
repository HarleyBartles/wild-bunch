namespace WildBunch.Application.Games.Models;

public sealed record InvestigationActionResultDto(
    bool Success,
    string Message,
    JournalDto CurrentJournal);
