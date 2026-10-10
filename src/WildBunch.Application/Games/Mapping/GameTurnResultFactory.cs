using WildBunch.Application.Games.Models;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;

namespace WildBunch.Application.Games.Mapping;

public static class GameTurnResultFactory
{
    public static GameTurnResultDto Create(
        bool success,
        string message,
        GameSession session,
        JourneyStatus? journeyStatus = null,
        TravelJourneySnapshot? journey = null,
        JourneyTrailEventState? trailEvent = null)
    {
        var currentSession = GameSessionMapper.ToDto(session);
        return new GameTurnResultDto(
            success,
            message,
            currentSession,
            journeyStatus,
            journey is null ? null : TravelMapper.ToDto(journey),
            trailEvent is null ? null : TravelMapper.ToDto(trailEvent),
            currentSession.TravelDiary);
    }

    public static GameTurnResultDto Create(
        bool success,
        string message,
        GameSession session)
        => new(
            success,
            message,
            GameSessionMapper.ToDto(session));
}
