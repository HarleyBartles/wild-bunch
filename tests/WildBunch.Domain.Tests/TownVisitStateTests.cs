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

        var firstCheck = state.CheckSource(InvestigationSourceKind.LocalGossip);
        var repeatCheck = state.CheckSource(InvestigationSourceKind.LocalGossip);

        state.Reset(new TownId("connected"));
        state.Reset(new TownId("current"));

        var afterReturnCheck = state.CheckSource(InvestigationSourceKind.LocalGossip);

        Assert.Equal(TownSourceCheckOutcome.FirstCheck, firstCheck);
        Assert.Equal(TownSourceCheckOutcome.RepeatNoNewInfo, repeatCheck);
        Assert.Equal(TownSourceCheckOutcome.FirstCheck, afterReturnCheck);
        Assert.True(state.TryGetTownState(new TownId("current"), out var currentTownState));
        Assert.True(state.TryGetTownState(new TownId("connected"), out var connectedTownState));
        Assert.Equal(2, currentTownState!.VisitNumber);
        Assert.Equal(new TownId("connected"), connectedTownState!.TownId);
        Assert.True(connectedTownState.VisitNumber >= 1);
        Assert.True(currentTownState.TryGetSourceState(InvestigationSourceKind.LocalGossip, out var gossipState));
        Assert.Equal(TownSourceRefreshPolicy.PerVisit, gossipState!.RefreshPolicy);
        Assert.Equal(currentTownState.VisitNumber, gossipState.LastRefreshedVisitNumber);
        Assert.True(currentTownState.IsSpent(InvestigationSourceKind.LocalGossip));
        Assert.Single(currentTownState.SpentInvestigationSources);
        Assert.Empty(connectedTownState.SpentInvestigationSources);
    }
}
