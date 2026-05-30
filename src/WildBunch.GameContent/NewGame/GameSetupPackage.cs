using WildBunch.Domain.Cases;
using WildBunch.Domain.Economy;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;

namespace WildBunch.GameContent.NewGame;

internal sealed record GameSetupPackage(
    GameSetupSeed Seed,
    TravelDifficulty TravelDifficulty,
    TravelRulesProfile TravelRulesProfile,
    World World,
    TownId StartingTownId,
    Wallet StartingWallet,
    Inventory StartingInventory,
    CaseFile CaseFile);
