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
            Fixture: ScenarioSeedCatalog.CanonicalMountedStandard,
            PreviewDestinationTownId: "holloway");

    public static BoringScenario NoHorseFootTravelReady()
        => new(
            ScenarioName: "NoHorseFootTravelReady",
            Fixture: ScenarioSeedCatalog.NoHorseLightEasy,
            PreviewDestinationTownId: "redmesa");

    public static BoringScenario HighRiskFoeInterruptRoute()
        => new(
            ScenarioName: "HighRiskFoeInterruptRoute",
            Fixture: ScenarioSeedCatalog.HighRiskFoeInterruptRoute);

    public static BoringScenario PinecrossServicesOrWantedPosterReady()
        => new(
            ScenarioName: "PinecrossServicesOrWantedPosterReady",
            Fixture: ScenarioSeedCatalog.CanonicalPinecrossServices,
            PreviewDestinationTownId: "holloway");
}

internal sealed record BoringScenario(
    string ScenarioName,
    ScenarioSeedFixture Fixture,
    string? PreviewDestinationTownId = null)
{
    public string SeedCode => Fixture.SeedCode;

    public GameDifficulty GameDifficulty => Fixture.GameDifficulty;

    public void AssertReady()
        => Fixture.AssertCachedFixtureContract();

    public StartGameRequest CreateRequest(string playerName)
        => Fixture.CreateRequest(playerName);

    public GameSession CreateSession(string playerName = "Fixture Validator")
    {
        Fixture.AssertCachedFixtureContract();

        return new SeededNewGameFactory(new DeterministicSaltSourceFactory())
            .Create(playerName, GameDifficulty, SeedCode);
    }

    public GameSessionDto CreateSessionDto(string playerName = "Fixture Validator")
        => GameSessionMapper.ToDto(CreateSession(playerName));

    public TravelPreviewResultDto CreateTravelPreview(GameSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (PreviewDestinationTownId is null)
        {
            throw new InvalidOperationException($"Scenario '{ScenarioName}' does not define a preview destination town.");
        }

        var previewResult = new TravelResolver().PreviewJourney(
            session.World,
            session.Player.CurrentTownId,
            new TownId(PreviewDestinationTownId),
            session.Player.Inventory,
            session.TravelRules);

        var preview = new TravelPreviewResultDto(
            previewResult.Success,
            previewResult.Message,
            previewResult.Preview is null ? null : TravelMapper.ToDto(previewResult.Preview, session.TravelRules));

        Fixture.AssertTravelPreview(GameSessionMapper.ToDto(session), PreviewDestinationTownId, preview);
        return preview;
    }
}
