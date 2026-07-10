using WildBunch.Domain.Game;
using WildBunch.Domain.World;
using WildBunch.GameContent.NewGame;
using Xunit;

namespace WildBunch.GameContent.Tests.NewGame;

/// <summary>
/// Tests proving that layout salts are concern-scoped and do not leak into
/// unrelated game setup decisions like case file choices or mystery truth.
/// </summary>
public sealed class LayoutSaltsConcernBoundaryTests
{
    [Fact]
    public void GameSetupDeterministicSource_LayoutSalts_DoNotAffectNonLayoutRolls()
    {
        // Proves that GameSetupDeterministicSource.Roll no longer includes layout salts
        // in its hash, ensuring layout salt changes don't affect non-layout decisions.
        var seedCode = "test-seed";
        
        var sourceWithoutSalts = new GameSetupDeterministicSource(seedCode);
        var sourceWithSalts = new GameSetupDeterministicSource(
            seedCode,
            new LayoutSalts("buildings", "roads", "dirt", "props"));
        
        // Same label should produce same roll regardless of layout salts
        var roll1 = sourceWithoutSalts.Roll("case-file-choice");
        var roll2 = sourceWithSalts.Roll("case-file-choice");
        
        Assert.Equal(roll1, roll2);
    }
    
    [Fact]
    public void LayoutDeterministicSource_UsesConcernSpecificSalts()
    {
        // Proves that LayoutDeterministicSource uses the correct concern-specific salt
        var seedCode = "test-seed";
        var townId = new TownId("town-1");
        var salts = new LayoutSalts("buildings-salt", "roads-salt", "dirt-salt", "props-salt");
        
        var layoutSource = new LayoutDeterministicSource(seedCode, townId, 0, "1.0.0", salts);
        
        // Different concerns should use different salts
        var buildingsRoll = layoutSource.Roll("test", LayoutConcern.Buildings);
        var roadsRoll = layoutSource.Roll("test", LayoutConcern.Roads);
        var dirtRoll = layoutSource.Roll("test", LayoutConcern.Dirt);
        var propsRoll = layoutSource.Roll("test", LayoutConcern.Props);
        
        // All should be different (statistically unlikely to collide with different salts)
        Assert.NotEqual(buildingsRoll, roadsRoll);
        Assert.NotEqual(buildingsRoll, dirtRoll);
        Assert.NotEqual(buildingsRoll, propsRoll);
        Assert.NotEqual(roadsRoll, dirtRoll);
        Assert.NotEqual(roadsRoll, propsRoll);
        Assert.NotEqual(dirtRoll, propsRoll);
    }
    
    [Fact]
    public void LayoutDeterministicSource_ChangingBuildingsSalt_ChangesBuildingRolls()
    {
        // Proves that changing a concern-specific salt affects only that concern's rolls
        var seedCode = "test-seed";
        var townId = new TownId("town-1");
        
        var salts1 = new LayoutSalts("buildings-1", "roads", "dirt", "props");
        var salts2 = new LayoutSalts("buildings-2", "roads", "dirt", "props");
        
        var source1 = new LayoutDeterministicSource(seedCode, townId, 0, "1.0.0", salts1);
        var source2 = new LayoutDeterministicSource(seedCode, townId, 0, "1.0.0", salts2);
        
        // Buildings rolls should differ
        var buildingsRoll1 = source1.Roll("test", LayoutConcern.Buildings);
        var buildingsRoll2 = source2.Roll("test", LayoutConcern.Buildings);
        Assert.NotEqual(buildingsRoll1, buildingsRoll2);
        
        // Other concern rolls should remain the same
        var roadsRoll1 = source1.Roll("test", LayoutConcern.Roads);
        var roadsRoll2 = source2.Roll("test", LayoutConcern.Roads);
        Assert.Equal(roadsRoll1, roadsRoll2);
        
        var dirtRoll1 = source1.Roll("test", LayoutConcern.Dirt);
        var dirtRoll2 = source2.Roll("test", LayoutConcern.Dirt);
        Assert.Equal(dirtRoll1, dirtRoll2);
        
        var propsRoll1 = source1.Roll("test", LayoutConcern.Props);
        var propsRoll2 = source2.Roll("test", LayoutConcern.Props);
        Assert.Equal(propsRoll1, propsRoll2);
    }
}
