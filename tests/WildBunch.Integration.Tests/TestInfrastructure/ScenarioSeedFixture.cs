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
    string? PreviewDestinationTownId = null,
    Action<GameSessionDto, string, TravelPreviewResultDto>? AssertTravelPreviewContract = null,
    Action<GameSessionDto, string, TravelPreviewResultDto, GameTurnResultDto>? AssertTravelTurnContract = null)
{
    /// <summary>
    /// The default starting town for this fixture (slot-0 town of the seed-derived world).
    /// Used by the three-step setup helper when no explicit starting town is supplied.
    /// </summary>
    public string DefaultStartingTownId => Contract.ExactStartingTownId?.Value ?? "hardpan";
    public void AssertCachedFixtureContract()
    {
        if (!string.Equals(Contract.CodecVersion.Value, SeedWorldResolver.ResolverContractVersion, StringComparison.Ordinal))
        {
            ThrowDrift($"Resolver contract version changed from '{Contract.CodecVersion.Value}' to '{SeedWorldResolver.ResolverContractVersion}'.");
        }

        var session = CreateSession();
        var sessionDto = GameSessionMapper.ToDto(session);
        var preview = PreviewDestinationTownId is null ? null : CreatePreview(session, PreviewDestinationTownId);
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

        // If PreviewDestinationTownId is null, we skip the preview validation
        // This is used for geometry-first trail generation where the destination is selected dynamically
        if (PreviewDestinationTownId is null)
        {
            return;
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
            $"Cached scenario seed '{Name}' no longer satisfies required shape '{Contract.FormatRequiredShapeSignature()}'. {detail} Regenerate this fixture through the boring scenario path or intentionally update the fixture contract if the scenario requirement changed.");
}
