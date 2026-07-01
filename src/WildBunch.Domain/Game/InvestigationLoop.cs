using WildBunch.Domain.Cases;
using WildBunch.Domain.Events;
using WildBunch.Domain.World;

namespace WildBunch.Domain.Game;

/// <summary>
/// Stateless child domain component inside the session boundary that owns investigation
/// source resolution and clue/warrant surfacing decision logic. Receives narrow context records,
/// returns InvestigationPerformed events for the parent aggregate to produce. Does NOT reference
/// the parent aggregate, produce events directly, enter action context, adjust cash, or mutate
/// CaseFile/CurrentTown/TownVisitState/Player. See BUNCH-120 and ADR-0002/ADR-0020.
/// </summary>
internal sealed class InvestigationLoop
{
    // Stateless domain-service resolvers for investigation surfacing.
    // BUNCH-107: replace ordered-peek selection with town/visit-aware resolver selection.
    private static readonly WantedPosterResolver _wantedPosterResolver = new();
    private static readonly ClueSurfacingResolver _clueSurfacingResolver = new();

    /// <summary>
    /// Trims and strips trailing punctuation from a clue description for display in lead messages.
    /// </summary>
    internal static string DescribeClueLead(string description)
        => description.Trim().TrimEnd('.', '!', '?');

    /// <summary>
    /// A clue is "player known" if it is a warrant, alias, identity fact, or culprit trail clue,
    /// or if any of its anchor subjects have a non-blank alias or feature. Used to filter which
    /// clues surface from investigation sources.
    /// </summary>
    internal static bool IsPlayerKnownClue(Clue clue)
    {
        ArgumentNullException.ThrowIfNull(clue);

        if (clue.Kind is ClueKind.Warrant or ClueKind.Alias or ClueKind.IdentityFact or ClueKind.CulpritTrail)
        {
            return true;
        }

        return clue.Anchors.Subjects.Any(subject =>
            !string.IsNullOrWhiteSpace(subject.Alias)
            || !string.IsNullOrWhiteSpace(subject.Feature));
    }
}

/// <summary>
/// Read-only inputs for an investigation decision. All five investigation methods share this
/// context record; each method uses the fields it needs.
/// </summary>
internal sealed record InvestigationContext(
    CaseFile CaseFile,
    int CurrentTownSlotIndex,
    int CurrentTownVisitCount,
    SaltSource? SaltSource,  // null = boring mode (SaltSourceMode.Fixed)
    IReadOnlySet<Guid> RetiredWarrantIds,  // from BountyLoop.UnrelatedCriminalLedger
    TownId CurrentTownId,
    string CurrentTownName,
    string? BeatNarration,  // null for ReadWantedPosters (no beat narration in result)
    bool IsSourceSpent,  // CurrentTownVisit.WantedPostersSpent or IsSpent(sourceKind)
    bool IsSourceAvailable);  // CurrentTown.IsAvailable(sourceKind); true for sources always available

/// <summary>
/// Result of an investigation decision. Contains the event to produce and the display message
/// for the player-facing result. The parent aggregate produces the event and wraps the display
/// message in the appropriate result type (ReadWantedPostersResult or CaseInvestigationResult).
/// </summary>
internal sealed record InvestigationOutcome(
    InvestigationPerformed Event,
    string DisplayMessage);
