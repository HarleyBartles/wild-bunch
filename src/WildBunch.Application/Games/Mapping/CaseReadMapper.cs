using WildBunch.Application.Games.Models;
using WildBunch.Domain.Cases;

namespace WildBunch.Application.Games.Mapping;

public static class CaseReadMapper
{
    public static CaseStateDto ToDto(KillerReleaseState state)
        => new(ToStatusText(state));

    public static ClueDto ToDto(Clue clue)
        => new(
            clue.Id.Value,
            clue.Kind,
            clue.Description,
            clue.Source,
            clue.Context,
            ToDto(clue.Anchors));

    public static string ToLeadSummary(Clue clue)
        => $"{DescribeClueKind(clue.Kind)} clue: {NormalizeDescription(clue.Description)}";

    public static ClueAnchorsDto ToDto(ClueAnchors anchors)
        => new(
            anchors.Subjects.Select(ToDto).ToArray(),
            anchors.Locations.Select(ToDto).ToArray(),
            anchors.Times.Select(ToDto).ToArray(),
            anchors.Directions.Select(ToDto).ToArray());

    private static ClueSubjectAnchorDto ToDto(ClueSubjectAnchor anchor)
        => new(
            anchor.Label,
            anchor.Alias,
            anchor.Feature,
            anchor.Fact);

    private static ClueLocationAnchorDto ToDto(ClueLocationAnchor anchor)
        => new(
            anchor.Label,
            anchor.Place,
            anchor.Route);

    private static ClueTimeAnchorDto ToDto(ClueTimeAnchor anchor)
        => new(
            anchor.Recency,
            anchor.Day,
            anchor.Turn);

    private static ClueDirectionAnchorDto ToDto(ClueDirectionAnchor anchor)
        => new(
            anchor.Label,
            anchor.Movement,
            anchor.Route);

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

    private static string NormalizeDescription(string description)
        => description.Trim().TrimEnd('.', '!', '?');

    private static string ToStatusText(KillerReleaseState state)
        => state.IsReleased
            ? "The Wild Bunch trail has gone hot."
            : state.Progress > 0
                ? "The Wild Bunch trail is getting noisier."
                : "The Wild Bunch trail is quiet.";
}
