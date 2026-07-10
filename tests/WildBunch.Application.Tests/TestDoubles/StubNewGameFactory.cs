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

    public List<LayoutSalts?> RequestedDevLayoutSalts { get; } = [];

    public GameSession CreatedSession => _sessionToReturn;

    public (World World, CaseFile CaseFile, string SeedCodeText, SaltSource SaltSource) ResolveWorld(
        string playerName,
        GameDifficulty gameDifficulty,
        string? setupSeedCode,
        GameEntropy gameEntropy)
    {
        RequestedPlayerNames.Add(playerName);
        RequestedGameDifficulties.Add(gameDifficulty);
        RequestedSetupSeedCodes.Add(setupSeedCode);
        RequestedEntropies.Add(gameEntropy);
        return (_sessionToReturn.World, _sessionToReturn.CaseFile, _sessionToReturn.SeedCode ?? "00000000-0000-0000-0000-000000000000", _sessionToReturn.SaltSource);
    }

    public (World World, CaseFile CaseFile, string SeedCodeText, SaltSource SaltSource) ResolveWorld(
        string playerName,
        GameDifficulty gameDifficulty,
        string? setupSeedCode,
        GameEntropy gameEntropy,
        LayoutSalts? devLayoutSalts)
    {
        RequestedPlayerNames.Add(playerName);
        RequestedGameDifficulties.Add(gameDifficulty);
        RequestedSetupSeedCodes.Add(setupSeedCode);
        RequestedEntropies.Add(gameEntropy);
        RequestedDevLayoutSalts.Add(devLayoutSalts);
        return (_sessionToReturn.World, _sessionToReturn.CaseFile, _sessionToReturn.SeedCode ?? "00000000-0000-0000-0000-000000000000", _sessionToReturn.SaltSource);
    }

    public (Wallet Wallet, Inventory Inventory) ResolveStartingResources(GameDifficulty gameDifficulty)
    {
        return (_sessionToReturn.Player.Wallet, _sessionToReturn.Player.Inventory);
    }

    private static GameSession CreateSession()
    {
        var dustvale = new Town(new TownId("dustvale"), "Dustvale", TownServices.None);
        var silvercreek = new Town(new TownId("silvercreek"), "Silver Creek", TownServices.None);
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
                    new[] { new SuspectIdentityFact(FeatureLanguage.Raw("Wears a brass buckle with a cracked star engraving.", "a brass buckle with a cracked star engraving", "wears a brass buckle with a cracked star engraving")) }),
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

        return StartGameCanonical(
            "Ranger Vale",
            world,
            caseFile,
            dustvale.Id,
            Wallet.Starting(25m),
            inventory,
            saltSource: SaltSource.CreateFixed("application-tests"));
    }

    private static GameSession StartGameCanonical(
        string playerName,
        World world,
        CaseFile caseFile,
        TownId startingTownId,
        Wallet? wallet = null,
        Inventory? inventory = null,
        GameDifficulty gameDifficulty = GameDifficulty.Standard,
        SaltSource? saltSource = null,
        GameEntropy gameEntropy = GameEntropy.Classic,
        string? seedCode = null)
    {
        var resolvedSaltSource = saltSource ?? SaltSource.CreateFixed("application-tests");
        var resolvedSeedCode = seedCode ?? "stub-seed";

        var session = GameSession.StartSetup(
            playerName,
            world,
            caseFile,
            gameDifficulty,
            gameEntropy,
            resolvedSeedCode,
            resolvedSaltSource);

        session.ViewPrologue("test-prologue-descriptor");
        session.SelectStartingTown(startingTownId);
        session.CompleteGameStart(wallet, inventory);

        return session;
    }
}
