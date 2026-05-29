namespace WildBunch.Application.Games.Commands;

public sealed record ResolveJourneyEncounterCommand(Guid GameSessionId, string ChoiceId);
