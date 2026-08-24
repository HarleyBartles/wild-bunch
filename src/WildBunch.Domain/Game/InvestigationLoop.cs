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
    /// Read wanted posters decision logic. Resolves a warrant and/or clue from the wanted
    /// poster resolver and clue surfacing resolver. Returns the InvestigationPerformed event
    /// and display message. The parent aggregate produces the event and wraps the display
    /// message in a ReadWantedPostersResult.
    /// </summary>
    internal InvestigationOutcome ReadWantedPosters(InvestigationContext context)
    {
        if (context.IsSourceSpent)
        {
            var msg = "You study the wanted posters again, but find nothing new.";
            return new InvestigationOutcome(
                new InvestigationPerformed
                {
                    SourceKind = InvestigationSourceKind.SheriffWarrants,
                    TownId = context.CurrentTownId,
                    Message = msg
                },
                msg);
        }

        var warrant = _wantedPosterResolver.Resolve(
            context.CaseFile,
            context.CurrentTownSlotIndex,
            context.CurrentTownVisitCount,
            context.SaltSource,
            context.RetiredWarrantIds is { Count: > 0 } ? context.RetiredWarrantIds : null);
        var clue = _clueSurfacingResolver.Resolve(
            context.CaseFile,
            InvestigationSourceKind.SheriffWarrants,
            context.CurrentTownSlotIndex,
            context.CurrentTownVisitCount,
            context.SaltSource);
        if (clue is not null && !IsPlayerKnownClue(clue))
        {
            clue = null;
        }

        if (warrant is null && clue is null)
        {
            var msg = "You study the wanted posters, but find nothing new.";
            return new InvestigationOutcome(
                new InvestigationPerformed
                {
                    SourceKind = InvestigationSourceKind.SheriffWarrants,
                    TownId = context.CurrentTownId,
                    Message = msg
                },
                msg);
        }

        if (warrant is not null && clue is not null)
        {
            var msg = $"You study the wanted posters and copy down a wanted notice for {warrant.TargetName}, noting a public lead: {DescribeClueLead(clue.Description)}.";
            return new InvestigationOutcome(
                new InvestigationPerformed
                {
                    SourceKind = InvestigationSourceKind.SheriffWarrants,
                    TownId = context.CurrentTownId,
                    Message = msg,
                    ClueId = clue?.Id,
                    WarrantId = warrant?.Id
                },
                "You study the wanted posters and uncover a wanted notice and a public lead.");
        }

        if (warrant is not null)
        {
            var msg = $"You study the wanted posters and copy down a wanted notice for {warrant.TargetName}.";
            return new InvestigationOutcome(
                new InvestigationPerformed
                {
                    SourceKind = InvestigationSourceKind.SheriffWarrants,
                    TownId = context.CurrentTownId,
                    Message = msg,
                    WarrantId = warrant?.Id
                },
                msg);
        }

        var clueOnlyMsg = $"You study the wanted posters and note a public lead: {DescribeClueLead(clue!.Description)}.";
        return new InvestigationOutcome(
            new InvestigationPerformed
            {
                SourceKind = InvestigationSourceKind.SheriffWarrants,
                TownId = context.CurrentTownId,
                Message = clueOnlyMsg,
                ClueId = clue?.Id
            },
            "You study the wanted posters and uncover a public lead.");
    }

    /// <summary>
    /// Follow telegraph leads decision logic. Resolves a clue from the clue surfacing
    /// resolver for the TelegraphLead source kind. Returns the InvestigationPerformed event
    /// and display message.
    /// </summary>
    internal InvestigationOutcome FollowTelegraphLeads(InvestigationContext context)
    {
        if (context.IsSourceSpent)
        {
            var msg = "You ask after telegraph leads again, but no new wire has come in.";
            return new InvestigationOutcome(
                new InvestigationPerformed
                {
                    SourceKind = InvestigationSourceKind.TelegraphLead,
                    TownId = context.CurrentTownId,
                    Message = msg
                },
                msg);
        }

        var clue = _clueSurfacingResolver.Resolve(
            context.CaseFile,
            InvestigationSourceKind.TelegraphLead,
            context.CurrentTownSlotIndex,
            context.CurrentTownVisitCount,
            context.SaltSource);
        if (clue is not null && !IsPlayerKnownClue(clue))
        {
            clue = null;
        }

        if (clue is null)
        {
            var msg = "You follow the telegraph leads, but find nothing new.";
            return new InvestigationOutcome(
                new InvestigationPerformed
                {
                    SourceKind = InvestigationSourceKind.TelegraphLead,
                    TownId = context.CurrentTownId,
                    Message = msg
                },
                msg);
        }

        var foundMsg = $"You follow the telegraph leads and uncover a public lead: {DescribeClueLead(clue.Description)}.";
        return new InvestigationOutcome(
            new InvestigationPerformed
            {
                SourceKind = InvestigationSourceKind.TelegraphLead,
                TownId = context.CurrentTownId,
                Message = foundMsg,
                ClueId = clue?.Id
            },
            "You follow the telegraph leads and uncover a public lead.");
    }

    /// <summary>
    /// Gather local gossip decision logic. Resolves a clue from the clue surfacing
    /// resolver for the LocalGossip source kind. Returns the InvestigationPerformed event
    /// and display message.
    /// </summary>
    internal InvestigationOutcome GatherLocalGossip(InvestigationContext context)
    {
        if (context.IsSourceSpent)
        {
            var msg = "You ask around again, but hear nothing new.";
            return new InvestigationOutcome(
                new InvestigationPerformed
                {
                    SourceKind = InvestigationSourceKind.LocalGossip,
                    TownId = context.CurrentTownId,
                    Message = msg
                },
                msg);
        }

        var clue = _clueSurfacingResolver.Resolve(
            context.CaseFile,
            InvestigationSourceKind.LocalGossip,
            context.CurrentTownSlotIndex,
            context.CurrentTownVisitCount,
            context.SaltSource);
        if (clue is not null && !IsPlayerKnownClue(clue))
        {
            clue = null;
        }

        if (clue is null)
        {
            var msg = "You ask around for local gossip, but hear nothing new.";
            return new InvestigationOutcome(
                new InvestigationPerformed
                {
                    SourceKind = InvestigationSourceKind.LocalGossip,
                    TownId = context.CurrentTownId,
                    Message = msg
                },
                msg);
        }

        var foundMsg = $"You ask around for local gossip and uncover a public lead: {DescribeClueLead(clue.Description)}.";
        return new InvestigationOutcome(
            new InvestigationPerformed
            {
                SourceKind = InvestigationSourceKind.LocalGossip,
                TownId = context.CurrentTownId,
                Message = foundMsg,
                ClueId = clue?.Id
            },
            "You ask around for local gossip and uncover a public lead.");
    }

    /// <summary>
    /// Inspect notice board decision logic. Peeks the next public clue for the NoticeBoard
    /// source kind. Returns the InvestigationPerformed event and display message.
    /// </summary>
    internal InvestigationOutcome InspectNoticeBoard(InvestigationContext context)
    {
        if (context.IsSourceSpent)
        {
            var msg = "You inspect the notice board again, but nothing new has been posted.";
            return new InvestigationOutcome(
                new InvestigationPerformed
                {
                    SourceKind = InvestigationSourceKind.NoticeBoard,
                    TownId = context.CurrentTownId,
                    Message = msg
                },
                msg);
        }

        var clue = context.CaseFile.PeekNextPublicClue(c => c.SourceKind == InvestigationSourceKind.NoticeBoard);

        if (clue is null)
        {
            var msg = "You inspect the notice board, but find nothing new.";
            return new InvestigationOutcome(
                new InvestigationPerformed
                {
                    SourceKind = InvestigationSourceKind.NoticeBoard,
                    TownId = context.CurrentTownId,
                    Message = msg
                },
                msg);
        }

        var foundMsg = $"You inspect the notice board and uncover a civic notice: {DescribeClueLead(clue.Description)}.";
        return new InvestigationOutcome(
            new InvestigationPerformed
            {
                SourceKind = InvestigationSourceKind.NoticeBoard,
                TownId = context.CurrentTownId,
                Message = foundMsg,
                ClueId = clue?.Id
            },
            "You inspect the notice board and uncover a civic notice.");
    }

    /// <summary>
    /// Check sheriff records decision logic. Peeks the next public clue for the LocalRecords
    /// source kind, filtered to player-known clues. Returns the InvestigationPerformed event
    /// and display message.
    /// </summary>
    internal InvestigationOutcome CheckSheriffRecords(InvestigationContext context)
    {
        if (context.IsSourceSpent)
        {
            var msg = "You check the local records again, but find nothing new.";
            return new InvestigationOutcome(
                new InvestigationPerformed
                {
                    SourceKind = InvestigationSourceKind.LocalRecords,
                    TownId = context.CurrentTownId,
                    Message = msg
                },
                msg);
        }

        var clue = context.CaseFile.PeekNextPublicClue(c => IsPlayerKnownClue(c) && c.SourceKind == InvestigationSourceKind.LocalRecords);

        if (clue is null)
        {
            var msg = "You check the local records, but find nothing new.";
            return new InvestigationOutcome(
                new InvestigationPerformed
                {
                    SourceKind = InvestigationSourceKind.LocalRecords,
                    TownId = context.CurrentTownId,
                    Message = msg
                },
                msg);
        }

        var foundMsg = $"You check the local records and uncover a public lead: {DescribeClueLead(clue.Description)}.";
        return new InvestigationOutcome(
            new InvestigationPerformed
            {
                SourceKind = InvestigationSourceKind.LocalRecords,
                TownId = context.CurrentTownId,
                Message = foundMsg,
                ClueId = clue?.Id
            },
            "You check the local records and uncover a public lead.");
    }

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
    IReadOnlySet<WarrantId>? RetiredWarrantIds,  // from BountyLoop.UnrelatedCriminalLedger
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
