using WildBunch.Domain.Cases;
using WildBunch.Domain.Events;
using WildBunch.Domain.Game;
using WildBunch.Domain.World;

namespace WildBunch.Domain.Tests.EventSourcing;

public sealed class InvestigationEventSourcingTests
{
    [Fact]
    public void GatherLocalGossipProducesInvestigationPerformedEvent()
    {
        var session = TestSessionFactory.CreateWithPublicClue(
            InvestigationSourceKind.LocalGossip, "A dusty boot print.");

        var result = session.GatherLocalGossip();

        Assert.True(result.Success);
        Assert.True(result.SessionChanged);
        // TownActionContextEntered (Saloon) + InvestigationPerformed
        Assert.Equal(2, session.UncommittedEvents.Count);
        Assert.IsType<TownActionContextEntered>(session.UncommittedEvents[0]);
        var e = Assert.IsType<InvestigationPerformed>(session.UncommittedEvents[1]);
        Assert.Equal(InvestigationSourceKind.LocalGossip, e.SourceKind);
        Assert.NotNull(e.ClueId);
        Assert.Null(e.WarrantId);
    }

    [Fact]
    public void GatherLocalGossipNoNewInfoProducesEventWithoutClue()
    {
        var session = TestSessionFactory.CreateWithSpentSource(InvestigationSourceKind.LocalGossip);

        var result = session.GatherLocalGossip();

        Assert.True(result.Success);
        Assert.True(result.SessionChanged);
        // TownActionContextEntered (Saloon) + InvestigationPerformed
        Assert.Equal(2, session.UncommittedEvents.Count);
        Assert.IsType<TownActionContextEntered>(session.UncommittedEvents[0]);
        var e = Assert.IsType<InvestigationPerformed>(session.UncommittedEvents[1]);
        Assert.Equal(InvestigationSourceKind.LocalGossip, e.SourceKind);
        Assert.Null(e.ClueId);
        Assert.Null(e.WarrantId);
    }

    [Fact]
    public void FollowTelegraphLeadsProducesInvestigationPerformedEvent()
    {
        var session = TestSessionFactory.CreateWithPublicClue(
            InvestigationSourceKind.TelegraphLead, "A telegraph clerk filed a wire.");

        var result = session.FollowTelegraphLeads();

        Assert.True(result.Success);
        Assert.True(result.SessionChanged);
        // TownActionContextEntered (TelegraphOffice) + InvestigationPerformed
        Assert.Equal(2, session.UncommittedEvents.Count);
        Assert.IsType<TownActionContextEntered>(session.UncommittedEvents[0]);
        var e = Assert.IsType<InvestigationPerformed>(session.UncommittedEvents[1]);
        Assert.Equal(InvestigationSourceKind.TelegraphLead, e.SourceKind);
        Assert.NotNull(e.ClueId);
    }

    [Fact]
    public void InspectNoticeBoardProducesInvestigationPerformedEvent()
    {
        var session = TestSessionFactory.CreateWithPublicClue(
            InvestigationSourceKind.NoticeBoard, "A civic notice on the board.");

        var result = session.InspectNoticeBoard();

        Assert.True(result.Success);
        Assert.True(result.SessionChanged);
        // TownActionContextEntered (TownSquare) + InvestigationPerformed
        Assert.Equal(2, session.UncommittedEvents.Count);
        Assert.IsType<TownActionContextEntered>(session.UncommittedEvents[0]);
        var e = Assert.IsType<InvestigationPerformed>(session.UncommittedEvents[1]);
        Assert.Equal(InvestigationSourceKind.NoticeBoard, e.SourceKind);
        Assert.NotNull(e.ClueId);
    }

    [Fact]
    public void CheckSheriffRecordsProducesInvestigationPerformedEvent()
    {
        var session = TestSessionFactory.CreateWithPublicClue(
            InvestigationSourceKind.LocalRecords, "A sheriff ledger note.");

        var result = session.CheckSheriffRecords();

        Assert.True(result.Success);
        Assert.True(result.SessionChanged);
        // TownActionContextEntered (SheriffOffice) + InvestigationPerformed
        Assert.Equal(2, session.UncommittedEvents.Count);
        Assert.IsType<TownActionContextEntered>(session.UncommittedEvents[0]);
        var e = Assert.IsType<InvestigationPerformed>(session.UncommittedEvents[1]);
        Assert.Equal(InvestigationSourceKind.LocalRecords, e.SourceKind);
        Assert.NotNull(e.ClueId);
    }

    [Fact]
    public void ReadWantedPostersProducesEventWithWarrantAndClue()
    {
        var session = TestSessionFactory.CreateWithPublicWarrantAndClue(
            InvestigationSourceKind.SheriffWarrants);

        var result = session.ReadWantedPosters();

        Assert.True(result.Success);
        Assert.True(result.SessionChanged);
        // TownActionContextEntered (SheriffOffice) + InvestigationPerformed
        Assert.Equal(2, session.UncommittedEvents.Count);
        Assert.IsType<TownActionContextEntered>(session.UncommittedEvents[0]);
        var e = Assert.IsType<InvestigationPerformed>(session.UncommittedEvents[1]);
        Assert.Equal(InvestigationSourceKind.SheriffWarrants, e.SourceKind);
        Assert.NotNull(e.WarrantId);
        Assert.NotNull(e.ClueId);
    }

    [Fact]
    public void ReadWantedPostersSpentProducesEventWithoutWarrantOrClue()
    {
        var session = TestSessionFactory.CreateWithSpentSource(InvestigationSourceKind.SheriffWarrants);

        var result = session.ReadWantedPosters();

        Assert.True(result.Success);
        Assert.True(result.SessionChanged);
        // TownActionContextEntered (SheriffOffice) + InvestigationPerformed
        Assert.Equal(2, session.UncommittedEvents.Count);
        Assert.IsType<TownActionContextEntered>(session.UncommittedEvents[0]);
        var e = Assert.IsType<InvestigationPerformed>(session.UncommittedEvents[1]);
        Assert.Null(e.WarrantId);
        Assert.Null(e.ClueId);
    }

    [Fact]
    public void InvestigationFailedDoesNotProduceEvent()
    {
        var session = TestSessionFactory.CreateWithActiveJourney();
        session.MarkEventsCommitted();

        var result = session.GatherLocalGossip();

        Assert.False(result.Success);
        Assert.Empty(session.UncommittedEvents);
    }

    [Fact]
    public void GatherLocalGossipAdvancesClockAndDiscoversClueViaApply()
    {
        var session = TestSessionFactory.CreateWithPublicClue(
            InvestigationSourceKind.LocalGossip, "A dusty boot print.");
        var clue = session.CaseFile.PeekNextPublicClue(_ => true)!;
        var turnBefore = session.Clock.Turn;

        var result = session.GatherLocalGossip();

        Assert.True(result.Success);
        Assert.Contains(clue, session.CaseFile.KnownClues);
        Assert.DoesNotContain(clue, session.CaseFile.PublicClues);
        Assert.True(session.Clock.Turn > turnBefore);
    }

    [Fact]
    public void GatherLocalGossipMarksSourceSpentViaApply()
    {
        var session = TestSessionFactory.CreateWithPublicClue(
            InvestigationSourceKind.LocalGossip, "A dusty boot print.");

        Assert.False(session.CurrentTownVisit.IsSpent(InvestigationSourceKind.LocalGossip));

        session.GatherLocalGossip();

        Assert.True(session.CurrentTownVisit.IsSpent(InvestigationSourceKind.LocalGossip));
    }

    [Fact]
    public void ReadWantedPostersMarksWantedPostersSpentViaApply()
    {
        var session = TestSessionFactory.CreateWithPublicWarrantAndClue(
            InvestigationSourceKind.SheriffWarrants);

        Assert.False(session.CurrentTownVisit.WantedPostersSpent);

        session.ReadWantedPosters();

        Assert.True(session.CurrentTownVisit.WantedPostersSpent);
    }

    [Fact]
    public void InvestigationEventIncrementsVersion()
    {
        var session = TestSessionFactory.CreateWithPublicClue(
            InvestigationSourceKind.LocalGossip, "A dusty boot print.");
        var versionBefore = session.Version;

        session.GatherLocalGossip();

        Assert.True(session.Version > versionBefore);
    }

    [Fact]
    public void InvestigationEventCarriesTownId()
    {
        var session = TestSessionFactory.CreateWithPublicClue(
            InvestigationSourceKind.LocalGossip, "A dusty boot print.");

        session.GatherLocalGossip();

        var e = Assert.IsType<InvestigationPerformed>(session.UncommittedEvents.OfType<InvestigationPerformed>().Single());
        Assert.Equal(new TownId("current"), e.TownId);
    }
}
