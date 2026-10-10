using System.Text.Json;
using WildBunch.Application.Games.Mapping;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;
using WildBunch.Domain.Journal;
using WildBunch.Domain.World;

namespace WildBunch.Application.Tests.Mappers;

public sealed class JournalMapperTests
{
    [Fact]
    public void CapturedWarrantLeavesActivePosterListByIdentityWhileCasebookRetainsAllKnownRecords()
    {
        var snapshot = new JournalSnapshot(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            GameStatus.Active,
            Day: 5,
            Turn: 2,
            new TownId("tumbleweed"),
            "Tumbleweed",
            AccusationId: null,
            "Follow the public leads and look for a signature mark.",
            new KillerReleaseState(0, 2),
            "Find the culprit before the law closes in.",
            Array.Empty<Suspect>(),
            Array.Empty<Clue>(),
            new[]
            {
                CreateWarrant("warrant-captured", "suspect-1"),
                CreateWarrant("warrant-same-name", "suspect-2"),
                CreateWarrant("warrant-legacy", targetSuspectId: null)
            },
            new[]
            {
                new SheriffTurnInSettlementState(
                    new SuspectId("suspect-1"),
                    "Mira Cline",
                    WarrantDisposition.DeadOrAlive,
                    IsAlive: true,
                    BountyAmount: 2500m,
                    Day: 5,
                    Turn: 2)
            },
            Array.Empty<GameLogEntry>());

        var dto = JournalMapper.ToDto(snapshot);

        Assert.Equal(3, dto.CaseFile.CaseBoard.Warrants.Count);
        Assert.NotNull(Assert.Single(dto.CaseFile.CaseBoard.Warrants, warrant => warrant.Id == "warrant-captured").Settlement);
        Assert.Null(Assert.Single(dto.CaseFile.CaseBoard.Warrants, warrant => warrant.Id == "warrant-same-name").Settlement);
        Assert.Null(Assert.Single(dto.CaseFile.CaseBoard.Warrants, warrant => warrant.Id == "warrant-legacy").Settlement);

        Assert.Equal(2, dto.CaseFile.KnownWarrants.Count);
        Assert.All(dto.CaseFile.KnownWarrants, warrant => Assert.Equal("Mira Cline", warrant.TargetName));
        Assert.Equal(2, dto.CaseFile.WantedPosters.Count);
        Assert.Equal(
            new[] { "warrant-legacy", "warrant-same-name" },
            dto.CaseFile.WantedPosters.Select(poster => poster.PosterId).Order(StringComparer.Ordinal));

        var serializedCaseBoard = JsonSerializer.Serialize(dto.CaseFile.CaseBoard, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.DoesNotContain("suspect-1", serializedCaseBoard, StringComparison.Ordinal);
        Assert.DoesNotContain("suspect-2", serializedCaseBoard, StringComparison.Ordinal);
    }

    private static Warrant CreateWarrant(string warrantId, string? targetSuspectId)
        => new(
            new WarrantId(warrantId),
            "Mira Cline",
            new WarrantTerms(
                WarrantDisposition.DeadOrAlive,
                2500m,
                new[] { "Red Wren" },
                new[] { "Raven-feather pin" },
                "County marshal",
                InvestigationTargetKind.GangMember,
                Array.Empty<OutlawGangId>(),
                null),
            "Wanted notice for Mira Cline.",
            targetSuspectId is null ? null : new SuspectId(targetSuspectId));
}
