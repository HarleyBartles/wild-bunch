using WildBunch.Domain.Actions;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;
using WildBunch.Domain.World;

namespace WildBunch.Domain.Tests;

public sealed class TownAggregateTests
{
    [Fact]
    public void TownAggregateOwnsTownSourceAffordancesRepeatRulesAndWantedPosterBookkeeping()
    {
        var currentTown = new Town(new TownId("current"), "Current Town", TownServices.Telegraph | TownServices.NoticeBoard);
        var connectedTown = new Town(new TownId("connected"), "Connected Town", TownServices.None);
        var aggregate = new TownAggregate(currentTown, new TownVisitState(currentTown.Id));

        Assert.Equal(currentTown.Id, aggregate.TownId);
        Assert.Equal("Current Town", aggregate.TownName);
        Assert.True(aggregate.SupportsWantedPosters);
        Assert.True(aggregate.IsAvailable(InvestigationSourceKind.TelegraphLead));
        Assert.Contains(aggregate.GetInvestigationActions(), action => action.Kind == AvailableActionKind.FollowTelegraphLeads);

        aggregate.PrimeCurrentTown();

        var firstTelegraphCheck = aggregate.CheckSource(InvestigationSourceKind.TelegraphLead);
        var repeatTelegraphCheck = aggregate.CheckSource(InvestigationSourceKind.TelegraphLead);
        var firstWantedPosterCheck = aggregate.CheckWantedPosters();
        var repeatWantedPosterCheck = aggregate.CheckWantedPosters();

        aggregate.EnterTown(connectedTown);
        aggregate.EnterTown(currentTown);

        var afterReturnTelegraphCheck = aggregate.CheckSource(InvestigationSourceKind.TelegraphLead);
        var afterReturnWantedPosterCheck = aggregate.CheckWantedPosters();

        Assert.Equal(TownSourceCheckOutcome.FirstCheck, firstTelegraphCheck);
        Assert.Equal(TownSourceCheckOutcome.RepeatNoNewInfo, repeatTelegraphCheck);
        Assert.Equal(TownSourceCheckOutcome.FirstCheck, firstWantedPosterCheck);
        Assert.Equal(TownSourceCheckOutcome.RepeatNoNewInfo, repeatWantedPosterCheck);
        Assert.Equal(TownSourceCheckOutcome.FirstCheck, afterReturnTelegraphCheck);
        Assert.Equal(TownSourceCheckOutcome.FirstCheck, afterReturnWantedPosterCheck);
        Assert.True(aggregate.VisitState.TryGetTownState(currentTown.Id, out var currentTownState));
        Assert.True(aggregate.VisitState.TryGetTownState(connectedTown.Id, out var connectedTownState));
        Assert.Equal(2, currentTownState!.VisitNumber);
        Assert.Equal(currentTown.Id, currentTownState.TownId);
        Assert.True(connectedTownState!.VisitNumber >= 1);
        Assert.False(connectedTownState.WantedPostersSpent);
        Assert.Contains(currentTownState.SpentInvestigationSources, source => source == InvestigationSourceKind.TelegraphLead);
    }
}
