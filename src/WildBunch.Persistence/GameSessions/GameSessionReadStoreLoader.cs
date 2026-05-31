using Microsoft.EntityFrameworkCore;
using WildBunch.Domain.Game;
using WildBunch.Domain.Journal;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using WildBunch.Application.Games.Models;
using WildBunch.Persistence.Serialization;

namespace WildBunch.Persistence.GameSessions;

internal static class GameSessionReadStoreLoader
{
    public static async Task<GameSessionReadModel?> LoadGameSessionReadModelAsync(
        WildBunchDbContext dbContext,
        GameSessionJsonSerializer serializer,
        GameSessionId sessionId,
        CancellationToken cancellationToken)
    {
        var store = await LoadStoreAsync(dbContext, serializer, sessionId, cancellationToken).ConfigureAwait(false);
        if (store is null)
        {
            return null;
        }

        var player = serializer.DeserializePlayer(GameSessionComponentPayloads.GetRequiredPayload(store.Components, GameSessionComponentNames.Player));
        var world = serializer.DeserializeWorld(GameSessionComponentPayloads.GetRequiredPayload(store.Components, GameSessionComponentNames.World));

        return new GameSessionReadModel(
            store.Envelope.Id,
            Enum.Parse<GameStatus>(store.Envelope.Status, ignoreCase: false),
            (TravelDifficulty)store.Envelope.TravelDifficulty,
            player,
            world,
            serializer.DeserializeCaseFile(GameSessionComponentPayloads.GetRequiredPayload(store.Components, GameSessionComponentNames.CaseFile)),
            serializer.DeserializeClock(GameSessionComponentPayloads.GetRequiredPayload(store.Components, GameSessionComponentNames.Clock)),
            serializer.DeserializePursuitState(GameSessionComponentPayloads.GetRequiredPayload(store.Components, GameSessionComponentNames.PursuitState)),
            GameSessionComponentPayloads.GetOptionalPayload(store.Components, GameSessionComponentNames.Journey) is { } journeyJson
                ? serializer.DeserializeJourneySnapshot(journeyJson)
                : null,
            store.TravelDiaryDays,
            store.LogEntries);
    }

    public static async Task<JournalSnapshot?> LoadJournalSnapshotAsync(
        WildBunchDbContext dbContext,
        GameSessionJsonSerializer serializer,
        GameSessionId sessionId,
        int skip,
        int? take,
        CancellationToken cancellationToken)
    {
        var store = await LoadStoreAsync(dbContext, serializer, sessionId, cancellationToken).ConfigureAwait(false);
        if (store is null)
        {
            return null;
        }

        var player = serializer.DeserializePlayer(GameSessionComponentPayloads.GetRequiredPayload(store.Components, GameSessionComponentNames.Player));
        var world = serializer.DeserializeWorld(GameSessionComponentPayloads.GetRequiredPayload(store.Components, GameSessionComponentNames.World));
        var caseFile = serializer.DeserializeCaseFile(GameSessionComponentPayloads.GetRequiredPayload(store.Components, GameSessionComponentNames.CaseFile));
        var currentTown = world.GetTown(player.CurrentTownId);
        var clock = serializer.DeserializeClock(GameSessionComponentPayloads.GetRequiredPayload(store.Components, GameSessionComponentNames.Clock));
        var logEntries = ApplySlice(store.LogEntries, skip, take);

        return new JournalSnapshot(
            store.Envelope.Id,
            Enum.Parse<GameStatus>(store.Envelope.Status, ignoreCase: false),
            clock.Day,
            clock.Turn,
            currentTown.Id,
            currentTown.Name,
            caseFile.Accusation is null ? null : caseFile.Accusation.Value.Value,
            caseFile.OpeningLead.Description,
            caseFile.KillerReleaseState,
            "Find the culprit before the law closes in.",
            caseFile.GetDiscoveredSuspects(),
            caseFile.KnownClues.ToArray(),
            caseFile.KnownWarrants.ToArray(),
            logEntries);
    }

    private static IReadOnlyList<GameLogEntry> ApplySlice(IReadOnlyList<GameLogEntry> entries, int skip, int? take)
    {
        var query = entries.Skip(Math.Max(0, skip));
        return take.HasValue ? query.Take(Math.Max(0, take.Value)).ToArray() : query.ToArray();
    }

    private static async Task<GameSessionStore?> LoadStoreAsync(
        WildBunchDbContext dbContext,
        GameSessionJsonSerializer serializer,
        GameSessionId id,
        CancellationToken cancellationToken)
    {
        var envelope = await dbContext.GameSessions.AsNoTracking().SingleOrDefaultAsync(session => session.Id == id.Value, cancellationToken).ConfigureAwait(false);
        if (envelope is null)
        {
            return null;
        }

        var components = await dbContext.GameSessionComponents.AsNoTracking()
            .Where(component => component.SessionId == id.Value)
            .ToDictionaryAsync(component => component.ComponentName, cancellationToken)
            .ConfigureAwait(false);

        var logEntries = await dbContext.GameSessionLogEntries.AsNoTracking()
            .Where(entry => entry.SessionId == id.Value)
            .OrderBy(entry => entry.Sequence)
            .Select(entry => new GameLogEntry(entry.Kind, entry.Message, entry.Day, entry.Turn))
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        var diaryDays = await dbContext.GameSessionDiaryDays.AsNoTracking()
            .Where(day => day.SessionId == id.Value)
            .OrderBy(day => day.Sequence)
            .Select(day => day.PayloadJson)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        return new GameSessionStore(
            envelope,
            components,
            logEntries,
            diaryDays.Select(serializer.DeserializeTravelDiaryDay).ToArray());
    }

    private sealed record GameSessionStore(
        GameSessionEntity Envelope,
        IReadOnlyDictionary<string, GameSessionComponentEntity> Components,
        IReadOnlyList<GameLogEntry> LogEntries,
        IReadOnlyList<TravelDiaryDayState> TravelDiaryDays);
}
