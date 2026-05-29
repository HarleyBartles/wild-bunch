using WildBunch.Application.Abstractions;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;
using WildBunch.Domain.Economy;
using WildBunch.Domain.World;

namespace WildBunch.GameContent.NewGame;

public sealed class SeededNewGameFactory : INewGameFactory
{
    public GameSession Create(string playerName)
    {
        var world = SeedWorldBuilder.CreateWorld();
        var caseFile = SeedCaseBuilder.CreateCaseFile();
        var inventory = SeedInventoryBuilder.CreateStartingLoadout();

        return GameSession.StartNew(
            playerName,
            world,
            caseFile,
            new TownId("pinecross"),
            Wallet.Starting(25m),
            inventory);
    }
}
