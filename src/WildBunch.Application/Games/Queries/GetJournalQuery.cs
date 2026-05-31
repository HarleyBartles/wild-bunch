namespace WildBunch.Application.Games.Queries;

public sealed record GetJournalQuery(Guid GameSessionId, int Skip = 0, int? Take = null);
