using WildBunch.Application.Abstractions;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;
using WildBunch.Domain.World;

namespace WildBunch.Application.Tests.TestDoubles;

public sealed class StubNewGameFactory : INewGameFactory
{
    private readonly GameSession _sessionToReturn;

    public StubNewGameFactory(GameSession? sessionToReturn = null)
    {
        _sessionToReturn = sessionToReturn ?? CreateSession();
    }

    public List<string> RequestedPlayerNames { get; } = [];

    public GameSession Create(string playerName)
    {
        RequestedPlayerNames.Add(playerName);
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
                new Trail(new TrailId("trail-1"), dustvale.Id, silvercreek.Id, SupplyCost: 2, TrailRisk.Low)
            });

        var suspects = new[]
        {
            new Suspect(new SuspectId("suspect-1"), "Ira Flint", new SuspectTraits(IsLocal: true, IsArmed: false, IsDesperate: true), SuspectStatus.AtLarge)
        };

        var caseFile = new CaseFile(null, suspects, new SuspectId("suspect-1"), Array.Empty<Clue>());
        return GameSession.StartNew("Ranger Vale", world, caseFile);
    }
}
