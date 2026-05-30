namespace WildBunch.Api.Games;

public sealed record ResolveJourneyEncounterRequest(string ChoiceId, int? BulletSpend = null, decimal? BribeAmount = null);
