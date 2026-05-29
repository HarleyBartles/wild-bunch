namespace WildBunch.Application.Games.Models;

public sealed record WantedPostersResultDto(
    bool Success,
    string Message,
    JournalDto CurrentJournal);
