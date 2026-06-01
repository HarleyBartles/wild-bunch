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
            var marker = TrySelectPrimaryMarker(clue);
            var handleIds = new List<string>();

            if (marker is not null)
            {
                var builder = GetOrCreate(looseLeads, marker.Kind, marker.KeyValue, marker.DisplayName);
                builder.AddEvidence(clue.Id.Value);
                builder.AddSummaryLine(DescribeClueSummary(clue));
                handleIds.Add(builder.Key);
            }

            evidenceItems.Add(new CaseEvidenceItemDto(
                clue.Id.Value,
                DescribeClueKind(clue.Kind),
                clue.Source ?? "Unknown source",
                clue.Description,
                IsIdentityBearingClue(clue),
                CaseReadMapper.ToDto(clue.Anchors),
                handleIds.Distinct(StringComparer.OrdinalIgnoreCase).ToArray()));
        }

        foreach (var warrant in warrants)
        {
            var namedRecord = GetOrCreate(namedRecords, CaseIdentityKind.WarrantTarget, warrant.TargetName, warrant.TargetName);
            namedRecord.Status = CaseIdentityStatus.Resolved;
            namedRecord.AddEvidence(warrant.Id.Value);
            namedRecord.AddSummaryLine(DescribeWarrantSummary(warrant));
            namedRecord.WarrantDisposition = warrant.Terms.Disposition;
            namedRecord.BountyAmount = warrant.Terms.BountyAmount;
            namedRecord.IssuingAuthority = warrant.Terms.IssuingSource;
            namedRecord.CrimeSummary = warrant.Summary;

            foreach (var alias in warrant.Terms.KnownAliases)
            {
                namedRecord.AddRelatedLabel(alias);
                namedRecord.AddKnownAlias(alias);
                ResolveLooseLead(looseLeads, namedRecord, alias, CaseIdentityKind.Alias);
            }

            foreach (var feature in warrant.Terms.KnownFeatures)
            {
                namedRecord.AddRelatedLabel(feature);
                namedRecord.AddDistinguishingFeature(feature);
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

        if (looseLead.Kind is CaseIdentityKind.Alias)
        {
            namedRecord.AddKnownAlias(looseLead.DisplayName);
        }
        else if (looseLead.Kind is CaseIdentityKind.FeatureLed)
        {
            namedRecord.AddDistinguishingFeature(looseLead.DisplayName);
        }
    }

    private static HandleBuilder GetOrCreate(
        Dictionary<string, HandleBuilder> builders,
        CaseIdentityKind kind,
        string keyValue,
        string displayName)
    {
        var key = BuildKey(kind, keyValue);
        if (!builders.TryGetValue(key, out var builder))
        {
            builder = new HandleBuilder(key, displayName, kind);
            builders[key] = builder;
        }

        return builder;
    }

    private static bool IsIdentityBearingClue(Clue clue)
    {
        if (clue.Kind is ClueKind.Warrant or ClueKind.Alias or ClueKind.IdentityFact or ClueKind.CulpritTrail)
        {
            return true;
        }

        return TrySelectPrimaryMarker(clue) is not null;
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

    private static string BuildFeatureDisplayName(string feature)
    {
        var cleaned = Clean(feature);
        if (cleaned.Length == 0)
        {
            return "Rider with an unknown feature";
        }

        if (cleaned.StartsWith("has no ", StringComparison.OrdinalIgnoreCase))
        {
            return $"Rider with {LowerFirst(cleaned[4..])}";
        }

        if (cleaned.StartsWith("has ", StringComparison.OrdinalIgnoreCase))
        {
            return $"Rider with {LowerFirst(cleaned[4..])}";
        }

        if (cleaned.StartsWith("is missing ", StringComparison.OrdinalIgnoreCase))
        {
            return $"Rider {LowerFirst(cleaned[3..])}";
        }

        if (cleaned.StartsWith("missing ", StringComparison.OrdinalIgnoreCase))
        {
            return $"Rider {LowerFirst(cleaned)}";
        }

        if (cleaned.StartsWith("wears ", StringComparison.OrdinalIgnoreCase))
        {
            return $"Rider wearing {LowerFirst(cleaned[6..])}";
        }

        if (cleaned.StartsWith("wearing ", StringComparison.OrdinalIgnoreCase))
        {
            return $"Rider wearing {LowerFirst(cleaned[8..])}";
        }

        return $"Rider with {LowerFirst(cleaned)}";
    }

    private static string BuildRouteDisplayName(string route)
    {
        var cleaned = Clean(route);
        if (cleaned.Length == 0)
        {
            return "Rider on an unnamed route";
        }

        return $"Rider on {cleaned}";
    }

    private static IdentityMarker? TrySelectPrimaryMarker(Clue clue)
    {
        ArgumentNullException.ThrowIfNull(clue);

        var knownName = clue.Anchors.Subjects.FirstOrDefault(subject => !string.IsNullOrWhiteSpace(subject.Alias));
        if (knownName is not null)
        {
            var displayName = Clean(knownName.Alias!);
            if (displayName.Length > 0)
            {
                return new IdentityMarker(displayName, displayName, CaseIdentityKind.Alias);
            }
        }

        var featureLead = clue.Anchors.Subjects.FirstOrDefault(subject => !string.IsNullOrWhiteSpace(subject.Feature));
        if (featureLead is not null)
        {
            var feature = Clean(featureLead.Feature!);
            var displayName = BuildFeatureDisplayName(feature);
            if (feature.Length > 0 && displayName.Length > 0)
            {
                return new IdentityMarker(feature, displayName, CaseIdentityKind.FeatureLed);
            }
        }

        var route = SelectBestRoute(clue.Anchors);
        if (!string.IsNullOrWhiteSpace(route))
        {
            var cleaned = Clean(route);
            var displayName = BuildRouteDisplayName(route);
            if (cleaned.Length > 0 && displayName.Length > 0)
            {
                return new IdentityMarker(cleaned, displayName, CaseIdentityKind.RouteLed);
            }
        }

        return null;
    }

    private static string? SelectBestRoute(ClueAnchors anchors)
    {
        var route = anchors.Locations
            .Select(location => location.Route)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

        if (!string.IsNullOrWhiteSpace(route))
        {
            return route;
        }

        route = anchors.Directions
            .Select(direction => direction.Route)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

        if (!string.IsNullOrWhiteSpace(route))
        {
            return route;
        }

        var place = anchors.Locations
            .Select(location => location.Place)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

        if (!string.IsNullOrWhiteSpace(place))
        {
            return place;
        }

        route = anchors.Locations
            .Select(location => location.Label)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

        if (!string.IsNullOrWhiteSpace(route))
        {
            return route;
        }

        return anchors.Directions
            .Select(direction => direction.Label)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
    }

    private static string LowerFirst(string value)
        => value.Length == 0 ? value : char.ToLowerInvariant(value[0]) + value[1..];

    private sealed record IdentityMarker(string KeyValue, string DisplayName, CaseIdentityKind Kind);

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

        public List<string> KnownAliases { get; } = [];

        public List<string> DistinguishingFeatures { get; } = [];

        public WarrantDisposition? WarrantDisposition { get; set; }

        public decimal? BountyAmount { get; set; }

        public string? IssuingAuthority { get; set; }

        public string? CrimeSummary { get; set; }

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

        public void AddKnownAlias(string alias)
        {
            if (!string.IsNullOrWhiteSpace(alias) && !KnownAliases.Contains(alias, StringComparer.OrdinalIgnoreCase))
            {
                KnownAliases.Add(alias);
            }
        }

        public void AddDistinguishingFeature(string feature)
        {
            if (!string.IsNullOrWhiteSpace(feature) && !DistinguishingFeatures.Contains(feature, StringComparer.OrdinalIgnoreCase))
            {
                DistinguishingFeatures.Add(feature);
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
                RelatedLabels.ToArray(),
                KnownAliases.ToArray(),
                DistinguishingFeatures.ToArray(),
                WarrantDisposition,
                BountyAmount,
                IssuingAuthority,
                CrimeSummary);
    }
}
