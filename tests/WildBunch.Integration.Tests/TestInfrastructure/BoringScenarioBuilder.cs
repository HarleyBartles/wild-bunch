using WildBunch.Api.Games;
using WildBunch.Application.Games.Mapping;
using WildBunch.Application.Games.Models;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using WildBunch.GameContent.NewGame;

namespace WildBunch.Integration.Tests.TestInfrastructure;

internal static class BoringScenarioBuilder
{
    public static BoringScenario MountedTravelReady()
        => new(
            ScenarioName: "MountedTravelReady",
            Fixture: ScenarioSeedCatalog.CanonicalMountedStandard);

    public static BoringScenario NoHorseFootTravelReady()
        => new(
            ScenarioName: "NoHorseFootTravelReady",
            Fixture: ScenarioSeedCatalog.NoHorseLightEasy);

    public static BoringScenario HighRiskFoeInterruptRoute()
        => new(
            ScenarioName: "HighRiskFoeInterruptRoute",
            Fixture: ScenarioSeedCatalog.HighRiskFoeInterruptRoute);

    public static BoringScenario PinecrossServicesOrWantedPosterReady()
        => new(
            ScenarioName: "PinecrossServicesOrWantedPosterReady",
            Fixture: ScenarioSeedCatalog.CanonicalPinecrossServices);
}

internal sealed record BoringScenario(
    string ScenarioName,
    ScenarioSeedFixture Fixture)
{
    public string SeedCode => Fixture.SeedCode;

    public GameDifficulty GameDifficulty => Fixture.GameDifficulty;

    public void AssertReady()
        => Fixture.AssertCachedFixtureContract();

    public SetupGameRequest CreateRequest(string playerName)
        => Fixture.CreateRequest(playerName);

    public GameSession CreateSession(string playerName = "Fixture Validator")
    {
        Fixture.AssertCachedFixtureContract();

        return CanonicalStartFlow.StartGame(
            new SeededNewGameFactory(new DeterministicSaltSourceFactory()),
            playerName,
            GameDifficulty,
            SeedCode,
            Fixture.GameEntropy);
    }

    public GameSessionDto CreateSessionDto(string playerName = "Fixture Validator")
        => GameSessionMapper.ToDto(CreateSession(playerName));

    /// <summary>
    /// Discovers the first connected town from the starting town in the given session.
    /// Used by callers that need a travel destination without hardcoding town names.
    /// </summary>
    public string DiscoverFirstConnectedTownId(GameSessionDto session)
    {
        var connectedTownId = session.World.Trails
            .Where(t => t.FromTownId == session.Player.CurrentTownId || t.ToTownId == session.Player.CurrentTownId)
            .Select(t => t.FromTownId == session.Player.CurrentTownId ? t.ToTownId : t.FromTownId)
            .FirstOrDefault();

        if (connectedTownId is null)
        {
            throw new InvalidOperationException($"Scenario '{ScenarioName}': no connected town found from starting town '{session.Player.CurrentTownId}' for travel preview.");
        }

        return connectedTownId;
    }

    public TravelPreviewResultDto CreateTravelPreview(GameSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        // Discover the first connected town dynamically
        var connectedTownId = session.World.Trails
            .Where(t => t.FromTownId == session.Player.CurrentTownId || t.ToTownId == session.Player.CurrentTownId)
            .Select(t => t.FromTownId == session.Player.CurrentTownId ? t.ToTownId : t.FromTownId)
            .FirstOrDefault();

        if (connectedTownId.Value is null)
        {
            throw new InvalidOperationException($"Scenario '{ScenarioName}': no connected town found from starting town for travel preview.");
        }

        var previewResult = new TravelResolver().PreviewJourney(
            session.World,
            session.Player.CurrentTownId,
            connectedTownId,
            session.Player.Inventory,
            session.TravelRules);

        var preview = new TravelPreviewResultDto(
            previewResult.Success,
            previewResult.Message,
            previewResult.Preview is null ? null : TravelMapper.ToDto(previewResult.Preview, session.TravelRules));

        Fixture.AssertTravelPreview(GameSessionMapper.ToDto(session), connectedTownId.Value, preview);
        return preview;
    }
}
