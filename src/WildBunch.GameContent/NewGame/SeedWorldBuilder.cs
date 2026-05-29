using WildBunch.Domain.World;

namespace WildBunch.GameContent.NewGame;

internal static class SeedWorldBuilder
{
    public static World CreateWorld()
    {
        var pinecross = new Town(new TownId("pinecross"), "Pinecross", TownServices.Supplies | TownServices.Lodging | TownServices.NoticeBoard);
        var redmesa = new Town(new TownId("redmesa"), "Red Mesa", TownServices.Supplies | TownServices.Telegraph);
        var holloway = new Town(new TownId("holloway"), "Holloway", TownServices.Doctor);
        var sagewell = new Town(new TownId("sagewell"), "Sagewell", TownServices.Supplies | TownServices.Doctor);
        var dryfork = new Town(new TownId("dryfork"), "Dry Fork", TownServices.None);
        var emberfall = new Town(new TownId("emberfall"), "Emberfall", TownServices.Supplies | TownServices.Lodging | TownServices.Telegraph);

        var towns = new[]
        {
            pinecross,
            redmesa,
            holloway,
            sagewell,
            dryfork,
            emberfall
        };

        var trails = new[]
        {
            new Trail(new TrailId("trail-pine-red"), pinecross.Id, redmesa.Id, TrailRisk.Low),
            new Trail(new TrailId("trail-pine-hollow"), pinecross.Id, holloway.Id, TrailRisk.Moderate),
            new Trail(new TrailId("trail-red-sage"), redmesa.Id, sagewell.Id, TrailRisk.Low),
            new Trail(new TrailId("trail-red-dry"), redmesa.Id, dryfork.Id, TrailRisk.High),
            new Trail(new TrailId("trail-hollow-sage"), holloway.Id, sagewell.Id, TrailRisk.Low),
            new Trail(new TrailId("trail-sage-ember"), sagewell.Id, emberfall.Id, TrailRisk.Moderate),
            new Trail(new TrailId("trail-red-ember"), redmesa.Id, emberfall.Id, TrailRisk.High)
        };

        return new World(towns, trails);
    }
}
