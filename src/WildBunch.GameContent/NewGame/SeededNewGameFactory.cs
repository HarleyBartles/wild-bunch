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
        var setupSeed = ResolveSeed(travelDifficulty, setupSeedCode);
        var setupPackage = _setupPackageBuilder.Build(setupSeed);
        var travelRandomnessState = setupSeed.Options.JourneyRandomnessMode == TravelRandomnessMode.Deterministic
            ? TravelRandomnessState.CreateDeterministic(string.Empty)
            : _travelRandomnessSource.Create(setupSeedCode ?? GameSetupSeedCodec.Encode(setupSeed), setupPackage.TravelDifficulty);

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

    private static GameSetupSeed ResolveSeed(TravelDifficulty travelDifficulty, string? setupSeedCode)
    {
        if (string.IsNullOrWhiteSpace(setupSeedCode))
        {
            return GameSetupSeedCodec.CreateCanonicalSeed(travelDifficulty);
        }

        var decodeResult = GameSetupSeedCodec.Decode(setupSeedCode);
        if (!decodeResult.Success || decodeResult.Seed is null)
        {
            throw new ArgumentException(decodeResult.ErrorMessage ?? "Seed code is invalid.", nameof(setupSeedCode));
        }

        return decodeResult.Seed;
    }
}
