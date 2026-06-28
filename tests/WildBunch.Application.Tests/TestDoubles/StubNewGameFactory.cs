using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;
using WildBunch.Domain.Economy;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using WildBunch.GameContent.Abstractions;

namespace WildBunch.Application.Tests.TestDoubles;

public sealed class StubNewGameFactory : INewGameFactory
{
    private readonly GameSession _sessionToReturn;

    public StubNewGameFactory(GameSession? sessionToReturn = null)
    {
        _sessionToReturn = sessionToReturn ?? CreateSession();
    }

    public List<string> RequestedPlayerNames { get; } = [];

    public List<GameDifficulty> RequestedGameDifficulties { get; } = [];

    public List<string?> RequestedSetupSeedCodes { get; } = [];

    public List<GameEntropy> RequestedEntropies { get; } = [];

    public List<string?> RequestedStartingTownIds { get; } = [];

    public GameSession Create(
        string playerName,
        GameDifficulty GameDifficulty = GameDifficulty.Standard,
        string? setupSeedCode = null,
        GameEntropy entropy = GameEntropy.Classic,
        string? startingTownId = null)
    {
        RequestedPlayerNames.Add(playerName);
        RequestedGameDifficulties.Add(GameDifficulty);
        RequestedSetupSeedCodes.Add(setupSeedCode);
        RequestedEntropies.Add(entropy);
        RequestedStartingTownIds.Add(startingTownId);
        return _sessionToReturn;
    }

    public GameSession CreatedSession => _sessionToReturn;

    private static GameSession CreateSession()
    {
        var dustvale = new Town(new TownId("dustvale"), "Dustvale", TownServices.Supplies | TownServices.Lodging);
        var silvercreek = new Town(new TownId("silvercreek"), "Silver Creek", TownServices.Supplies);
        var dryridge = new Town(new TownId("dryridge"), "Dry Ridge", TownServices.None);

        var world = new World(
            new[] { dustvale, silvercreek, dryridge },
            new[]
            {
                new Trail(new TrailId("trail-1"), dustvale.Id, silvercreek.Id, TrailRisk.Low)
            });

        var suspects = new[]
        {
            new Suspect(
                new SuspectId("suspect-1"),
                "Ira Flint",
                new SuspectProfile(
                    new[] { new SuspectAlias("Dust Runner", AliasKind.Nickname) },
                    new[] { new SuspectIdentityFact("Wears a brass buckle with a cracked star engraving.") }),
                SuspectTraits.FromTags(SuspectTraitTags.Local, SuspectTraitTags.Desperate),
                SuspectStatus.AtLarge)
        };

        var caseFile = new CaseFile(
            null,
            suspects,
            new SuspectId("suspect-1"),
            CaseOpeningLead.Create("A brass buckle bears a cracked star engraving."),
            Array.Empty<Clue>());

        var inventory = new Inventory(new[]
        {
            new InventoryItem(ItemKind.Food, 3),
            new InventoryItem(ItemKind.HorseFeed, 2),
            new InventoryItem(ItemKind.Canteen, 1),
            new InventoryItem(ItemKind.Horse, 1, HorseTravelState.Healthy),
            new InventoryItem(ItemKind.Saddle, 1),
            new InventoryItem(ItemKind.Knife, 1),
            new InventoryItem(ItemKind.Revolver, 1),
            new InventoryItem(ItemKind.RevolverAmmo, 4)
        });

        return GameSession.StartNew(
            "Ranger Vale",
            world,
            caseFile,
            dustvale.Id,
            Wallet.Starting(25m),
            inventory,
            saltSource: SaltSource.CreateFixed("application-tests"));
    }
}
