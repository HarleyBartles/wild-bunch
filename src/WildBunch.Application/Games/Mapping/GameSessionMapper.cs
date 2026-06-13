using WildBunch.Application.Games.Models;
using WildBunch.Domain.Cases;
using DomainCaseFile = WildBunch.Domain.Cases.CaseFile;
using DomainSuspect = WildBunch.Domain.Cases.Suspect;
using DomainGameLogEntry = WildBunch.Domain.Game.GameLogEntry;
using DomainGameSession = WildBunch.Domain.Game.GameSession;
using DomainGameSessionReadModel = WildBunch.Application.Games.Models.GameSessionReadModel;
using DomainPlayer = WildBunch.Domain.Game.Player;
using DomainPursuitState = WildBunch.Domain.Game.PursuitState;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using DomainWorld = WildBunch.Domain.World.World;
using DomainTown = WildBunch.Domain.World.Town;
using DomainTrail = WildBunch.Domain.World.Trail;
using DomainTravelDiaryDayState = WildBunch.Domain.Travel.TravelDiaryDayState;
using DomainTravelRulesProfile = WildBunch.Domain.Travel.TravelRulesProfile;

namespace WildBunch.Application.Games.Mapping;

public static class GameSessionMapper
{
    public static GameSessionDto ToDto(DomainGameSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        return ToDto(
            session.Id.Value,
            session.Status,
            session.TravelDifficulty,
            session.Entropy,
            session.Player,
            session.World,
            session.CaseFile,
            session.Clock,
            session.PursuitState,
            session.Journey is null ? null : TravelMapper.ToDto(session.Journey, session.TravelRules),
            session.TravelDiaryDays,
            session.LogEntries,
            ToActiveSaloonWantedSuspectDto(session.CurrentTownVisit.CurrentTownState.ActiveSaloonWantedSuspectId, session.CaseFile));
    }

    public static GameSessionDto ToDto(DomainGameSessionReadModel session)
    {
        ArgumentNullException.ThrowIfNull(session);

        return ToDto(
            session.Id,
            session.Status,
            session.TravelDifficulty,
            session.Entropy,
            session.Player,
            session.World,
            session.CaseFile,
            session.Clock,
            session.PursuitState,
            session.Journey is null ? null : TravelMapper.ToDto(session.Journey, TravelRulesProfile.For(session.TravelDifficulty)),
            session.TravelDiaryDays,
            session.LogEntries,
            ToActiveSaloonWantedSuspectDto(session.TownVisitState.CurrentTownState.ActiveSaloonWantedSuspectId, session.CaseFile));
    }

    private static GameSessionDto ToDto(
        Guid id,
        GameStatus status,
        TravelDifficulty travelDifficulty,
        AdventureRandomnessPolicy entropy,
        DomainPlayer player,
        DomainWorld world,
        DomainCaseFile caseFile,
        GameClock clock,
        DomainPursuitState pursuitState,
        TravelJourneyDto? journey,
        IReadOnlyList<DomainTravelDiaryDayState> travelDiaryDays,
        IReadOnlyList<DomainGameLogEntry> logEntries,
        ActiveSaloonWantedSuspectDto? activeSaloonWantedSuspect)
        => new(
            id,
            status,
            travelDifficulty,
            entropy,
            ToDto(player),
            ToDto(world),
            ToDto(caseFile),
            InventoryMapper.ToDto(player, TravelRulesProfile.For(travelDifficulty)),
            new GameClockDto(clock.Day, clock.Turn),
            new PursuitStateDto(pursuitState.Heat),
            journey,
            TravelDiaryMapper.ToDto(travelDiaryDays, TravelRulesProfile.For(travelDifficulty)),
            logEntries.Select(ToDto).ToArray(),
            activeSaloonWantedSuspect);

    private static PlayerDto ToDto(DomainPlayer player)
        => new(
            player.Name,
            player.CurrentTownId.Value,
            player.Health);

    private static WorldDto ToDto(DomainWorld world)
        => new(
            world.Towns.Select(ToDto).ToArray(),
            world.Trails.Select(ToDto).ToArray());

    private static TownDto ToDto(DomainTown town)
        => new(
            town.Id.Value,
            town.Name,
            town.Services);

    private static TrailDto ToDto(DomainTrail trail)
        => new(
            trail.Id.Value,
            trail.FromTownId.Value,
            trail.ToTownId.Value,
            trail.Risk,
            trail.Terrain,
            trail.WaterFeature,
            trail.RideDayDistance);

    private static CaseFileDto ToDto(DomainCaseFile caseFile)
        => new(
            caseFile.OpeningLead.Description,
            CaseReadMapper.ToDto(caseFile.KillerReleaseState),
            caseFile.GetDiscoveredSuspects().Select(ToDto).ToArray(),
            CaseBoardMapper.ToDto(caseFile.KnownClues, caseFile.KnownWarrants),
            caseFile.KnownClues.Select(CaseReadMapper.ToDto).ToArray());

    private static DiscoveredSuspectDto ToDto(DomainSuspect suspect)
        => new(
            suspect.Id.Value,
            suspect.Name,
            suspect.Status);

    private static GameLogEntryDto ToDto(DomainGameLogEntry logEntry)
        => new(
            logEntry.Kind,
            logEntry.Message,
            logEntry.Day,
            logEntry.Turn);

    private static ActiveSaloonWantedSuspectDto? ToActiveSaloonWantedSuspectDto(
        SuspectId? activeSaloonWantedSuspectId,
        DomainCaseFile caseFile)
    {
        if (activeSaloonWantedSuspectId is null)
        {
            return null;
        }

        var targetName = caseFile.Suspects.FirstOrDefault(suspect => suspect.Id.Equals(activeSaloonWantedSuspectId))?.Name;
        return targetName is null
            ? null
            : new ActiveSaloonWantedSuspectDto(targetName);
    }
}
