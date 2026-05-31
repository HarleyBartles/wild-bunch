using WildBunch.Domain.Cases;
using WildBunch.Domain.World;

namespace WildBunch.Domain.Actions;

public sealed record InvestigationSourceDefinition(
    InvestigationSourceKind Kind,
    AvailableActionKind ActionKind,
    string Label,
    TownServices RequiredServices);

public static class InvestigationSources
{
    public static readonly InvestigationSourceDefinition NoticeBoard = new(
        InvestigationSourceKind.NoticeBoard,
        AvailableActionKind.InspectNoticeBoard,
        "Inspect notice board",
        TownServices.NoticeBoard);

    public static readonly InvestigationSourceDefinition SheriffRecords = new(
        InvestigationSourceKind.SheriffRecords,
        AvailableActionKind.CheckSheriffRecords,
        "Check sheriff records",
        TownServices.NoticeBoard);

    public static IReadOnlyList<InvestigationSourceDefinition> All { get; } = [NoticeBoard, SheriffRecords];
}
