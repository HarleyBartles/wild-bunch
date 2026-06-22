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

        // The case file view starts from the seed and is updated by events.
        // Currently, no events mutate the case file view (clue/journal flows are
        // not yet event-sourced). This projector is the contract for when they are.
        string? accusationId = null;
        var discoveredSuspects = seedCaseFile.Suspects.ToList();
        var knownClues = seedCaseFile.KnownClues.ToList();
        var knownWarrants = seedCaseFile.KnownWarrants.ToList();

        foreach (var e in events)
        {
            // Future event types (ClueDiscovered, SuspectAccused, etc.) will update
            // the projection here. For now, the seed case file is the projection.
            _ = e;
        }

        return new CaseFileViewProjection(
            sessionId,
            accusationId,
            seedCaseFile.OpeningLead.Description,
            discoveredSuspects,
            knownClues,
            knownWarrants);
    }
}
