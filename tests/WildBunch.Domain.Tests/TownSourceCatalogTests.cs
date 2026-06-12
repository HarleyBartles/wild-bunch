using WildBunch.Domain.Cases;
using WildBunch.Domain.Actions;
using WildBunch.Domain.World;

namespace WildBunch.Domain.Tests;

public sealed class TownSourceCatalogTests
{
    [Fact]
    public void TownExposesADefaultSourceCatalogWithStablePolicyMetadata()
    {
        var town = new Town(new TownId("current"), "Current Town", TownServices.None);

        var noticeBoard = town.Sources.GetRequiredDefinition(InvestigationSourceKind.NoticeBoard);
        var localRecords = town.Sources.GetRequiredDefinition(InvestigationSourceKind.LocalRecords);
        var localGossip = town.Sources.GetRequiredDefinition(InvestigationSourceKind.LocalGossip);
        var saloonLookAround = town.Sources.GetRequiredDefinition(InvestigationSourceKind.SaloonLookAround);
        var telegraphLead = town.Sources.GetRequiredDefinition(InvestigationSourceKind.TelegraphLead);

        Assert.Equal("town-source.notice-board", noticeBoard.Id);
        Assert.Equal(TownSourceAvailability.Baseline, noticeBoard.Availability);
        Assert.Equal(TownSourceLocality.TownLocal, noticeBoard.Locality);
        Assert.Equal(TownSourceRefreshPolicy.PerVisit, noticeBoard.RefreshPolicy);

        Assert.Equal("town-source.local-records", localRecords.Id);
        Assert.Equal(TownSourceAvailability.Baseline, localRecords.Availability);
        Assert.Equal(TownSourceLocality.TownLocal, localRecords.Locality);
        Assert.Equal(TownSourceRefreshPolicy.PerVisit, localRecords.RefreshPolicy);

        Assert.Equal("town-source.local-gossip", localGossip.Id);
        Assert.Equal(TownSourceAvailability.Baseline, localGossip.Availability);
        Assert.Equal(TownSourceLocality.TownLocal, localGossip.Locality);
        Assert.Equal(TownSourceRefreshPolicy.PerVisit, localGossip.RefreshPolicy);

        Assert.Equal("town-source.saloon-look-around", saloonLookAround.Id);
        Assert.Equal(TownSourceAvailability.Conditional, saloonLookAround.Availability);
        Assert.Equal(TownServices.Saloon, saloonLookAround.RequiredServices);
        Assert.Equal(TownSourceLocality.TownLocal, saloonLookAround.Locality);
        Assert.Equal(TownSourceRefreshPolicy.PerVisit, saloonLookAround.RefreshPolicy);

        Assert.Equal("town-source.telegraph-leads", telegraphLead.Id);
        Assert.Equal(TownSourceAvailability.Conditional, telegraphLead.Availability);
        Assert.Equal(TownServices.Telegraph, telegraphLead.RequiredServices);
        Assert.Equal(TownSourceLocality.Distant, telegraphLead.Locality);
        Assert.Equal(TownSourceRefreshPolicy.PerVisit, telegraphLead.RefreshPolicy);
    }

    [Fact]
    public void DefaultSourceCatalogDistinguishesBaselineAndConditionalAvailability()
    {
        var townWithoutTelegraph = new Town(new TownId("current"), "Current Town", TownServices.None);
        var townWithTelegraph = new Town(new TownId("current"), "Current Town", TownServices.Telegraph);

        Assert.True(townWithoutTelegraph.Sources.IsAvailable(InvestigationSourceKind.NoticeBoard, townWithoutTelegraph.Services));
        Assert.True(townWithoutTelegraph.Sources.IsAvailable(InvestigationSourceKind.LocalRecords, townWithoutTelegraph.Services));
        Assert.True(townWithoutTelegraph.Sources.IsAvailable(InvestigationSourceKind.LocalGossip, townWithoutTelegraph.Services));
        Assert.False(townWithoutTelegraph.Sources.IsAvailable(InvestigationSourceKind.SaloonLookAround, townWithoutTelegraph.Services));
        Assert.False(townWithoutTelegraph.Sources.IsAvailable(InvestigationSourceKind.TelegraphLead, townWithoutTelegraph.Services));

        Assert.True(townWithTelegraph.Sources.IsAvailable(InvestigationSourceKind.TelegraphLead, townWithTelegraph.Services));
    }

    [Fact]
    public void DefaultSourceCatalogProjectsTheTownInvestigationActionsFromTheCatalog()
    {
        var townWithoutTelegraph = new Town(new TownId("current"), "Current Town", TownServices.None);
        var townWithTelegraph = new Town(new TownId("current"), "Current Town", TownServices.Telegraph);

        var withoutTelegraph = townWithoutTelegraph.Sources.GetInvestigationActions(townWithoutTelegraph.Services);
        var withTelegraph = townWithTelegraph.Sources.GetInvestigationActions(townWithTelegraph.Services);

        Assert.Contains(withoutTelegraph, action => action.Kind == AvailableActionKind.InspectNoticeBoard);
        Assert.Contains(withoutTelegraph, action => action.Kind == AvailableActionKind.CheckSheriffRecords);
        Assert.Contains(withoutTelegraph, action => action.Kind == AvailableActionKind.GatherLocalGossip);
        Assert.DoesNotContain(withoutTelegraph, action => action.Kind == AvailableActionKind.LookAroundSaloon);
        Assert.DoesNotContain(withoutTelegraph, action => action.Kind == AvailableActionKind.FollowTelegraphLeads);

        var townWithSaloon = new Town(new TownId("current"), "Current Town", TownServices.Saloon);
        var withSaloon = townWithSaloon.Sources.GetInvestigationActions(townWithSaloon.Services);

        Assert.Contains(withSaloon, action => action.Kind == AvailableActionKind.LookAroundSaloon);
        Assert.Contains(withTelegraph, action => action.Kind == AvailableActionKind.FollowTelegraphLeads);
        Assert.Equal(withoutTelegraph.Count + 1, withTelegraph.Count);
    }
}
