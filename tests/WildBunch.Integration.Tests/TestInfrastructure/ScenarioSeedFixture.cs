using WildBunch.Application.Games.Mapping;
using WildBunch.Application.Games.Models;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using WildBunch.GameContent.NewGame;
using Xunit.Sdk;

namespace WildBunch.Integration.Tests.TestInfrastructure;

internal sealed record ScenarioSeedFixture(
    string Name,
    string SeedCode,
    GameDifficulty GameDifficulty,
    GameEntropy GameEntropy,
    ScenarioSeedDescriptor Contract,
    Func<GameSessionDto, TravelPreviewResultDto?, string> DescribeShapeSignature,
    Action<GameSessionDto> AssertCreatedSessionContract,
    Action<GameSessionDto, string, TravelPreviewResultDto>? AssertTravelPreviewContract = null,
    Action<GameSessionDto, string, TravelPreviewResultDto, GameTurnResultDto>? AssertTravelTurnContract = null)
{
    public void AssertCachedFixtureContract()
    {
        if (!string.Equals(Contract.CodecVersion.Value, SeedWorldResolver.ResolverContractVersion, StringComparison.Ordinal))
        {
            ThrowDrift($"Resolver contract version changed from '{Contract.CodecVersion.Value}' to '{SeedWorldResolver.ResolverContractVersion}'.");
        }

        var session = CreateSession();
        var sessionDto = GameSessionMapper.ToDto(session);

        // Discover preview destination dynamically (first connected town from starting town)
        TravelPreviewResultDto? preview = null;
        TownId? previewDestination = null;
        if (AssertTravelPreviewContract is not null)
        {
            previewDestination = DiscoverFirstConnectedTown(session);
            preview = CreatePreview(session, previewDestination.Value.Value);
        }

        var actualShapeSignature = DescribeShapeSignature(sessionDto, preview);
        var requiredShapeSignature = Contract.FormatRequiredShapeSignature();

        if (!string.Equals(actualShapeSignature, requiredShapeSignature, StringComparison.Ordinal))
        {
            ThrowDrift($"Observed required-shape signature '{actualShapeSignature}'.");
        }

        try
        {
            AssertCreatedSessionContract(sessionDto);
        }
        catch (Exception ex)
        {
            ThrowDrift($"The start-session contract failed: {ex.Message}");
        }

        if (AssertTravelPreviewContract is null)
        {
            return;
        }

        if (preview is null)
        {
            ThrowDrift("Expected a travel preview but none was produced.");
        }

        try
        {
            AssertTravelPreviewContract(sessionDto, previewDestination!.Value.Value, preview!);
        }
        catch (Exception ex)
        {
            ThrowDrift($"The travel preview contract failed: {ex.Message}");
        }
    }

    private TownId DiscoverFirstConnectedTown(GameSession session)
    {
        var connectedTownId = session.World.Trails
            .Where(t => t.FromTownId == session.Player.CurrentTownId || t.ToTownId == session.Player.CurrentTownId)
            .Select(t => t.FromTownId == session.Player.CurrentTownId ? t.ToTownId : t.FromTownId)
            .FirstOrDefault();

        if (connectedTownId.Value is null)
        {
            throw new XunitException($"Fixture '{Name}': no connected town found from starting town '{session.Player.CurrentTownId}' for travel preview.");
        }

        return connectedTownId;
    }

    private GameSession CreateSession()
    {
        return CanonicalStartFlow.StartGame(
            new SeededNewGameFactory(new DeterministicSaltSourceFactory()),
            "Fixture Validator",
            GameDifficulty,
            SeedCode,
            GameEntropy);
    }

    private static TravelPreviewResultDto CreatePreview(GameSession session, string destinationTownId)
    {
        var previewResult = new TravelResolver().PreviewJourney(
            session.World,
            session.Player.CurrentTownId,
            new TownId(destinationTownId),
            session.Player.Inventory,
            session.TravelRules);

        return new TravelPreviewResultDto(
            previewResult.Success,
            previewResult.Message,
            previewResult.Preview is null ? null : TravelMapper.ToDto(previewResult.Preview, session.TravelRules));
    }

    private void ThrowDrift(string detail)
        => throw new XunitException(
            $"Cached scenario seed '{Name}' no longer satisfies required shape '{Contract.FormatRequiredShapeSignature()}'. {detail} Regenerate this fixture through the boring scenario path or intentionally update the fixture contract if the scenario requirement changed.");
}
