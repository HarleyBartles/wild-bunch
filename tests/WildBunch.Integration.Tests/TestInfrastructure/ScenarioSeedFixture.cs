using WildBunch.Application.Games.Models;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;

namespace WildBunch.Integration.Tests.TestInfrastructure;

internal sealed record ScenarioSeedFixture(
    string Name,
    string SeedCode,
    GameDifficulty GameDifficulty,
    GameEntropy GameEntropy,
    Action<GameSessionDto> AssertCreatedSessionContract,
    Action<GameSessionDto, string, TravelPreviewResultDto>? AssertTravelPreviewContract = null,
    Action<GameSessionDto, string, TravelPreviewResultDto, GameTurnResultDto>? AssertTravelTurnContract = null);
