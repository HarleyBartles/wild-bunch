using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using DomainWorld = WildBunch.Domain.World.World;
using Town = WildBunch.Domain.World.Town;
using Trail = WildBunch.Domain.World.Trail;
using TrailId = WildBunch.Domain.World.TrailId;

namespace WildBunch.Domain.Tests;

public sealed class GameSessionBountyLoopCoordinatorTests
{
    [Fact]
    public void CoordinatorRejectsSheriffSettlementBeforeTheSuspectIsSecured()
    {
        var session = CreateArmedWantedSession();
        var suspectId = new SuspectId("suspect-1");

        var repeatTurnIn = session.SettleSheriffTurnIn(suspectId, isAlive: true);

        Assert.False(repeatTurnIn.Success);
        Assert.Equal(SheriffTurnInOutcome.Rejected, repeatTurnIn.Outcome);
        Assert.Contains("not secured", repeatTurnIn.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(session.CaseFile.SheriffTurnInSettlements);
        Assert.Equal(25m, session.Player.Wallet.Cash);
    }

    private static GameSession CreateArmedWantedSession()
    {
        var currentTown = new Town(new TownId("current"), "Current Town", TownServices.None);
        var connectedTown = new Town(new TownId("connected"), "Connected Town", TownServices.None);
        var world = new DomainWorld(
            new[] { currentTown, connectedTown },
            new[] { new Trail(new TrailId("trail-1"), currentTown.Id, connectedTown.Id, TrailRisk.Low) });

        var suspects = new[]
        {
            new Suspect(new SuspectId("suspect-1"), "Mira Cline", SuspectTraits.Empty, SuspectStatus.AtLarge),
            new Suspect(
                new SuspectId("suspect-2"),
                "Reno Pike",
                new SuspectProfile(
                    Array.Empty<SuspectAlias>(),
                    new[] { new SuspectIdentityFact(FeatureLanguage.Raw("a black duster", "a black duster", "wears a black duster")) }),
                SuspectTraits.Empty,
                SuspectStatus.AtLarge)
        };

        var caseFile = new CaseFile(
            accusation: null,
            suspects,
            trueCulpritId: new SuspectId("suspect-2"),
            openingLead: CaseOpeningLead.Create("Follow the public leads and look for a signature mark."),
            knownClues: Array.Empty<Clue>(),
            knownWarrants: new[]
            {
                new Warrant(
                    new WarrantId("warrant-public-1"),
                    "Mira Cline",
                    new WarrantTerms(
                        WarrantDisposition.DeadOrAlive,
                        2500m,
                        new[] { "Red Wren" },
                        new[] { "Raven-feather pin" },
                        "Dodge City Marshal",
                        InvestigationTargetKind.TrueCulprit,
                        Array.Empty<OutlawGangId>(),
                        null),
                    "Wanted for a stage robbery.")
            });

        return TestSessionFactory.StartGameCanonical("Ranger Vale", world, caseFile, currentTown.Id, gameDifficulty: GameDifficulty.Standard);
    }
}
