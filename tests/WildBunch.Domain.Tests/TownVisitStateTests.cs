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
        state.PrimeCurrentTown(TownSourceCatalog.Default);

        var firstCheck = state.CheckSource(InvestigationSourceKind.TelegraphLead);
        var repeatCheck = state.CheckSource(InvestigationSourceKind.TelegraphLead);

        state.Reset(new TownId("connected"));
        state.Reset(new TownId("current"));
        state.PrimeCurrentTown(TownSourceCatalog.Default);

        var afterReturnCheck = state.CheckSource(InvestigationSourceKind.TelegraphLead);

        Assert.Equal(TownSourceCheckOutcome.FirstCheck, firstCheck);
        Assert.Equal(TownSourceCheckOutcome.RepeatNoNewInfo, repeatCheck);
        Assert.Equal(TownSourceCheckOutcome.FirstCheck, afterReturnCheck);
        Assert.True(state.TryGetTownState(new TownId("current"), out var currentTownState));
        Assert.True(state.TryGetTownState(new TownId("connected"), out var connectedTownState));
        Assert.Equal(2, currentTownState!.VisitNumber);
        Assert.Equal(new TownId("connected"), connectedTownState!.TownId);
        Assert.True(connectedTownState.VisitNumber >= 1);
        Assert.True(currentTownState.TryGetSourceState(InvestigationSourceKind.TelegraphLead, out var telegraphLeadState));
        Assert.Equal(TownSourceRefreshPolicy.PerVisit, telegraphLeadState!.RefreshPolicy);
        Assert.Equal(currentTownState.VisitNumber, telegraphLeadState.LastRefreshedVisitNumber);
        Assert.True(currentTownState.IsSpent(InvestigationSourceKind.TelegraphLead));
        Assert.Single(currentTownState.SpentInvestigationSources);
        Assert.Empty(connectedTownState.SpentInvestigationSources);
    }
}
