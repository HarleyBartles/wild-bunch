using WildBunch.Domain.Travel;
using WildBunch.GameContent.Abstractions;

namespace WildBunch.Integration.Tests.TestInfrastructure;

internal sealed class DeterministicTravelRandomnessSource : ITravelRandomnessSource
{
    public TravelRandomnessState Create(string? setupSeedCode, GameDifficulty GameDifficulty)
        => TravelRandomnessState.CreateDeterministic(string.Empty);
}
