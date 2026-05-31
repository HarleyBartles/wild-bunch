using WildBunch.GameContent.NewGame;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;

namespace WildBunch.GameContent.Tests;

public sealed class SeededNewGameFactoryTests
{
    [Fact]
    public void CreatesRicherSeedWorldAndCase()
    {
        var factory = new SeededNewGameFactory();

        var session = factory.Create("Ranger Vale");

        Assert.Equal("Ranger Vale", session.Player.Name);
        Assert.Equal(new WildBunch.Domain.World.TownId("pinecross"), session.Player.CurrentTownId);
        Assert.Equal(WildBunch.Domain.Travel.TravelDifficulty.Normal, session.TravelDifficulty);
        Assert.Equal(25m, session.Player.Wallet.Cash);
        Assert.Equal(8, session.Player.Inventory.Items.Count);
        Assert.Equal(HorseTravelState.Healthy, session.Player.Inventory.GetHorseState());
        var capabilities = new InventoryCapabilityResolver().Resolve(session.Player.Inventory);
        Assert.True(capabilities.MountedTravelAvailable);
        Assert.True(capabilities.HorseUpkeepRequired);
        Assert.True(capabilities.NormalRouteWaterSecure);
        Assert.True(capabilities.TrailUtility);
        Assert.True(capabilities.CloseThreatAvailable);
        Assert.True(capabilities.FirearmThreatAvailable);
        Assert.True(capabilities.GunfightCapable);
        Assert.True(capabilities.RevolverUsable);
        Assert.False(capabilities.RifleUsable);
        Assert.Equal(6, session.World.Towns.Count);
        Assert.Equal(7, session.World.Trails.Count);
        Assert.Contains(session.World.Trails, trail => trail.Connects(new WildBunch.Domain.World.TownId("pinecross"), new WildBunch.Domain.World.TownId("redmesa")));
        Assert.DoesNotContain(session.World.Trails, trail => trail.Connects(new WildBunch.Domain.World.TownId("pinecross"), new WildBunch.Domain.World.TownId("dryfork")));
        Assert.Equal(4, session.CaseFile.Suspects.Count);
        Assert.Single(session.CaseFile.Suspects, suspect => suspect.Id.Equals(session.CaseFile.TrueCulpritId));
        Assert.Equal("A pale scar cuts across the left cheek.", session.CaseFile.OpeningLead.Description);
        Assert.False(session.CaseFile.KillerReleaseState.IsReleased);
        Assert.Equal(0, session.CaseFile.KillerReleaseState.Progress);
        Assert.Equal(2, session.CaseFile.KillerReleaseState.RequiredPublicClues);
        Assert.Equal(2, session.CaseFile.PublicClues.Count);
        Assert.Equal(new[] { new SuspectId("suspect-1") }, session.CaseFile.PublicClues[0].LinkedSuspectIds);
        Assert.Equal(new[] { new SuspectId("suspect-2") }, session.CaseFile.PublicClues[1].LinkedSuspectIds);
        Assert.Equal(3, session.CaseFile.KnownClues.Count);
        Assert.Equal(new SuspectId("suspect-2"), session.CaseFile.Accusation);
        Assert.Contains(session.CaseFile.Suspects, suspect => suspect.Profile.Aliases.Count > 0 && suspect.Profile.IdentifyingFacts.Count > 0);
        Assert.Single(session.CaseFile.Suspects, suspect => suspect.Profile.IdentifyingFacts.Any(fact => fact.Description == session.CaseFile.OpeningLead.Description));
    }

    [Fact]
    public void RandomizedNoHorseLightLoadoutSeedCreatesNoHorseOrSaddle()
    {
        var options = new GameSetupOptionsV1(false, StartingLoadoutProfile.Light);
        var seed = GameSetupSeedCodec.GenerateRandom(options, TravelDifficulty.Easy);
        var seedCode = GameSetupSeedCodec.Encode(seed);
        var factory = new SeededNewGameFactory();

        var session = factory.Create("Ranger Vale", TravelDifficulty.Normal, seedCode);

        Assert.Equal(TravelDifficulty.Easy, session.TravelDifficulty);
        Assert.Null(session.Player.Inventory.GetHorseState());
        Assert.DoesNotContain(session.Player.Inventory.Items, item => item.Kind == ItemKind.Horse);
        Assert.DoesNotContain(session.Player.Inventory.Items, item => item.Kind == ItemKind.Saddle);
        Assert.Equal(3, session.Player.Inventory.GetQuantity(ItemKind.Food));
        Assert.Equal(2, session.Player.Inventory.GetQuantity(ItemKind.HorseFeed));
        Assert.Equal(4, session.Player.Inventory.GetQuantity(ItemKind.RevolverAmmo));
    }

    [Fact]
    public void DefaultJourneyRandomnessStaysRuntimeSaltedAndDeterministicSetupOptionCanOptOut()
    {
        var factory = new SeededNewGameFactory();

        var runtimeFirst = factory.Create("Ranger Vale");
        var runtimeSecond = factory.Create("Ranger Vale");

        Assert.Equal(TravelRandomnessMode.RuntimeSalted, runtimeFirst.TravelRandomness.Mode);
        Assert.Equal(TravelRandomnessMode.RuntimeSalted, runtimeSecond.TravelRandomness.Mode);
        Assert.NotEqual(runtimeFirst.TravelRandomness.Salt, runtimeSecond.TravelRandomness.Salt);

        var deterministicSeed = GameSetupSeedCodec.Encode(
            GameSetupSeedCodec.WithOption(
                GameSetupSeedCodec.WithDifficulty(GameSetupSeedCodec.CreateCanonicalSeed(), TravelDifficulty.Normal),
                GameSetupOption.JourneyRandomness,
                1));

        var deterministicFirst = factory.Create("Ranger Vale", setupSeedCode: deterministicSeed);
        var deterministicSecond = factory.Create("Ranger Vale", setupSeedCode: deterministicSeed);

        Assert.Equal(TravelRandomnessMode.Deterministic, deterministicFirst.TravelRandomness.Mode);
        Assert.Equal(TravelRandomnessMode.Deterministic, deterministicSecond.TravelRandomness.Mode);
        Assert.Equal(deterministicFirst.TravelRandomness.Salt, deterministicSecond.TravelRandomness.Salt);
    }
}
