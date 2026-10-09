using WildBunch.Domain.Cases;
using WildBunch.Domain.Events;

namespace WildBunch.Persistence.GameSessions;

internal static class CaseFileEvidenceCacheRecovery
{
    internal static bool MatchesEventEvidenceCollections(
        IReadOnlyList<IDomainEvent> events,
        CaseFile cachedCaseFile)
    {
        ArgumentNullException.ThrowIfNull(events);
        ArgumentNullException.ThrowIfNull(cachedCaseFile);

        CaseFileSnapshot? generatedCaseFile = null;
        var generatedCaseFileIndex = -1;
        for (var index = 0; index < events.Count; index++)
        {
            if (events[index] is CaseFileGenerated caseFileGenerated)
            {
                generatedCaseFile = caseFileGenerated.CaseFile;
                generatedCaseFileIndex = index;
            }
        }

        if (generatedCaseFile is null)
        {
            return true;
        }

        var cluesMatch = generatedCaseFile.Clues is null || generatedCaseFile.PublicClues is null
            || MatchesClueCollections(events, generatedCaseFile, generatedCaseFileIndex, cachedCaseFile);
        var warrantsMatch = generatedCaseFile.KnownWarrants is null || generatedCaseFile.PublicWarrants is null
            || MatchesWarrantCollections(events, generatedCaseFile, generatedCaseFileIndex, cachedCaseFile);
        var discoveredSuspectsMatch = MatchesDiscoveredSuspectIds(
            events,
            generatedCaseFile,
            generatedCaseFileIndex,
            cachedCaseFile);

        return cluesMatch && warrantsMatch && discoveredSuspectsMatch;
    }

    private static bool MatchesDiscoveredSuspectIds(
        IReadOnlyList<IDomainEvent> events,
        CaseFileSnapshot generatedCaseFile,
        int generatedCaseFileIndex,
        CaseFile cachedCaseFile)
    {
        if (generatedCaseFile.DiscoveredSuspectIds is null
            || generatedCaseFile.Suspects is null
            || generatedCaseFile.Clues is null
            || generatedCaseFile.PublicClues is null)
        {
            return false;
        }

        var expectedDiscoveredSuspectIds = generatedCaseFile.DiscoveredSuspectIds
            .ToHashSet(StringComparer.Ordinal);
        var generatedSuspectIds = generatedCaseFile.Suspects
            .Select(suspect => suspect.Id)
            .ToHashSet(StringComparer.Ordinal);
        var expectedKnownClueIds = generatedCaseFile.Clues
            .Select(clue => clue.Id)
            .ToHashSet(StringComparer.Ordinal);
        var generatedPublicClues = generatedCaseFile.PublicClues
            .GroupBy(clue => clue.Id, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        foreach (var investigation in events.Skip(generatedCaseFileIndex + 1).OfType<InvestigationPerformed>())
        {
            if (investigation.ClueId is null
                || !generatedPublicClues.TryGetValue(investigation.ClueId.Value.Value, out var clue)
                || !expectedKnownClueIds.Add(clue.Id))
            {
                continue;
            }

            foreach (var suspectId in clue.LinkedSuspectIds)
            {
                if (generatedSuspectIds.Contains(suspectId))
                {
                    expectedDiscoveredSuspectIds.Add(suspectId);
                }
            }
        }

        var actualDiscoveredSuspectIds = cachedCaseFile.DiscoveredSuspectIds
            .Select(suspectId => suspectId.Value)
            .ToHashSet(StringComparer.Ordinal);
        return expectedDiscoveredSuspectIds.Count == cachedCaseFile.DiscoveredSuspectIds.Count
            && expectedDiscoveredSuspectIds.SetEquals(actualDiscoveredSuspectIds);
    }

    private static bool MatchesClueCollections(
        IReadOnlyList<IDomainEvent> events,
        CaseFileSnapshot generatedCaseFile,
        int generatedCaseFileIndex,
        CaseFile cachedCaseFile)
    {
        var expectedKnownClues = generatedCaseFile.Clues.ToList();
        var expectedKnownClueIds = expectedKnownClues
            .Select(clue => clue.Id)
            .ToHashSet(StringComparer.Ordinal);
        var generatedPublicClues = generatedCaseFile.PublicClues
            .GroupBy(clue => clue.Id, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        var expectedPublicClues = generatedCaseFile.PublicClues
            .GroupBy(clue => clue.Id, StringComparer.Ordinal)
            .Select(group => group.First())
            .Where(clue => !expectedKnownClueIds.Contains(clue.Id))
            .ToList();

        foreach (var investigation in events.Skip(generatedCaseFileIndex + 1).OfType<InvestigationPerformed>())
        {
            if (investigation.ClueId is not null
                && generatedPublicClues.TryGetValue(investigation.ClueId.Value.Value, out var clue)
                && expectedKnownClueIds.Add(clue.Id))
            {
                expectedKnownClues.Add(clue);
                expectedPublicClues.RemoveAll(publicClue => publicClue.Id == clue.Id);
            }
        }

        return SameClueSequence(expectedKnownClues, cachedCaseFile.KnownClues.Select(ClueSnapshot.FromDomain).ToArray())
            && SameClueSequence(expectedPublicClues, cachedCaseFile.PublicClues.Select(ClueSnapshot.FromDomain).ToArray());
    }

    private static bool MatchesWarrantCollections(
        IReadOnlyList<IDomainEvent> events,
        CaseFileSnapshot generatedCaseFile,
        int generatedCaseFileIndex,
        CaseFile cachedCaseFile)
    {
        var expectedKnownWarrants = generatedCaseFile.KnownWarrants
            .GroupBy(warrant => warrant.Id, StringComparer.Ordinal)
            .Select(group => group.First())
            .ToList();
        var knownWarrantIds = expectedKnownWarrants
            .Select(warrant => warrant.Id)
            .ToHashSet(StringComparer.Ordinal);
        var generatedPublicWarrants = generatedCaseFile.PublicWarrants
            .GroupBy(warrant => warrant.Id, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        var expectedPublicWarrants = generatedCaseFile.PublicWarrants
            .GroupBy(warrant => warrant.Id, StringComparer.Ordinal)
            .Select(group => group.First())
            .ToList();

        foreach (var investigation in events.Skip(generatedCaseFileIndex + 1).OfType<InvestigationPerformed>())
        {
            if (investigation.WarrantId is null
                || !generatedPublicWarrants.TryGetValue(investigation.WarrantId.Value.Value, out var warrant)
                || !expectedPublicWarrants.Any(publicWarrant => publicWarrant.Id == warrant.Id))
            {
                continue;
            }

            expectedPublicWarrants.RemoveAll(publicWarrant => knownWarrantIds.Contains(publicWarrant.Id));
            if (expectedPublicWarrants.RemoveAll(publicWarrant => publicWarrant.Id == warrant.Id) > 0
                && knownWarrantIds.Add(warrant.Id))
            {
                expectedKnownWarrants.Add(warrant);
            }
        }

        return SameWarrantSequence(
                expectedKnownWarrants,
                cachedCaseFile.KnownWarrants.Select(WarrantSnapshot.FromDomain).ToArray())
            && SameWarrantSequence(
                expectedPublicWarrants,
                cachedCaseFile.PublicWarrants.Select(WarrantSnapshot.FromDomain).ToArray());
    }

    private static bool SameClueSequence(IReadOnlyList<ClueSnapshot> expected, IReadOnlyList<ClueSnapshot> actual)
        => expected.Count == actual.Count
            && expected.Zip(actual).All(pair => SameCluePayload(pair.First, pair.Second));

    private static bool SameWarrantSequence(IReadOnlyList<WarrantSnapshot> expected, IReadOnlyList<WarrantSnapshot> actual)
        => expected.Count == actual.Count
            && expected.Zip(actual).All(pair => SameWarrantPayload(pair.First, pair.Second));

    private static bool SameCluePayload(ClueSnapshot expected, ClueSnapshot actual)
        => expected.Id == actual.Id
            && expected.Kind == actual.Kind
            && expected.Description == actual.Description
            && expected.LinkedSuspectIds.SequenceEqual(actual.LinkedSuspectIds, StringComparer.Ordinal)
            && expected.TargetKind == actual.TargetKind
            && expected.SourceKind == actual.SourceKind
            && expected.Source == actual.Source
            && expected.Context == actual.Context
            && expected.Anchors.Subjects.SequenceEqual(actual.Anchors.Subjects)
            && expected.Anchors.Locations.SequenceEqual(actual.Anchors.Locations)
            && expected.Anchors.Times.SequenceEqual(actual.Anchors.Times)
            && expected.Anchors.Directions.SequenceEqual(actual.Anchors.Directions);

    private static bool SameWarrantPayload(WarrantSnapshot expected, WarrantSnapshot actual)
        => expected.Id == actual.Id
            && expected.TargetName == actual.TargetName
            && expected.Summary == actual.Summary
            && expected.Terms.Disposition == actual.Terms.Disposition
            && expected.Terms.BountyAmount == actual.Terms.BountyAmount
            && expected.Terms.KnownAliases.SequenceEqual(actual.Terms.KnownAliases, StringComparer.Ordinal)
            && expected.Terms.KnownFeatures.SequenceEqual(actual.Terms.KnownFeatures, StringComparer.Ordinal)
            && expected.Terms.IssuingSource == actual.Terms.IssuingSource
            && expected.Terms.TargetKind == actual.Terms.TargetKind
            && expected.Terms.GangAffiliations.SequenceEqual(actual.Terms.GangAffiliations)
            && expected.Terms.AdvancesGangPressureFor == actual.Terms.AdvancesGangPressureFor
            && expected.Terms.SourceKind == actual.Terms.SourceKind;
}
