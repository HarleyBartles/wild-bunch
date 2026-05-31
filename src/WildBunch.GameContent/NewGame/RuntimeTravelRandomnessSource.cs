using WildBunch.Domain.Travel;
using WildBunch.GameContent.Abstractions;

namespace WildBunch.GameContent.NewGame;

public sealed class RuntimeTravelRandomnessSource : ITravelRandomnessSource
{
    public TravelRandomnessState Create(string? setupSeedCode, TravelDifficulty travelDifficulty)
        => TravelRandomnessState.CreateRuntimeSalted();
}
