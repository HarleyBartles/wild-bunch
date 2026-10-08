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
        var currentTown = new Town(new TownId("current"), "Current Town");
        var connectedTown = new Town(new TownId("connected"), "Connected Town");
        var aggregate = new TownAggregate(currentTown, new TownVisitState(currentTown.Id));

        Assert.Equal(currentTown.Id, aggregate.TownId);
        Assert.Equal("Current Town", aggregate.TownName);
        Assert.True(aggregate.IsAvailable(InvestigationSourceKind.LocalGossip));
        Assert.Contains(aggregate.GetInvestigationActions(), action => action.Kind == AvailableActionKind.GatherLocalGossip);

        aggregate.PrimeCurrentTown();

        var firstGossipCheck = aggregate.CheckSource(InvestigationSourceKind.LocalGossip);
        var repeatGossipCheck = aggregate.CheckSource(InvestigationSourceKind.LocalGossip);
        var firstWantedPosterCheck = aggregate.CheckWantedPosters();
        var repeatWantedPosterCheck = aggregate.CheckWantedPosters();

        aggregate.EnterTown(connectedTown);
        aggregate.EnterTown(currentTown);

        var afterReturnGossipCheck = aggregate.CheckSource(InvestigationSourceKind.LocalGossip);
        var afterReturnWantedPosterCheck = aggregate.CheckWantedPosters();

        Assert.Equal(TownSourceCheckOutcome.FirstCheck, firstGossipCheck);
        Assert.Equal(TownSourceCheckOutcome.RepeatNoNewInfo, repeatGossipCheck);
        Assert.Equal(TownSourceCheckOutcome.FirstCheck, firstWantedPosterCheck);
        Assert.Equal(TownSourceCheckOutcome.RepeatNoNewInfo, repeatWantedPosterCheck);
        Assert.Equal(TownSourceCheckOutcome.FirstCheck, afterReturnGossipCheck);
        Assert.Equal(TownSourceCheckOutcome.FirstCheck, afterReturnWantedPosterCheck);
        Assert.True(aggregate.VisitState.TryGetTownState(currentTown.Id, out var currentTownState));
        Assert.True(aggregate.VisitState.TryGetTownState(connectedTown.Id, out var connectedTownState));
        Assert.Equal(2, currentTownState!.VisitNumber);
        Assert.Equal(currentTown.Id, currentTownState.TownId);
        Assert.True(connectedTownState!.VisitNumber >= 1);
        Assert.False(connectedTownState.WantedPostersSpent);
        Assert.Contains(currentTownState.SpentInvestigationSources, source => source == InvestigationSourceKind.LocalGossip);
    }
}
