namespace WildBunch.GameContent.Tests;

public sealed class TravelTestSeedCatalogTests
{
    [Fact]
    public void SessionFromCatalogStartsInFirstGeneratedTown()
    {
        var session = TravelTestSeedCatalog.CreateSession(TravelTestSeedCatalog.CanonicalClassic);

        Assert.Equal(session.World.Towns.First().Id, session.Player.CurrentTownId);
    }
}
