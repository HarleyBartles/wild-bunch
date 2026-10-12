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

        var session = scenario.CreateSession();
        var sessionDto = GameSessionMapper.ToDto(session);
        var preview = scenario.CreateTravelPreview(session);
        Assert.NotNull(preview.Preview);
        var previewValue = preview.Preview!;

        scenario.Fixture.AssertCreatedSession(sessionDto);

        Assert.Equal(GameDifficulty.Standard, session.GameDifficulty);
        Assert.Equal(25m, session.Player.Wallet.Cash);
        Assert.NotNull(session.Player.Inventory.GetHorseState());
        Assert.Equal(HorseTravelState.Healthy, session.Player.Inventory.GetHorseState());
        Assert.Contains(session.Player.Inventory.Items, item => item.Kind == ItemKind.Saddle);
        Assert.Equal(TravelMode.Mounted, previewValue.TravelMode);
        Assert.True(previewValue.MountedTravelAvailable);
    }

    [Fact]
    public void EasyDifficultyScenarioStartsMountedAndPreviewsMountedTravel()
    {
        var scenario = BoringScenarioBuilder.EasyDifficultyMountedTravelReady();

        var session = scenario.CreateSession();
        var sessionDto = GameSessionMapper.ToDto(session);
        var preview = scenario.CreateTravelPreview(session);
        Assert.NotNull(preview.Preview);
        var previewValue = preview.Preview!;

        scenario.Fixture.AssertCreatedSession(sessionDto);

        Assert.Equal(GameDifficulty.Easy, session.GameDifficulty);
        Assert.Equal(1250, session.Player.Health);
        Assert.NotNull(session.Player.Inventory.GetHorseState());
        Assert.Contains(session.Player.Inventory.Items, item => item.Kind == ItemKind.Horse);
        Assert.Contains(session.Player.Inventory.Items, item => item.Kind == ItemKind.Saddle);
        Assert.Equal(TravelMode.Mounted, previewValue.TravelMode);
        Assert.True(previewValue.MountedTravelAvailable);
    }

    [Fact]
    public void HighRiskFoeScenarioProducesAConnectedWorld()
    {
        var scenario = BoringScenarioBuilder.HighRiskFoeInterruptRoute();

        var session = scenario.CreateSession();
        var sessionDto = GameSessionMapper.ToDto(session);

        scenario.Fixture.AssertCreatedSession(sessionDto);

        Assert.Equal(25m, session.Player.Wallet.Cash);
        Assert.NotNull(session.Player.Inventory.GetHorseState());
        Assert.Contains(session.Player.Inventory.Items, item => item.Kind == ItemKind.Saddle);

        // Assert graph properties, not specific town names
        var connectedCount = session.World.Trails
            .Where(trail => trail.FromTownId == session.Player.CurrentTownId || trail.ToTownId == session.Player.CurrentTownId)
            .Select(trail => trail.FromTownId == session.Player.CurrentTownId ? trail.ToTownId : trail.FromTownId)
            .Distinct()
            .Count();

        Assert.True(connectedCount >= 2, $"expected at least 2 connected towns from starting town, got {connectedCount}");
    }

    [Fact]
    public void StartingTownScenarioStartsWithExpectedResources()
    {
        var scenario = BoringScenarioBuilder.StartingTownReady();

        var session = scenario.CreateSession();

        Assert.Equal(25m, session.Player.Wallet.Cash);
        Assert.NotNull(session.Player.Inventory.GetHorseState());
        Assert.Contains(session.Player.Inventory.Items, item => item.Kind == ItemKind.Saddle);
        Assert.Equal(4, session.Player.Inventory.Items.Single(item => item.Kind == ItemKind.Food).Quantity);
        Assert.Equal(3, session.Player.Inventory.Items.Single(item => item.Kind == ItemKind.HorseFeed).Quantity);
    }
}
