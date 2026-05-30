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
        => new(
            success,
            message,
            GameSessionMapper.ToDto(session),
            journeyStatus,
            journey is null ? null : TravelMapper.ToDto(journey),
            trailEvent is null ? null : TravelMapper.ToDto(trailEvent),
            TravelDiaryMapper.ToDto(session.TravelDiaryDays, session.TravelRules));

    public static GameTurnResultDto Create(
        bool success,
        string message,
        GameSession session)
        => new(
            success,
            message,
            GameSessionMapper.ToDto(session));
}
