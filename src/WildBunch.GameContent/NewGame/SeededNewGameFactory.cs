using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;
using WildBunch.Domain.Economy;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using WildBunch.GameContent.Abstractions;

namespace WildBunch.GameContent.NewGame;

public sealed class SeededNewGameFactory : INewGameFactory
{
    private readonly GameSetupPackageBuilder _setupPackageBuilder = new();
    private readonly ITravelRandomnessSource _travelRandomnessSource;

    public SeededNewGameFactory()
        : this(new RuntimeTravelRandomnessSource())
    {
    }

    public SeededNewGameFactory(ITravelRandomnessSource travelRandomnessSource)
    {
        _travelRandomnessSource = travelRandomnessSource;
    }

    public GameSession Create(string playerName, TravelDifficulty travelDifficulty = TravelDifficulty.Normal, string? setupSeedCode = null)
    {
        var descriptor = ResolveDescriptor(travelDifficulty, setupSeedCode);
        var setupPackage = _setupPackageBuilder.Build(descriptor);
        var travelRandomnessState = descriptor.AdventureRandomnessPolicy == AdventureRandomnessPolicy.Boring
            ? TravelRandomnessState.CreateDeterministic(descriptor.SeedCodeText)
            : _travelRandomnessSource.Create(descriptor.SeedCodeText, setupPackage.TravelDifficulty);

        return GameSession.StartNew(
            playerName,
            setupPackage.World,
            setupPackage.CaseFile,
            setupPackage.StartingTownId,
            setupPackage.StartingWallet,
            setupPackage.StartingInventory,
            setupPackage.TravelDifficulty,
            travelRandomnessState);
    }

    private static StartingWorldDescriptor ResolveDescriptor(TravelDifficulty travelDifficulty, string? setupSeedCode)
    {
        return StartingWorldDescriptorResolver.Resolve(setupSeedCode, travelDifficulty);
    }
}
