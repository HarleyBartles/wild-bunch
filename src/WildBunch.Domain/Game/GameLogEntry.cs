namespace WildBunch.Domain.Game;

public sealed record GameLogEntry(GameLogEntryKind Kind, string Message, int Day, int Turn);
