using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;

namespace WildBunch.Application.Games.Models;

public sealed record GameSessionReadModel(
    Guid Id,
    GameStatus Status,
    TravelDifficulty TravelDifficulty,
    AdventureRandomnessPolicy Entropy,
    Player Player,
    World World,
    CaseFile CaseFile,
    GameClock Clock,
    PursuitState PursuitState,
    TownVisitState TownVisitState,
    TravelJourneySnapshot? Journey,
    IReadOnlyList<TravelDiaryDayState> TravelDiaryDays,
    IReadOnlyList<GameLogEntry> LogEntries);
