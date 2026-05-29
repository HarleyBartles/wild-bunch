using WildBunch.Application.Games.Models;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;
using WildBunch.Domain.Journal;

namespace WildBunch.Application.Games.Mapping;

public static class JournalMapper
{
    public static JournalDto ToDto(JournalSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        return new JournalDto(
            snapshot.SessionId,
            snapshot.Status,
            new GameClockDto(snapshot.Day, snapshot.Turn),
            new JournalTownDto(snapshot.CurrentTownId.Value, snapshot.CurrentTownName),
            new JournalCaseFileDto(
                snapshot.AccusationId,
                snapshot.OpeningLead,
                ToDto(snapshot.KillerReleaseState),
                snapshot.CaseSummary,
                snapshot.Suspects.Select(ToDto).ToArray(),
                snapshot.KnownClues.Select(ToDto).ToArray()),
            snapshot.LogEntries.Select(ToDto).ToArray());
    }

    private static SuspectDto ToDto(Suspect suspect)
        => new(
            suspect.Id.Value,
            suspect.Name,
            ToDto(suspect.Profile),
            new SuspectTraitsDto(
                suspect.Traits.IsLocal,
                suspect.Traits.IsArmed,
                suspect.Traits.IsDesperate),
            suspect.Status);

    private static SuspectProfileDto ToDto(SuspectProfile profile)
        => new(
            profile.Aliases.Select(ToDto).ToArray(),
            profile.IdentifyingFacts.Select(ToDto).ToArray());

    private static SuspectAliasDto ToDto(SuspectAlias alias)
        => new(alias.Name, alias.Kind);

    private static SuspectIdentityFactDto ToDto(SuspectIdentityFact fact)
        => new(fact.Description);

    private static KillerReleaseStateDto ToDto(KillerReleaseState state)
        => new(
            state.IsReleased,
            state.Progress,
            state.RequiredPublicClues,
            state.StatusText);

    private static ClueDto ToDto(Clue clue)
        => new(
            clue.Id.Value,
            clue.Kind,
            clue.Description);

    private static GameLogEntryDto ToDto(GameLogEntry logEntry)
        => new(
            logEntry.Kind,
            logEntry.Message,
            logEntry.Day,
            logEntry.Turn);
}
