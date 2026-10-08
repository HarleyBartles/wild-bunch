using WildBunch.Domain.Cases;
using WildBunch.Domain.Actions;
using WildBunch.Domain.Game;
using WildBunch.Domain.World;

namespace WildBunch.Domain.Tests;

public sealed class TownVisitStateTests
{
    [Fact]
    public void TownVisitStateTracksFirstChecksRevisitsAndTownReturnRefreshesPerTown()
    {
        var state = new TownVisitState(new TownId("current"));
        state.PrimeCurrentTown();

        var firstCheck = state.CheckSource(InvestigationSourceKind.NoticeBoard);
        var repeatCheck = state.CheckSource(InvestigationSourceKind.NoticeBoard);

        state.Reset(new TownId("connected"));
        state.Reset(new TownId("current"));

        var afterReturnCheck = state.CheckSource(InvestigationSourceKind.NoticeBoard);

        Assert.Equal(TownSourceCheckOutcome.FirstCheck, firstCheck);
        Assert.Equal(TownSourceCheckOutcome.RepeatNoNewInfo, repeatCheck);
        Assert.Equal(TownSourceCheckOutcome.FirstCheck, afterReturnCheck);
        Assert.True(state.TryGetTownState(new TownId("current"), out var currentTownState));
        Assert.True(state.TryGetTownState(new TownId("connected"), out var connectedTownState));
        Assert.Equal(2, currentTownState!.VisitNumber);
        Assert.Equal(new TownId("connected"), connectedTownState!.TownId);
        Assert.True(connectedTownState.VisitNumber >= 1);
        Assert.True(currentTownState.TryGetSourceState(InvestigationSourceKind.NoticeBoard, out var noticeBoardState));
        Assert.Equal(TownSourceRefreshPolicy.PerVisit, noticeBoardState!.RefreshPolicy);
        Assert.Equal(currentTownState.VisitNumber, noticeBoardState.LastRefreshedVisitNumber);
        Assert.True(currentTownState.IsSpent(InvestigationSourceKind.NoticeBoard));
        Assert.Single(currentTownState.SpentInvestigationSources);
        Assert.Empty(connectedTownState.SpentInvestigationSources);
    }
}
