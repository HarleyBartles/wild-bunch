using WildBunch.Application.Games.Models;
using DomainCaseFile = WildBunch.Domain.Cases.CaseFile;
using DomainClue = WildBunch.Domain.Cases.Clue;
using DomainSuspect = WildBunch.Domain.Cases.Suspect;
using DomainGameLogEntry = WildBunch.Domain.Game.GameLogEntry;
using DomainGameSession = WildBunch.Domain.Game.GameSession;
using DomainPlayer = WildBunch.Domain.Game.Player;
using DomainPursuitState = WildBunch.Domain.Game.PursuitState;
using DomainWorld = WildBunch.Domain.World.World;
using DomainTown = WildBunch.Domain.World.Town;
using DomainTrail = WildBunch.Domain.World.Trail;
using DomainKillerReleaseState = WildBunch.Domain.Cases.KillerReleaseState;

namespace WildBunch.Application.Games.Mapping;

public static class GameSessionMapper
{
    public static GameSessionDto ToDto(DomainGameSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        return new GameSessionDto(
            session.Id.Value,
            session.Status,
            session.TravelDifficulty,
            ToDto(session.Player),
            ToDto(session.World),
            ToDto(session.CaseFile),
            InventoryMapper.ToDto(session.Player, session.TravelRules),
            new GameClockDto(session.Clock.Day, session.Clock.Turn),
            new PursuitStateDto(session.PursuitState.Heat),
            session.Journey is null ? null : TravelMapper.ToDto(session.Journey, session.TravelRules),
            TravelDiaryMapper.ToDto(session.TravelDiaryDays, session.TravelRules),
            session.LogEntries.Select(ToDto).ToArray());
    }

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
            ToDto(caseFile.KillerReleaseState),
            caseFile.GetDiscoveredSuspects().Select(ToDto).ToArray(),
            caseFile.KnownClues.Select(ToDto).ToArray());

    private static KillerReleaseStateDto ToDto(DomainKillerReleaseState state)
        => new(
            state.IsReleased,
            state.Progress,
            state.RequiredPublicClues,
            state.StatusText);

    private static DiscoveredSuspectDto ToDto(DomainSuspect suspect)
        => new(
            suspect.Id.Value,
            suspect.Name,
            suspect.Status);

    private static ClueDto ToDto(DomainClue clue)
        => new(
            clue.Id.Value,
            clue.Kind,
            clue.Description);

    private static GameLogEntryDto ToDto(DomainGameLogEntry logEntry)
        => new(
            logEntry.Kind,
            logEntry.Message,
            logEntry.Day,
            logEntry.Turn);
}
