using WildBunch.Application.Games.Mapping;
using WildBunch.Domain.Game;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;

namespace WildBunch.Integration.Tests.TestInfrastructure;

public sealed class BoringScenarioBuilderTests
{
    [Fact]
    public void MountedTravelReadyCreatesDeterministicMountedSession()
    {
        var scenario = BoringScenarioBuilder.MountedTravelReady();

        scenario.AssertReady();

        var session = scenario.CreateSession();
        var sessionDto = GameSessionMapper.ToDto(session);
        var preview = scenario.CreateTravelPreview(session);
        Assert.NotNull(preview.Preview);
        var previewValue = preview.Preview!;

        scenario.Fixture.AssertCreatedSession(sessionDto);

        Assert.Equal("MountedTravelReady", scenario.ScenarioName);
        Assert.Equal(scenario.Fixture.SeedCode, scenario.SeedCode);
        Assert.Equal(GameDifficulty.Standard, scenario.GameDifficulty);
        Assert.Equal(25m, session.Player.Wallet.Cash);
        Assert.NotNull(session.Player.Inventory.GetHorseState());
        Assert.Equal(HorseTravelState.Healthy, session.Player.Inventory.GetHorseState());
        Assert.Contains(session.Player.Inventory.Items, item => item.Kind == ItemKind.Saddle);
        Assert.Equal(TravelMode.Mounted, previewValue.TravelMode);
        Assert.True(previewValue.MountedTravelAvailable);
        Assert.Equal("redmesa", previewValue.DestinationTownId);
    }

    [Fact]
    public void NoHorseFootTravelReadyCreatesDeterministicSessionWithTransitionalDefaults()
    {
        // BUNCH-107 transitional: NoHorseLightEasy now gets horse+saddle (transitional defaults).
        // This test was renamed from ...FootSession to ...TransitionalDefaults to reflect
        // that horse/saddle/loadout are now difficulty-owned, not seed-owned.
        // BUNCH-94 will restore no-horse variety via difficulty-owned envelopes.
        var scenario = BoringScenarioBuilder.NoHorseFootTravelReady();

        scenario.AssertReady();

        var session = scenario.CreateSession();
        var sessionDto = GameSessionMapper.ToDto(session);
        var preview = scenario.CreateTravelPreview(session);
        Assert.NotNull(preview.Preview);
        var previewValue = preview.Preview!;

        scenario.Fixture.AssertCreatedSession(sessionDto);

        Assert.Equal("NoHorseFootTravelReady", scenario.ScenarioName);
        Assert.Equal(GameDifficulty.Easy, scenario.GameDifficulty);
        Assert.Equal(1250, session.Player.Health);
        // Transitional: all difficulties now get horse+saddle.
        Assert.NotNull(session.Player.Inventory.GetHorseState());
        Assert.Contains(session.Player.Inventory.Items, item => item.Kind == ItemKind.Horse);
        Assert.Contains(session.Player.Inventory.Items, item => item.Kind == ItemKind.Saddle);
        Assert.Equal(TravelMode.Mounted, previewValue.TravelMode);
        Assert.True(previewValue.MountedTravelAvailable);
        Assert.Equal("redmesa", previewValue.DestinationTownId);
    }

    [Fact]
    public void HighRiskFoeInterruptRouteUsesTheCachedFixtureAndKeepsTheMountedRouteShape()
    {
        var scenario = BoringScenarioBuilder.HighRiskFoeInterruptRoute();

        scenario.AssertReady();

        var session = scenario.CreateSession();
        var sessionDto = GameSessionMapper.ToDto(session);

        scenario.Fixture.AssertCreatedSession(sessionDto);

        Assert.Equal("HighRiskFoeInterruptRoute", scenario.ScenarioName);
        Assert.Equal(GameDifficulty.Standard, scenario.GameDifficulty);
        Assert.Equal(25m, session.Player.Wallet.Cash);
        Assert.NotNull(session.Player.Inventory.GetHorseState());
        Assert.Contains(session.Player.Inventory.Items, item => item.Kind == ItemKind.Saddle);
        Assert.Contains(session.World.Trails, trail => trail.FromTownId.Value == "lostcanyon" && trail.ToTownId.Value == "redmesa");
        Assert.Contains(session.World.Trails, trail => trail.FromTownId.Value == "lostcanyon" && trail.ToTownId.Value == "goldgulch");
    }

    [Fact]
    public void PinecrossServicesOrWantedPosterReadyKeepsThePublicServiceSurfaceReady()
    {
        var scenario = BoringScenarioBuilder.PinecrossServicesOrWantedPosterReady();

        scenario.AssertReady();

        var session = scenario.CreateSession();
        var sessionDto = GameSessionMapper.ToDto(session);

        scenario.Fixture.AssertCreatedSession(sessionDto);

        Assert.Equal(25m, session.Player.Wallet.Cash);
        Assert.NotNull(session.Player.Inventory.GetHorseState());
        Assert.Contains(session.Player.Inventory.Items, item => item.Kind == ItemKind.Saddle);
    }
}
