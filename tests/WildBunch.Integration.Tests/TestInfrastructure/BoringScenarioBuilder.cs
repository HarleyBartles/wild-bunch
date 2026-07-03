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
            PreviewDestinationTownId: null); // Will be set dynamically to a connected town

    public static BoringScenario NoHorseFootTravelReady()
        => new(
            ScenarioName: "NoHorseFootTravelReady",
            Fixture: ScenarioSeedCatalog.NoHorseLightEasy,
            PreviewDestinationTownId: null); // Will be set dynamically to a connected town

    public static BoringScenario HighRiskFoeInterruptRoute()
        => new(
            ScenarioName: "HighRiskFoeInterruptRoute",
            Fixture: ScenarioSeedCatalog.HighRiskFoeInterruptRoute,
            PreviewDestinationTownId: null); // Will be set dynamically to a connected town

    public static BoringScenario PinecrossServicesOrWantedPosterReady()
        => new(
            ScenarioName: "PinecrossServicesOrWantedPosterReady",
            Fixture: ScenarioSeedCatalog.CanonicalPinecrossServices,
            PreviewDestinationTownId: null); // Will be set dynamically to a connected town
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

    public SetupGameRequest CreateRequest(string playerName)
        => Fixture.CreateRequest(playerName);

    public GameSession CreateSession(string playerName = "Fixture Validator")
    {
        Fixture.AssertCachedFixtureContract();

        return new SeededNewGameFactory(new DeterministicSaltSourceFactory())
            .Create(playerName, GameDifficulty, SeedCode, Fixture.GameEntropy);
    }

    public GameSessionDto CreateSessionDto(string playerName = "Fixture Validator")
        => GameSessionMapper.ToDto(CreateSession(playerName));

    public TravelPreviewResultDto CreateTravelPreview(GameSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        TownId destinationTownId;
        if (PreviewDestinationTownId is null)
        {
            // Dynamically select a connected town
            var connectedTownId = session.World.Trails
                .Where(trail => trail.FromTownId == session.Player.CurrentTownId || trail.ToTownId == session.Player.CurrentTownId)
                .Select(trail => trail.FromTownId == session.Player.CurrentTownId ? trail.ToTownId : trail.FromTownId)
                .Distinct()
                .OrderBy(townId => townId.Value)
                .FirstOrDefault();

            if (connectedTownId == default)
            {
                throw new InvalidOperationException($"Scenario '{ScenarioName}' has no connected towns from {session.Player.CurrentTownId}.");
            }

            destinationTownId = connectedTownId;
        }
        else
        {
            destinationTownId = new TownId(PreviewDestinationTownId);
        }

        var previewResult = new TravelResolver().PreviewJourney(
            session.World,
            session.Player.CurrentTownId,
            destinationTownId,
            session.Player.Inventory,
            session.TravelRules);

        var preview = new TravelPreviewResultDto(
            previewResult.Success,
            previewResult.Message,
            previewResult.Preview is null ? null : TravelMapper.ToDto(previewResult.Preview, session.TravelRules));

        Fixture.AssertTravelPreview(GameSessionMapper.ToDto(session), destinationTownId.Value, preview);
        return preview;
    }
}
