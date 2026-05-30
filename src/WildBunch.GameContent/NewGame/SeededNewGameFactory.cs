using WildBunch.Application.Abstractions;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;
using WildBunch.Domain.Economy;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;

namespace WildBunch.GameContent.NewGame;

public sealed class SeededNewGameFactory : INewGameFactory
{
    private readonly GameSetupPackageBuilder _setupPackageBuilder = new();

    public GameSession Create(string playerName, TravelDifficulty travelDifficulty = TravelDifficulty.Normal, string? setupSeedCode = null)
    {
        var setupSeed = ResolveSeed(travelDifficulty, setupSeedCode);
        var setupPackage = _setupPackageBuilder.Build(setupSeed);

        return GameSession.StartNew(
            playerName,
            setupPackage.World,
            setupPackage.CaseFile,
            setupPackage.StartingTownId,
            setupPackage.StartingWallet,
            setupPackage.StartingInventory,
            setupPackage.TravelDifficulty);
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
