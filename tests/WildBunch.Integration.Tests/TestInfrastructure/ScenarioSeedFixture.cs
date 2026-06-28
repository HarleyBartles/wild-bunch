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
    string ResolverContractVersion,
    string RequiredShapeSignature,
    Func<GameSessionDto, TravelPreviewResultDto?, string> DescribeShapeSignature,
    Action<GameSessionDto> AssertCreatedSessionContract,
    string? PreviewDestinationTownId = null,
    Action<GameSessionDto, string, TravelPreviewResultDto>? AssertTravelPreviewContract = null,
    Action<GameSessionDto, string, TravelPreviewResultDto, GameTurnResultDto>? AssertTravelTurnContract = null)
{
    public void AssertCachedFixtureContract()
    {
        if (!string.Equals(ResolverContractVersion, SeedWorldResolver.ResolverContractVersion, StringComparison.Ordinal))
        {
            ThrowDrift($"Resolver contract version changed from '{ResolverContractVersion}' to '{SeedWorldResolver.ResolverContractVersion}'.");
        }

        var session = CreateSession();
        var sessionDto = GameSessionMapper.ToDto(session);
        var preview = PreviewDestinationTownId is null ? null : CreatePreview(session, PreviewDestinationTownId);
        var actualShapeSignature = DescribeShapeSignature(sessionDto, preview);

        if (!string.Equals(actualShapeSignature, RequiredShapeSignature, StringComparison.Ordinal))
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

        if (PreviewDestinationTownId is null)
        {
            ThrowDrift("The fixture declares a travel preview contract but does not define a preview destination town.");
        }

        if (preview is null)
        {
            ThrowDrift($"Expected a travel preview for '{PreviewDestinationTownId}' but none was produced.");
        }

        try
        {
            AssertTravelPreviewContract(sessionDto, PreviewDestinationTownId!, preview!);
        }
        catch (Exception ex)
        {
            ThrowDrift($"The travel preview contract failed: {ex.Message}");
        }
    }

    private GameSession CreateSession()
    {
        return new SeededNewGameFactory(new DeterministicSaltSourceFactory())
            .Create("Fixture Validator", GameDifficulty, setupSeedCode: SeedCode, gameEntropy: GameEntropy);
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
            $"Cached scenario seed '{Name}' no longer satisfies required shape '{RequiredShapeSignature}'. {detail} Regenerate this fixture through the boring scenario path or intentionally update the fixture contract if the scenario requirement changed.");
}
