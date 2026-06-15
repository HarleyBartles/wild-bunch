using WildBunch.Application.Games.Mapping;
using WildBunch.Application.Games.Models;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;
using WildBunch.Domain.Journal;
using WildBunch.Domain.World;

namespace WildBunch.Application.Tests;

public sealed class JournalMapperTests
{
    [Fact]
    public void CapturedWantedRecordIsCompactAndRemovedFromActiveJournalWarrants()
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
                CreateWarrant("warrant-mira", "Mira Cline", "Red Wren", "Raven-feather pin", 2500m),
                CreateWarrant("warrant-reno", "Reno Pike", "The Magpie", "Mismatched spurs", 300m)
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

        var capturedRecord = Assert.Single(dto.CaseFile.CaseBoard.NamedRecords, record => record.DisplayName == "Mira Cline");
        Assert.Equal(CaseIdentityStatus.Captured, capturedRecord.Status);
        Assert.Contains(capturedRecord.SummaryLines, line => line.Contains("Captured alive", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(dto.CaseFile.KnownWarrants, warrant => warrant.TargetName == "Mira Cline");
        Assert.DoesNotContain(dto.CaseFile.WantedPosters, poster => poster.TargetDisplayName == "Mira Cline");
        Assert.Contains(dto.CaseFile.KnownWarrants, warrant => warrant.TargetName == "Reno Pike");
        Assert.Contains(dto.CaseFile.WantedPosters, poster => poster.TargetDisplayName == "Reno Pike");
    }

    private static Warrant CreateWarrant(
        string id,
        string targetName,
        string alias,
        string feature,
        decimal bounty)
        => new(
            new WarrantId(id),
            targetName,
            new WarrantTerms(
                WarrantDisposition.DeadOrAlive,
                bounty,
                new[] { alias },
                new[] { feature },
                "County marshal",
                InvestigationTargetKind.UnrelatedWantedCriminal,
                Array.Empty<OutlawGangId>(),
                null),
            $"Wanted notice for {targetName}.");
}
