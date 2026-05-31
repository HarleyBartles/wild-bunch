using WildBunch.Application.Games.Models;
using WildBunch.Domain.Cases;

namespace WildBunch.Application.Games.Mapping;

public static class CaseBoardMapper
{
    public static CaseBoardDto ToDto(IReadOnlyList<Clue> clues, IReadOnlyList<Warrant> warrants)
    {
        ArgumentNullException.ThrowIfNull(clues);
        ArgumentNullException.ThrowIfNull(warrants);

        var looseLeads = new Dictionary<string, HandleBuilder>(StringComparer.OrdinalIgnoreCase);
        var namedRecords = new Dictionary<string, HandleBuilder>(StringComparer.OrdinalIgnoreCase);
        var evidenceItems = new List<CaseEvidenceItemDto>(clues.Count);

        foreach (var clue in clues)
        {
            var markers = ExtractMarkers(clue);
            var handleIds = new List<string>();

            foreach (var marker in markers)
            {
                var builder = GetOrCreate(looseLeads, marker.Kind, marker.DisplayName);
                builder.AddEvidence(clue.Id.Value);
                builder.AddSummaryLine(DescribeClueSummary(clue));
                handleIds.Add(builder.Key);
            }

            evidenceItems.Add(new CaseEvidenceItemDto(
                clue.Id.Value,
                DescribeClueKind(clue.Kind),
                clue.Source ?? "Unknown source",
                clue.Description,
                markers.Count > 0,
                CaseReadMapper.ToDto(clue.Anchors),
                handleIds.Distinct(StringComparer.OrdinalIgnoreCase).ToArray()));
        }

        foreach (var warrant in warrants)
        {
            var namedRecord = GetOrCreate(namedRecords, CaseIdentityKind.WarrantTarget, warrant.TargetName);
            namedRecord.Status = CaseIdentityStatus.Resolved;
            namedRecord.AddEvidence(warrant.Id.Value);
            namedRecord.AddSummaryLine(DescribeWarrantSummary(warrant));

            foreach (var alias in warrant.Terms.KnownAliases)
            {
                namedRecord.AddRelatedLabel(alias);
                ResolveLooseLead(looseLeads, namedRecord, alias, CaseIdentityKind.Alias);
            }

            foreach (var feature in warrant.Terms.KnownFeatures)
            {
                namedRecord.AddRelatedLabel(feature);
                ResolveLooseLead(looseLeads, namedRecord, feature, CaseIdentityKind.FeatureLed);
            }
        }

        return new CaseBoardDto(
            namedRecords.Values
                .Select(builder => builder.ToDto())
                .OrderBy(record => record.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            looseLeads.Values
                .Where(builder => builder.Status is CaseIdentityStatus.Unresolved or CaseIdentityStatus.PossibleMatch)
                .Select(builder => builder.ToDto())
                .OrderBy(record => record.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            evidenceItems);
    }

    private static void ResolveLooseLead(
        Dictionary<string, HandleBuilder> looseLeads,
        HandleBuilder namedRecord,
        string label,
        CaseIdentityKind kind)
    {
        var key = BuildKey(kind, label);
        if (!looseLeads.TryGetValue(key, out var looseLead))
        {
            return;
        }

        looseLead.Status = CaseIdentityStatus.Resolved;
        looseLead.ResolvedToDisplayName = namedRecord.DisplayName;
        namedRecord.AddSummaryLine($"Linked lead: {looseLead.DisplayName}");
        namedRecord.AddEvidence(looseLead.EvidenceIds);
        namedRecord.AddRelatedLabel(looseLead.DisplayName);
    }

    private static HandleBuilder GetOrCreate(
        Dictionary<string, HandleBuilder> builders,
        CaseIdentityKind kind,
        string displayName)
    {
        var key = BuildKey(kind, displayName);
        if (!builders.TryGetValue(key, out var builder))
        {
            builder = new HandleBuilder(key, displayName, kind);
            builders[key] = builder;
        }

        return builder;
    }

    private static IReadOnlyList<IdentityMarker> ExtractMarkers(Clue clue)
    {
        if (!IsIdentityBearingClue(clue))
        {
            return [];
        }

        var markers = new List<IdentityMarker>();

        foreach (var subject in clue.Anchors.Subjects)
        {
            var addedMarker = false;

            if (!string.IsNullOrWhiteSpace(subject.Alias))
            {
                markers.Add(new IdentityMarker(subject.Alias!, CaseIdentityKind.Alias));
                addedMarker = true;
            }

            if (!string.IsNullOrWhiteSpace(subject.Feature))
            {
                markers.Add(new IdentityMarker(subject.Feature!, CaseIdentityKind.FeatureLed));
                addedMarker = true;
            }

            if (!string.IsNullOrWhiteSpace(subject.Fact))
            {
                markers.Add(new IdentityMarker(subject.Fact!, CaseIdentityKind.FeatureLed));
                addedMarker = true;
            }

            if (!addedMarker && !string.IsNullOrWhiteSpace(subject.Label))
            {
                markers.Add(new IdentityMarker(subject.Label, CaseIdentityKind.KnownName));
            }
        }

        foreach (var location in clue.Anchors.Locations)
        {
            var route = !string.IsNullOrWhiteSpace(location.Route)
                ? location.Route!
                : !string.IsNullOrWhiteSpace(location.Place)
                    ? location.Place!
                    : location.Label;

            if (!string.IsNullOrWhiteSpace(route))
            {
                markers.Add(new IdentityMarker(route, CaseIdentityKind.RouteLed));
            }
        }

        return markers
            .Select(marker => new IdentityMarker(Clean(marker.DisplayName), marker.Kind))
            .Where(marker => marker.DisplayName.Length > 0)
            .DistinctBy(marker => $"{marker.Kind}:{Normalize(marker.DisplayName)}")
            .ToArray();
    }

    private static bool IsIdentityBearingClue(Clue clue)
    {
        if (clue.Kind is ClueKind.Warrant or ClueKind.Alias or ClueKind.IdentityFact or ClueKind.CulpritTrail)
        {
            return true;
        }

        return clue.Anchors.Subjects.Any(subject =>
            !string.IsNullOrWhiteSpace(subject.Alias)
            || !string.IsNullOrWhiteSpace(subject.Feature)
            || !string.IsNullOrWhiteSpace(subject.Fact));
    }

    private static string DescribeClueSummary(Clue clue)
        => $"{DescribeClueKind(clue.Kind)} lead: {clue.Description}";

    private static string DescribeWarrantSummary(Warrant warrant)
    {
        var labels = new List<string>
        {
            $"{warrant.Terms.Disposition} warrant",
            warrant.Terms.IssuingSource,
            $"{warrant.Terms.BountyAmount:0.00} bounty"
        };

        if (!string.IsNullOrWhiteSpace(warrant.Summary))
        {
            labels.Add(warrant.Summary);
        }

        return string.Join(" - ", labels);
    }

    private static string DescribeClueKind(ClueKind kind)
        => kind switch
        {
            ClueKind.Physical => "Physical",
            ClueKind.Witness => "Witness",
            ClueKind.Record => "Record",
            ClueKind.Rumor => "Rumor",
            ClueKind.CulpritTrail => "Culprit trail",
            ClueKind.IdentityFact => "Identity fact",
            ClueKind.Alias => "Alias",
            ClueKind.Whereabouts => "Whereabouts",
            ClueKind.Warrant => "Warrant",
            ClueKind.Contradiction => "Contradiction",
            ClueKind.Context => "Context",
            _ => $"Clue {kind}"
        };

    private static string BuildKey(CaseIdentityKind kind, string value)
        => $"{kind}:{Normalize(value)}";

    private static string Normalize(string value)
        => string.Join(" ", value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant();

    private static string Clean(string value)
        => value.Trim().TrimEnd('.', '!', '?');

    private sealed record IdentityMarker(string DisplayName, CaseIdentityKind Kind);

    private sealed class HandleBuilder
    {
        public HandleBuilder(string key, string displayName, CaseIdentityKind kind)
        {
            Key = key;
            DisplayName = displayName;
            Kind = kind;
        }

        public string Key { get; }

        public string DisplayName { get; }

        public CaseIdentityKind Kind { get; }

        public CaseIdentityStatus Status { get; set; } = CaseIdentityStatus.Unresolved;

        public string? ResolvedToDisplayName { get; set; }

        public List<string> EvidenceIds { get; } = [];

        public List<string> SummaryLines { get; } = [];

        public List<string> RelatedLabels { get; } = [];

        public void AddEvidence(string evidenceId)
        {
            if (!string.IsNullOrWhiteSpace(evidenceId) && !EvidenceIds.Contains(evidenceId, StringComparer.OrdinalIgnoreCase))
            {
                EvidenceIds.Add(evidenceId);
            }
        }

        public void AddEvidence(IEnumerable<string> evidenceIds)
        {
            foreach (var evidenceId in evidenceIds)
            {
                AddEvidence(evidenceId);
            }
        }

        public void AddSummaryLine(string line)
        {
            if (!string.IsNullOrWhiteSpace(line) && !SummaryLines.Contains(line, StringComparer.OrdinalIgnoreCase))
            {
                SummaryLines.Add(line);
            }
        }

        public void AddRelatedLabel(string label)
        {
            if (!string.IsNullOrWhiteSpace(label) && !RelatedLabels.Contains(label, StringComparer.OrdinalIgnoreCase))
            {
                RelatedLabels.Add(label);
            }
        }

        public CaseIdentityHandleDto ToDto()
            => new(
                Key,
                DisplayName,
                Kind,
                Status,
                ResolvedToDisplayName,
                EvidenceIds.ToArray(),
                SummaryLines.ToArray(),
                RelatedLabels.ToArray());
    }
}
