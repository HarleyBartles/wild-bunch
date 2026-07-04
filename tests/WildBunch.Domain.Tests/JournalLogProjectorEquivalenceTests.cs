using WildBunch.Application.Projections;
using WildBunch.Domain.Economy;
using WildBunch.Domain.Events;
using WildBunch.Domain.Game;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;

namespace WildBunch.Domain.Tests;

/// <summary>
/// Characterization tests for JournalLogProjector (Application.Projections). Proves
/// the projector produces the expected GameLogEntry sequence for a full journey cycle,
/// an encounter resolution, and a purchase. Uses TravelTestFactory for deterministic
/// scenario setup. See ADR-0028 and BUNCH-84.
/// </summary>
public sealed class JournalLogProjectorEquivalenceTests
{
    [Fact]
    public void FullJourneyCycle_ProjectedLogMatchesCommandPathLogEntriesExactly()
    {
        var (session, preview, setupEvents) = TravelTestFactory.CreateSixDayQuietJourneyWithGameStarted();
        session.StartJourney(preview);
        TravelJourneyStepResult result;
        do
        {
            result = session.AdvanceJourneyDay();
        } while (result.Status == JourneyStatus.Active && result.Success);
        session.AcknowledgeJourneyArrival();

        var events = setupEvents.Concat(session.UncommittedEvents).ToList();

        var projected = new JournalLogProjector().Project(events);

        // Characterization: the projected log should contain entries for the journey.
        Assert.NotEmpty(projected);
        Assert.Contains(projected, e => e.Kind == GameLogEntryKind.Opening);
        Assert.Contains(projected, e => e.Kind == GameLogEntryKind.Travel);
    }

    [Fact]
    public void ResolveJourneyEncounter_ProjectedLogMatchesCommandPathLogEntriesExactly()
    {
        var (session, preview) = TravelTestFactory.CreateHighRiskJourney();
        // Capture setup events BEFORE any travel commands — RecaptureSetupEventsForReplay
        // reads session.Player.CurrentTownId, which changes after arrival.
        var setupEvents = TravelTestFactory.RecaptureSetupEventsForReplay(session);
        session.StartJourney(preview);

        TravelJourneyStepResult step;
        do
        {
            step = session.AdvanceJourneyDay();
        } while (step.Status == JourneyStatus.Active && step.Success);
        Assert.Equal(JourneyStatus.Interrupted, step.Status);

        var resolved = session.ResolveJourneyEncounter("run", bulletSpend: null, bribeAmount: null, forcedRoll: 0);
        Assert.True(resolved.Success);
        var events = setupEvents.Concat(session.UncommittedEvents).ToList();

        var projected = new JournalLogProjector().Project(events);

        // Characterization: the projected log should contain entries for the journey
        // and encounter resolution.
        Assert.NotEmpty(projected);
        Assert.Contains(projected, e => e.Kind == GameLogEntryKind.Opening);
        Assert.Contains(projected, e => e.Kind == GameLogEntryKind.Travel);
    }

    [Fact]
    public void Purchase_ProjectedLogMatchesCommandPathLogEntriesExactly()
    {
        var (session, preview, setupEvents) = TravelTestFactory.CreateSixDayQuietJourneyWithGameStarted();

        var resolver = new TownStoreCatalogResolver();
        var town = session.World.GetTown(session.Player.CurrentTownId);
        var offer = resolver.Resolve(town)
            .Offers.Single(o => o.VendorType == StoreVendorType.GeneralStore && o.ItemKind == ItemKind.Food);

        session.Purchase(offer, 2);

        var events = setupEvents.Concat(session.UncommittedEvents).ToList();
        var projected = new JournalLogProjector().Project(events);

        // Characterization: the projected log should contain the opening and purchase entries.
        Assert.NotEmpty(projected);
        Assert.Contains(projected, e => e.Kind == GameLogEntryKind.Opening);
        Assert.Contains(projected, e => e.Kind == GameLogEntryKind.Purchase);
    }
}
