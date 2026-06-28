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

    public GameSession Create(
        string playerName,
        TravelDifficulty travelDifficulty = TravelDifficulty.Normal,
        string? setupSeedCode = null,
        AdventureRandomnessPolicy entropy = AdventureRandomnessPolicy.Standard,
        string? startingTownId = null)
    {
        var descriptor = ResolveDescriptor(travelDifficulty, setupSeedCode, entropy);
        var setupPackage = _setupPackageBuilder.Build(descriptor);
        var travelRandomnessState = descriptor.AdventureRandomnessPolicy == AdventureRandomnessPolicy.Boring
            ? TravelRandomnessState.CreateDeterministic(descriptor.SeedCodeText)
            : _travelRandomnessSource.Create(descriptor.SeedCodeText, setupPackage.TravelDifficulty);

        // Player-chosen town overrides the seed-derived default; null falls back to the seed default.
        var resolvedStartingTownId = startingTownId is null
            ? setupPackage.StartingTownId
            : new TownId(startingTownId);

        return GameSession.StartNew(
            playerName,
            setupPackage.World,
            setupPackage.CaseFile,
            resolvedStartingTownId,
            setupPackage.StartingWallet,
            setupPackage.StartingInventory,
            setupPackage.TravelDifficulty,
            travelRandomnessState,
            descriptor.AdventureRandomnessPolicy);
    }

    private static StartingWorldDescriptor ResolveDescriptor(TravelDifficulty travelDifficulty, string? setupSeedCode, AdventureRandomnessPolicy entropy)
    {
        return StartingWorldDescriptorResolver.Resolve(setupSeedCode, travelDifficulty, entropy);
    }
}
