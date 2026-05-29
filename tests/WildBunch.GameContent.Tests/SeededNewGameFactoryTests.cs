using WildBunch.GameContent.NewGame;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;
using WildBunch.Domain.Inventory;

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
        Assert.Equal(25m, session.Player.Wallet.Cash);
        Assert.Equal(8, session.Player.Inventory.Items.Count);
        Assert.Equal(HorseCondition.Healthy, session.Player.Inventory.GetHorseCondition());
        Assert.True(session.Player.Capabilities.MountedTravelAvailable);
        Assert.True(session.Player.Capabilities.HorseUpkeepRequired);
        Assert.True(session.Player.Capabilities.NormalRouteWaterSecure);
        Assert.True(session.Player.Capabilities.TrailUtility);
        Assert.True(session.Player.Capabilities.CloseThreatAvailable);
        Assert.True(session.Player.Capabilities.FirearmThreatAvailable);
        Assert.True(session.Player.Capabilities.GunfightCapable);
        Assert.True(session.Player.Capabilities.RevolverUsable);
        Assert.False(session.Player.Capabilities.RifleUsable);
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
        Assert.Equal(3, session.CaseFile.KnownClues.Count);
        Assert.Equal(new SuspectId("suspect-2"), session.CaseFile.Accusation);
        Assert.Contains(session.CaseFile.Suspects, suspect => suspect.Profile.Aliases.Count > 0 && suspect.Profile.IdentifyingFacts.Count > 0);
        Assert.Single(session.CaseFile.Suspects, suspect => suspect.Profile.IdentifyingFacts.Any(fact => fact.Description == session.CaseFile.OpeningLead.Description));
    }
}
