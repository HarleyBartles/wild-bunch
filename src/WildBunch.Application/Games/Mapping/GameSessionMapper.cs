using WildBunch.Application.Games.Models;
using DomainCaseFile = WildBunch.Domain.Cases.CaseFile;
using DomainClue = WildBunch.Domain.Cases.Clue;
using DomainGameLogEntry = WildBunch.Domain.Game.GameLogEntry;
using DomainGameSession = WildBunch.Domain.Game.GameSession;
using DomainPlayer = WildBunch.Domain.Game.Player;
using DomainPursuitState = WildBunch.Domain.Game.PursuitState;
using DomainWorld = WildBunch.Domain.World.World;
using DomainTown = WildBunch.Domain.World.Town;
using DomainTrail = WildBunch.Domain.World.Trail;
using DomainSuspect = WildBunch.Domain.Cases.Suspect;
using DomainSuspectAlias = WildBunch.Domain.Cases.SuspectAlias;
using DomainSuspectIdentityFact = WildBunch.Domain.Cases.SuspectIdentityFact;
using DomainSuspectProfile = WildBunch.Domain.Cases.SuspectProfile;
using DomainSuspectTraits = WildBunch.Domain.Cases.SuspectTraits;
using DomainKillerReleaseState = WildBunch.Domain.Cases.KillerReleaseState;
using TownId = WildBunch.Domain.World.TownId;
using TrailId = WildBunch.Domain.World.TrailId;
using SuspectId = WildBunch.Domain.Cases.SuspectId;
using ClueId = WildBunch.Domain.Cases.ClueId;

namespace WildBunch.Application.Games.Mapping;

public static class GameSessionMapper
{
    public static GameSessionDto ToDto(DomainGameSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        return new GameSessionDto(
            session.Id.Value,
            session.Status,
            ToDto(session.Player),
            ToDto(session.World),
            ToDto(session.CaseFile),
            InventoryMapper.ToDto(session.Player),
            new GameClockDto(session.Clock.Day, session.Clock.Turn),
            new PursuitStateDto(session.PursuitState.Heat),
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
            trail.Risk);

    private static CaseFileDto ToDto(DomainCaseFile caseFile)
    {
        string? accusationId = caseFile.Accusation.HasValue ? caseFile.Accusation.Value.Value : null;

        return new CaseFileDto(
            accusationId,
            caseFile.OpeningLead.Description,
            ToDto(caseFile.KillerReleaseState),
            caseFile.Suspects.Select(ToDto).ToArray(),
            caseFile.KnownClues.Select(ToDto).ToArray());
    }

    private static SuspectDto ToDto(DomainSuspect suspect)
        => new(
            suspect.Id.Value,
            suspect.Name,
            ToDto(suspect.Profile),
            new SuspectTraitsDto(
                suspect.Traits.IsLocal,
                suspect.Traits.IsArmed,
                suspect.Traits.IsDesperate),
            suspect.Status);

    private static SuspectProfileDto ToDto(DomainSuspectProfile profile)
        => new(
            profile.Aliases.Select(ToDto).ToArray(),
            profile.IdentifyingFacts.Select(ToDto).ToArray());

    private static SuspectAliasDto ToDto(DomainSuspectAlias alias)
        => new(alias.Name, alias.Kind);

    private static SuspectIdentityFactDto ToDto(DomainSuspectIdentityFact fact)
        => new(fact.Description);

    private static KillerReleaseStateDto ToDto(DomainKillerReleaseState state)
        => new(
            state.IsReleased,
            state.Progress,
            state.RequiredPublicClues,
            state.StatusText);

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
