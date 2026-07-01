using WildBunch.Domain.Cases;
using WildBunch.Domain.Events;

namespace WildBunch.Application.Projections;

/// <summary>
/// Reference projector for the case file view projection.
/// Derives the detective's case file view from typed domain events and a seed case file.
/// The seed case file provides the initial suspects, clues, and warrants that are not
/// carried in domain events (they are static world content, not decision data).
/// This is a pure function over the event stream — no aggregate mutation.
/// See ADR-0028.
/// </summary>
public sealed class CaseFileViewProjector
{
    /// <summary>
    /// Projects the case file view from a seed case file and typed domain events.
    /// </summary>
    /// <param name="sessionId">The session ID for the projection.</param>
    /// <param name="seedCaseFile">The initial case file (static world content).</param>
    /// <param name="events">The typed domain events to project.</param>
    public CaseFileViewProjection Project(Guid sessionId, CaseFile seedCaseFile, IReadOnlyList<IDomainEvent> events)
    {
        ArgumentNullException.ThrowIfNull(seedCaseFile);
        ArgumentNullException.ThrowIfNull(events);

        string? accusationId = null;
        var discoveredSuspects = seedCaseFile.Suspects.ToList();
        var knownClues = seedCaseFile.KnownClues.ToList();
        var knownWarrants = seedCaseFile.KnownWarrants.ToList();
        var confrontations = new List<WantedSuspectConfrontationState>();
        var settlements = new List<SheriffTurnInSettlementState>();
        var revealedClueIds = new HashSet<ClueId>();
        var revealedWarrantIds = new HashSet<WarrantId>();

        foreach (var e in events)
        {
            switch (e)
            {
                case InvestigationPerformed ip:
                    if (ip.ClueId is { } clueId) revealedClueIds.Add(clueId);
                    if (ip.WarrantId is { } warrantId) revealedWarrantIds.Add(warrantId);
                    break;

                case WantedSuspectConfronted wc:
                    if (wc.Outcome is not WantedSuspectConfrontationOutcome.Abandoned)
                    {
                        confrontations.Add(new WantedSuspectConfrontationState(
                            wc.TargetSuspectId, wc.TargetName, wc.Disposition,
                            wc.Outcome, wc.IsAlive, wc.IsSecured, 0, 0));
                    }
                    break;

                case SheriffTurnInSettled st:
                    settlements.Add(new SheriffTurnInSettlementState(
                        st.TargetSuspectId, st.TargetName, st.Disposition,
                        st.IsAlive, st.BountyAmount, st.Day, st.Turn));
                    break;

                case PlaythroughArchived:
                    // No case file view state change on archive.
                    break;
            }
        }

        // If clues/warrants were revealed via events, filter the seed by revealed IDs.
        // If no events revealed clues/warrants, keep the seed (backward compatibility).
        if (revealedClueIds.Count > 0)
        {
            knownClues = seedCaseFile.KnownClues
                .Where(c => revealedClueIds.Contains(c.Id))
                .ToList();
        }

        if (revealedWarrantIds.Count > 0)
        {
            knownWarrants = seedCaseFile.KnownWarrants
                .Where(w => revealedWarrantIds.Contains(w.Id))
                .ToList();
        }

        return new CaseFileViewProjection(
            sessionId,
            accusationId,
            seedCaseFile.OpeningLead.Description,
            discoveredSuspects,
            knownClues,
            knownWarrants,
            confrontations,
            settlements);
    }
}
