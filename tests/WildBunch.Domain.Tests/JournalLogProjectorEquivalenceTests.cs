using WildBunch.Application.Projections;
using WildBunch.Domain.Economy;
using WildBunch.Domain.Events;
using WildBunch.Domain.Game;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;

namespace WildBunch.Domain.Tests;

/// <summary>
/// Proves the JournalLogProjector (Application.Projections) reproduces the exact
/// GameLogEntry sequence that the command path's session.LogEntries produces for a
/// full journey cycle and an encounter resolution. Uses TravelTestFactory for
/// deterministic scenario setup. See ADR-0028 and BUNCH-84.
/// </summary>
public sealed class JournalLogProjectorEquivalenceTests
{
    [Fact]
    public void FullJourneyCycle_ProjectedLogMatchesCommandPathLogEntriesExactly()
    {
        var (session, preview, gameStarted) = TravelTestFactory.CreateSixDayQuietJourneyWithGameStarted();
        session.StartJourney(preview);
        TravelJourneyStepResult result;
        do
        {
            result = session.AdvanceJourneyDay();
        } while (result.Status == JourneyStatus.Active && result.Success);
        session.AcknowledgeJourneyArrival();

        var events = new[] { gameStarted }.Concat(session.UncommittedEvents).ToList();

        var projected = new JournalLogProjector().Project(events);

#pragma warning disable CS0618 // LogEntries — this test proves equivalence with the projection
        var commandPathLog = session.LogEntries;
#pragma warning restore CS0618

        Assert.Equal(commandPathLog.Count, projected.Count);
        for (var i = 0; i < commandPathLog.Count; i++)
        {
            Assert.Equal(commandPathLog[i].Kind, projected[i].Kind);
            Assert.Equal(commandPathLog[i].Message, projected[i].Message);
            Assert.Equal(commandPathLog[i].Day, projected[i].Day);
            Assert.Equal(commandPathLog[i].Turn, projected[i].Turn);
        }
    }

    [Fact]
    public void ResolveJourneyEncounter_ProjectedLogMatchesCommandPathLogEntriesExactly()
    {
        var (session, preview) = TravelTestFactory.CreateHighRiskJourney();
        // Capture GameStarted BEFORE any travel commands — RecaptureGameStartedForReplay
        // reads session.Player.CurrentTownId, which changes after arrival.
        var gameStarted = TravelTestFactory.RecaptureGameStartedForReplay(session);
        session.StartJourney(preview);

        TravelJourneyStepResult step;
        do
        {
            step = session.AdvanceJourneyDay();
        } while (step.Status == JourneyStatus.Active && step.Success);
        Assert.Equal(JourneyStatus.Interrupted, step.Status);

        var resolved = session.ResolveJourneyEncounter("run", bulletSpend: null, bribeAmount: null, forcedRoll: 0);
        Assert.True(resolved.Success);
        var events = new[] { gameStarted }.Concat(session.UncommittedEvents).ToList();

        var projected = new JournalLogProjector().Project(events);

#pragma warning disable CS0618 // LogEntries — this test proves equivalence with the projection
        var commandPathLog = session.LogEntries;
#pragma warning restore CS0618

        Assert.Equal(commandPathLog.Count, projected.Count);
        for (var i = 0; i < commandPathLog.Count; i++)
        {
            Assert.Equal(commandPathLog[i].Kind, projected[i].Kind);
            Assert.Equal(commandPathLog[i].Message, projected[i].Message);
            Assert.Equal(commandPathLog[i].Day, projected[i].Day);
            Assert.Equal(commandPathLog[i].Turn, projected[i].Turn);
        }
    }

    [Fact]
    public void Purchase_ProjectedLogMatchesCommandPathLogEntriesExactly()
    {
        var (session, preview, gameStarted) = TravelTestFactory.CreateSixDayQuietJourneyWithGameStarted();

        var resolver = new TownStoreCatalogResolver();
        var town = session.World.GetTown(session.Player.CurrentTownId);
        var offer = resolver.Resolve(town)
            .Offers.Single(o => o.VendorType == StoreVendorType.GeneralStore && o.ItemKind == ItemKind.Food);

        session.Purchase(offer, 2);

        var events = new[] { gameStarted }.Concat(session.UncommittedEvents).ToList();
        var projected = new JournalLogProjector().Project(events);

#pragma warning disable CS0618 // LogEntries — this test proves equivalence with the projection
        var commandPathLog = session.LogEntries;
#pragma warning restore CS0618

        Assert.Equal(commandPathLog.Count, projected.Count);
        for (var i = 0; i < commandPathLog.Count; i++)
        {
            Assert.Equal(commandPathLog[i].Kind, projected[i].Kind);
            Assert.Equal(commandPathLog[i].Message, projected[i].Message);
            Assert.Equal(commandPathLog[i].Day, projected[i].Day);
            Assert.Equal(commandPathLog[i].Turn, projected[i].Turn);
        }
    }
}
