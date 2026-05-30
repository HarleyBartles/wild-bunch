namespace WildBunch.Application.Games.Commands;

public sealed record ResolveJourneyEncounterCommand(
    Guid GameSessionId,
    string ChoiceId,
    int? BulletSpend = null,
    decimal? BribeAmount = null,
    ulong? ForcedRoll = null);
