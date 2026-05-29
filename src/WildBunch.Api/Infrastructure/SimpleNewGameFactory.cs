using WildBunch.Application.Abstractions;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;
using WildBunch.Domain.World;

namespace WildBunch.Api.Infrastructure;

public sealed class SimpleNewGameFactory : INewGameFactory
{
    public GameSession Create(string playerName)
    {
        var world = CreateWorld();
        var caseFile = CreateCaseFile();

        return GameSession.StartNew(playerName, world, caseFile);
    }

    private static World CreateWorld()
    {
        var briarGlen = new Town(new TownId("briar-glen"), "Briar Glen", TownServices.Supplies | TownServices.Lodging);
        var cinderFord = new Town(new TownId("cinder-ford"), "Cinder Ford", TownServices.Supplies);
        var ashHollow = new Town(new TownId("ash-hollow"), "Ash Hollow", TownServices.Doctor);

        var trails = new[]
        {
            new Trail(new TrailId("trail-briar-cinder"), briarGlen.Id, cinderFord.Id, SupplyCost: 2, TrailRisk.Low)
        };

        return new World(new[] { briarGlen, cinderFord, ashHollow }, trails);
    }

    private static CaseFile CreateCaseFile()
    {
        var suspect = new Suspect(
            new SuspectId("suspect-1"),
            "Ira Flint",
            new SuspectTraits(IsLocal: true, IsArmed: false, IsDesperate: true),
            SuspectStatus.AtLarge);

        var clue = new Clue(new ClueId("clue-1"), ClueKind.Witness, "A rider was seen near the river road.");

        return new CaseFile(null, new[] { suspect }, suspect.Id, new[] { clue });
    }
}
