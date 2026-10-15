using WildBunch.Application.Games.Mapping;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Economy;
using WildBunch.Domain.Events;
using WildBunch.Domain.Game;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using DomainInventory = WildBunch.Domain.Inventory.Inventory;
using DomainWorld = WildBunch.Domain.World.World;
using Town = WildBunch.Domain.World.Town;
using Trail = WildBunch.Domain.World.Trail;
using TrailId = WildBunch.Domain.World.TrailId;

namespace WildBunch.Domain.Tests;

public sealed class GameSessionSaloonPersonOfInterestTests
{
    [Fact]
    public void LookAroundSaloonSurfacesAnActivePersonOfInterestWithoutNamingThem()
    {
        var session = CreateSessionWithoutKnownWarrants();
        var suspectId = new SuspectId("suspect-1");
        session.ForceDevSaloonOverride(DevSaloonOverride.ForSuspect(suspectId));
        session.MarkEventsCommitted();

        var lookAround = session.LookAroundSaloon();
        Assert.Equal(suspectId, session.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestId);
        Assert.True(lookAround.Success);
        Assert.Equal("You look around the saloon and spot a stranger with a scar on the left cheek.", lookAround.Message);
        Assert.Equal(suspectId, session.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestId);
        Assert.Empty(session.CaseFile.WantedSuspectConfrontations);
    }

    [Fact]
    public void ConfrontSaloonPersonOfInterestRejectsWhenNoPersonOfInterestHasBeenSpotted()
    {
        var session = CreateSession();

        var result = session.ConfrontSaloonPersonOfInterest();

        Assert.False(result.Success);
        Assert.Equal(SaloonPersonOfInterestConfrontationOutcome.Rejected, result.Outcome);
        Assert.Contains("saloon", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(session.CaseFile.WantedSuspectConfrontations);
        Assert.Null(session.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestId);
    }

    [Fact]
    public void GoneToGroundWantedSuspectCanSurfaceAgainAfterReenteringTown()
    {
        var session = CreateSession();
        var suspectId = new SuspectId("suspect-1");
        session.SetWantedSuspectPresenceState(suspectId, WantedSuspectPresenceState.GoneToGround);
        session.ForceDevSaloonOverride(DevSaloonOverride.ForSuspect(suspectId));
        session.MarkEventsCommitted();

        var firstVisit = session.LookAroundSaloon();

        Assert.True(firstVisit.Success);
        Assert.Equal("You look around the saloon and spot a stranger with Raven-feather pin.", firstVisit.Message);
        Assert.Equal(suspectId, session.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestId);

        session.Player.TravelTo(new TownId("connected"));
        session.CurrentTownVisit.Reset(new TownId("connected"));
        session.ResetActionContextForTownChange();
        session.Player.TravelTo(new TownId("current"));
        session.CurrentTownVisit.Reset(new TownId("current"));
        session.ResetActionContextForTownChange();
        session.ForceDevSaloonOverride(DevSaloonOverride.ForSuspect(suspectId));
        session.MarkEventsCommitted();

        var secondVisit = session.LookAroundSaloon();

        Assert.True(secondVisit.Success);
        Assert.Equal("You look around the saloon and spot a stranger with Raven-feather pin.", secondVisit.Message);
        Assert.Equal(suspectId, session.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestId);
    }

    [Fact]
    public void LookAroundSaloonDoesNotSurfaceTheTrueCulprit()
    {
        var session = CreateSessionWithoutKnownWarrants();

        // The true culprit (suspect-2) is still gated — forcing it throws.
        var ex = Assert.Throws<InvalidOperationException>(() =>
            session.ForceDevSaloonOverride(DevSaloonOverride.ForSuspect(new SuspectId("suspect-2"))));
        Assert.Contains("killer trail is locked", ex.Message);
        Assert.Empty(session.CaseFile.WantedSuspectConfrontations);
    }

    [Fact]
    public void LookAroundSaloonUsesAPublicDescriptorWhenOneIsAvailable()
    {
        var session = CreateSessionWithPublicDescriptor();
        session.ForceDevSaloonOverride(DevSaloonOverride.ForSuspect(new SuspectId("suspect-1")));
        session.MarkEventsCommitted();

        var result = session.LookAroundSaloon();

        Assert.True(result.Success);
        Assert.Equal("You look around the saloon and spot a stranger with a scar on the left cheek.", result.Message);
        Assert.Equal(new SuspectId("suspect-1"), session.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestId);
        Assert.Empty(session.CaseFile.WantedSuspectConfrontations);
    }

    [Fact]
    public void ConfrontSaloonPersonOfInterestRejectsUnknownIdentityWhenNoWarrantsAreKnownWithoutConsumingThePerson()
    {
        var session = CreateSessionWithPublicDescriptor();
        var activePersonOfInterest = new SuspectId("suspect-1");
        var declaredWantedIdentityHandle = "public-warrant-99";
        session.ForceDevSaloonOverride(DevSaloonOverride.ForSuspect(activePersonOfInterest));
        session.MarkEventsCommitted();

        var lookAround = session.LookAroundSaloon();

        Assert.True(lookAround.Success);
        Assert.Equal(activePersonOfInterest, session.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestId);
        session.MarkEventsCommitted();
        var eventCountBeforeConfront = session.AllEvents.Count;

        var result = session.ConfrontSaloonPersonOfInterest(declaredWantedIdentityHandle);
        Assert.False(result.Success);
        Assert.Equal(SaloonPersonOfInterestConfrontationOutcome.Rejected, result.Outcome);
        Assert.Equal(declaredWantedIdentityHandle, result.DeclaredWantedIdentityHandle);
        Assert.Contains("wanted identity", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(result.SessionChanged);
        Assert.Equal(activePersonOfInterest, session.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestId);
        Assert.Empty(session.CaseFile.WantedSuspectConfrontations);
        Assert.Empty(session.CaseFile.SheriffTurnInSettlements);
        Assert.Equal(0, session.CaseFile.KillerReleaseState.Progress);
        Assert.Equal(eventCountBeforeConfront, session.AllEvents.Count);
        Assert.Empty(session.UncommittedEvents);
        Assert.False(session.CaseFile.TryGetWantedSuspectConfrontationState(activePersonOfInterest, out _));
        Assert.False(session.CaseFile.TryGetWantedSuspectConfrontationState(new SuspectId("suspect-2"), out _));
    }

    [Fact]
    public void ConfrontSaloonPersonOfInterestPaysTheWantedBountyOnceForAnObservedSuspectWithoutGlobalPresenceSeeding()
    {
        var session = CreateArmedWantedSession();
        var suspectId = new SuspectId("suspect-1");
        var capabilityResolver = new InventoryCapabilityResolver();

        Assert.True(capabilityResolver.Resolve(session.Player.Inventory).FirearmThreatAvailable);
        Assert.Equal(WantedSuspectPresenceState.Unavailable, session.GetWantedSuspectPresenceState(suspectId));
        session.ForceDevSaloonOverride(DevSaloonOverride.ForSuspect(suspectId));
        session.MarkEventsCommitted();
        var lookAround = session.LookAroundSaloon();
        Assert.True(lookAround.Success);
        Assert.Equal(suspectId, session.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestId);
        Assert.Equal(WantedSuspectPresenceState.Unavailable, session.GetWantedSuspectPresenceState(suspectId));

        var firstTurnIn = session.ConfrontSaloonPersonOfInterest("warrant-public-1");
        var repeatTurnIn = session.SettleSheriffTurnIn(suspectId, isAlive: true);

        Assert.True(firstTurnIn.Success);
        Assert.Equal(SaloonPersonOfInterestConfrontationOutcome.Surrendered, firstTurnIn.Outcome);
        Assert.Equal("warrant-public-1", firstTurnIn.DeclaredWantedIdentityHandle);
        Assert.Equal("Mira Cline", firstTurnIn.TargetName);
        Assert.True(firstTurnIn.IsAlive);
        Assert.True(firstTurnIn.IsSecured);
        Assert.Equal(SaloonPersonOfInterestKind.WantedSuspect, firstTurnIn.PersonOfInterestKind);
        Assert.Contains("pays you $2500.00", firstTurnIn.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Null(session.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestId);
        Assert.True(session.CaseFile.TryGetWantedSuspectConfrontationState(suspectId, out var confrontationState));
        Assert.True(confrontationState.IsAlive);
        Assert.True(confrontationState.IsSecured);
        Assert.Single(session.CaseFile.WantedSuspectConfrontations);
        Assert.Single(session.CaseFile.SheriffTurnInSettlements);
        Assert.True(session.CaseFile.TryGetSheriffTurnInSettlementState(suspectId, out var settlementState));
        Assert.Equal("Mira Cline", settlementState.TargetName);
        Assert.True(settlementState.IsAlive);
        Assert.Equal(2500m, settlementState.BountyAmount);
        Assert.Equal(2525m, session.Player.Wallet.Cash);

        Assert.False(repeatTurnIn.Success);
        Assert.Equal(SheriffTurnInOutcome.Rejected, repeatTurnIn.Outcome);
        Assert.Equal(2525m, session.Player.Wallet.Cash);
        Assert.Single(session.CaseFile.SheriffTurnInSettlements);
        Assert.Contains("already been paid", repeatTurnIn.Message, StringComparison.OrdinalIgnoreCase);

        var payload = System.Text.Json.JsonSerializer.Serialize(firstTurnIn);
        Assert.DoesNotContain("\"trueCulpritId\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"killerReleaseState\"", payload, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ConfrontSaloonPersonOfInterestTakesInCorrectlyNamedWantedPersonWithAnUnloadedGun()
    {
        var session = CreateArmedWantedSession();
        var suspectId = new SuspectId("suspect-1");
        session.Player.RemoveQuantity(ItemKind.RevolverAmmo, 2);
        Assert.True(session.Player.HasItem(ItemKind.Revolver));
        Assert.Equal(0, session.Player.GetQuantity(ItemKind.RevolverAmmo));
        session.ForceDevSaloonOverride(DevSaloonOverride.ForSuspect(suspectId));
        session.MarkEventsCommitted();

        Assert.True(session.LookAroundSaloon().Success);
        var result = session.ConfrontSaloonPersonOfInterest("warrant-public-1");

        Assert.True(result.Success);
        Assert.Equal(SaloonPersonOfInterestConfrontationOutcome.Surrendered, result.Outcome);
        Assert.Single(session.CaseFile.SheriffTurnInSettlements);
        Assert.Equal(2525m, session.Player.Wallet.Cash);
    }

    [Fact]
    public void ConfrontSaloonPersonOfInterestRefusesTakeInWithoutAFirearmAndDoesNotMarkTheSuspectAsFled()
    {
        var session = CreateUnarmedWantedSession();
        var suspectId = new SuspectId("suspect-1");
        var capabilityResolver = new InventoryCapabilityResolver();

        Assert.False(capabilityResolver.Resolve(session.Player.Inventory).FirearmThreatAvailable);

        session.ForceDevSaloonOverride(DevSaloonOverride.ForSuspect(suspectId));
        session.MarkEventsCommitted();
        var lookAround = session.LookAroundSaloon();
        Assert.True(lookAround.Success);
        Assert.Equal(suspectId, session.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestId);

        var result = session.ConfrontSaloonPersonOfInterest("warrant-public-1");
        var payload = System.Text.Json.JsonSerializer.Serialize(result);

        Assert.True(result.Success);
        Assert.Equal(SaloonPersonOfInterestConfrontationOutcome.Refused, result.Outcome);
        Assert.Equal("warrant-public-1", result.DeclaredWantedIdentityHandle);
        Assert.Equal("a stranger with Raven-feather pin", result.TargetName);
        Assert.DoesNotContain("Mira Cline", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.True(result.IsAlive);
        Assert.False(result.IsSecured);
        Assert.False(result.IsCitizen);
        Assert.Equal(SaloonPersonOfInterestKind.WantedSuspect, result.PersonOfInterestKind);
        Assert.Null(result.FineAmount);
        Assert.Null(result.WalletBefore);
        Assert.Null(result.WalletAfter);
        Assert.Contains("refuse", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("fled", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("sheriff", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Reno Pike", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"trueCulpritId\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"killerReleaseState\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.Null(session.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestId);
        Assert.Empty(session.CaseFile.SheriffTurnInSettlements);
        Assert.Equal(0, session.CaseFile.KillerReleaseState.Progress);
        Assert.Empty(session.CaseFile.WantedSuspectConfrontations);
        Assert.Equal(WantedSuspectPresenceState.Unavailable, session.GetWantedSuspectPresenceState(suspectId));
        Assert.Equal(25m, session.Player.Wallet.Cash);
        Assert.False(session.CaseFile.TryGetSheriffTurnInSettlementState(suspectId, out _));

        var refusalEvent = Assert.Single(session.AllEvents.OfType<SaloonPersonOfInterestConfronted>());
        Assert.Equal(SaloonPersonOfInterestConfrontationOutcome.Refused, refusalEvent.Outcome);
        Assert.Equal("a stranger with Raven-feather pin", refusalEvent.TargetName);
        Assert.DoesNotContain("Mira Cline", refusalEvent.Message, StringComparison.OrdinalIgnoreCase);
        var replayed = GameSession.RehydrateFromEvents(session.Id, session.World, session.AllEvents);
        Assert.Equal(25m, replayed.Player.Wallet.Cash);
        Assert.Empty(replayed.CaseFile.WantedSuspectConfrontations);
    }

    [Fact]
    public void LookAroundSaloonCanSurfaceATownCitizenAndWrongDeclarationCapsTheFineAtTheAvailableWallet()
    {
        // Use dev override to force a citizen POI — the proper test seam per BUNCH-106 realignment.
        // Use a session with suspects that have identifying facts so the shared feature vocabulary is non-empty.
        var session = CreateUnarmedWantedSession();
        Assert.False(session.Player.HasItem(ItemKind.Revolver));
        session.Player.AdjustCash(4m - session.Player.Wallet.Cash);
        session.ForceDevSaloonOverride(DevSaloonOverride.ForCitizen());
        session.MarkEventsCommitted();
        var initialLogCount = GameSessionLogProjection.Project(session).Count;

        var lookAround = session.LookAroundSaloon();

        Assert.True(lookAround.Success);
        Assert.Contains("a stranger with", lookAround.Message);
        Assert.DoesNotContain("town clerk", lookAround.Message);
        Assert.Equal(SaloonPersonOfInterestKind.Citizen, session.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestKind);
        Assert.NotNull(session.CurrentTownVisit.CurrentTownState.ActiveSaloonCitizenRole);
        Assert.Equal(initialLogCount, GameSessionLogProjection.Project(session).Count);
        Assert.Null(session.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestId);

        var result = session.ConfrontSaloonPersonOfInterest("warrant-public-1");

        Assert.True(result.Success);
        Assert.Equal(SaloonPersonOfInterestConfrontationOutcome.CitizenReleased, result.Outcome);
        Assert.Equal(0, session.CaseFile.KillerReleaseState.Progress);
        Assert.Contains("come quietly", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("releases them", result.Message);
        Assert.Contains("$4.00", result.Message);
        Assert.DoesNotContain("town clerk", result.Message);
        Assert.True(result.IsCitizen);
        Assert.Equal(4m, result.FineAmount);
        Assert.Equal(4m, result.WalletBefore);
        Assert.Equal(0m, result.WalletAfter);
        Assert.Null(result.Disposition);
        Assert.True(result.IsAlive);
        Assert.True(result.IsSecured);
        Assert.Equal(SaloonPersonOfInterestKind.Citizen, result.PersonOfInterestKind);
        var journal = GameSessionLogProjection.Project(session);
        Assert.Equal(initialLogCount + 1, journal.Count);
        Assert.Equal(result.Message, journal[^1].Message);
        Assert.Equal(0m, session.Player.Wallet.Cash);
        Assert.Null(session.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestId);
        Assert.Null(session.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestKind);
        var citizenReleaseEvent = Assert.Single(session.AllEvents.OfType<SaloonPersonOfInterestConfronted>());
        Assert.Equal(SaloonPersonOfInterestConfrontationOutcome.CitizenReleased, citizenReleaseEvent.Outcome);
        Assert.Equal(4m, citizenReleaseEvent.WalletBefore);
        Assert.Equal(0m, citizenReleaseEvent.WalletAfter);
        var replayed = GameSession.RehydrateFromEvents(session.Id, session.World, session.AllEvents);
        Assert.Equal(0m, replayed.Player.Wallet.Cash);
        Assert.Null(replayed.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestKind);

        // Repeat visit: force citizen again to verify the flow works on a fresh visit.
        session.Player.TravelTo(new TownId("connected"));
        session.CurrentTownVisit.Reset(new TownId("connected"));
        session.ResetActionContextForTownChange();
        session.Player.TravelTo(new TownId("current"));
        session.CurrentTownVisit.Reset(new TownId("current"));
        session.ResetActionContextForTownChange();

        session.ForceDevSaloonOverride(DevSaloonOverride.ForCitizen());
        session.MarkEventsCommitted();
        var repeatLookAround = session.LookAroundSaloon();

        Assert.True(repeatLookAround.Success);
        Assert.Contains("a stranger with", repeatLookAround.Message);
        Assert.DoesNotContain("town clerk", repeatLookAround.Message);
        var journalAfterRepeatLookAround = GameSessionLogProjection.Project(session);
        Assert.Equal(initialLogCount + 1, journalAfterRepeatLookAround.Count);
        Assert.Equal(result.Message, journalAfterRepeatLookAround[^1].Message);
    }

    [Fact]
    public void ConfrontSaloonPersonOfInterestReleasesWronglyNamedWantedPersonAndFinesTenWithoutBounty()
    {
        var session = CreateArmedWantedSession();
        Assert.True(session.Player.HasItem(ItemKind.Revolver));
        Assert.True(session.Player.GetQuantity(ItemKind.RevolverAmmo) > 0);
        var suspectId = new SuspectId("suspect-1");
        session.ForceDevSaloonOverride(DevSaloonOverride.ForSuspect(suspectId));
        session.MarkEventsCommitted();

        var lookAround = session.LookAroundSaloon();

        Assert.True(lookAround.Success);
        Assert.Equal("You look around the saloon and spot a stranger with Raven-feather pin.", lookAround.Message);
        Assert.Equal(suspectId, session.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestId);

        var result = session.ConfrontSaloonPersonOfInterest("warrant-public-2");

        Assert.True(result.Success);
        Assert.Equal(SaloonPersonOfInterestConfrontationOutcome.WrongWantedDeclaration, result.Outcome);
        Assert.Equal(0, session.CaseFile.KillerReleaseState.Progress);
        Assert.Equal("a stranger with Raven-feather pin", result.TargetName);
        Assert.False(result.IsCitizen);
        Assert.Equal(SaloonPersonOfInterestKind.WantedSuspect, result.PersonOfInterestKind);
        Assert.True(result.IsAlive);
        Assert.False(result.IsSecured);
        Assert.Equal(10m, result.FineAmount);
        Assert.Equal(25m, result.WalletBefore);
        Assert.Equal(15m, result.WalletAfter);
        Assert.Contains("declaration is wrong", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Mira Cline", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Null(session.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestId);
        Assert.Empty(session.CaseFile.WantedSuspectConfrontations);
        Assert.Empty(session.CaseFile.SheriffTurnInSettlements);
        Assert.Equal(15m, session.Player.Wallet.Cash);
        Assert.False(session.CaseFile.TryGetWantedSuspectConfrontationState(suspectId, out _));

        var wrongNameEvent = Assert.Single(session.AllEvents.OfType<SaloonPersonOfInterestConfronted>());
        Assert.Equal(SaloonPersonOfInterestConfrontationOutcome.WrongWantedDeclaration, wrongNameEvent.Outcome);
        Assert.Equal(25m, wrongNameEvent.WalletBefore);
        Assert.Equal(15m, wrongNameEvent.WalletAfter);
        var replayed = GameSession.RehydrateFromEvents(session.Id, session.World, session.AllEvents);
        Assert.Equal(15m, replayed.Player.Wallet.Cash);
        Assert.Empty(replayed.CaseFile.SheriffTurnInSettlements);
    }

    [Fact]
    public void ConfrontSaloonPersonWithUnreadPosterAndKnownDifferentIdentityUsesWrongNameOutcome()
    {
        var session = CreateArmedWantedSession(includePrimaryWarrantInKnown: false);
        var suspectId = new SuspectId("suspect-1");
        session.ForceDevSaloonOverride(DevSaloonOverride.ForSuspect(suspectId));
        session.MarkEventsCommitted();
        Assert.True(session.LookAroundSaloon().Success);

        var result = session.ConfrontSaloonPersonOfInterest("warrant-public-2");

        Assert.True(result.Success);
        Assert.Equal(SaloonPersonOfInterestConfrontationOutcome.WrongWantedDeclaration, result.Outcome);
        Assert.Equal("an unfamiliar person", result.TargetName);
        Assert.Equal(10m, result.FineAmount);
        Assert.Equal(15m, session.Player.Wallet.Cash);
        Assert.DoesNotContain("Mira Cline", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("warrant-public-1", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ConfrontSaloonPersonOfInterestRejectsABlankDeclarationWithoutConsumingTheObservedPerson()
    {
        var session = CreateArmedWantedSession();
        session.Player.AdjustCash(4m - session.Player.Wallet.Cash);
        var suspectId = new SuspectId("suspect-1");
        session.ForceDevSaloonOverride(DevSaloonOverride.ForSuspect(suspectId));
        session.MarkEventsCommitted();

        var lookAround = session.LookAroundSaloon();

        Assert.True(lookAround.Success);
        Assert.Equal("You look around the saloon and spot a stranger with Raven-feather pin.", lookAround.Message);
        session.MarkEventsCommitted();
        var turnBeforeBlankAttempt = session.Clock.Turn;

        var result = session.ConfrontSaloonPersonOfInterest(string.Empty);

        Assert.False(result.Success);
        Assert.Equal(SaloonPersonOfInterestConfrontationOutcome.Rejected, result.Outcome);
        Assert.Null(result.IsCitizen);
        Assert.Null(result.IsAlive);
        Assert.Null(result.IsSecured);
        Assert.Null(result.FineAmount);
        Assert.Equal(4m, session.Player.Wallet.Cash);
        Assert.Equal(suspectId, session.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestId);
        Assert.Empty(session.CaseFile.WantedSuspectConfrontations);
        Assert.Empty(session.CaseFile.SheriffTurnInSettlements);
        Assert.Equal(turnBeforeBlankAttempt, session.Clock.Turn);
        Assert.Empty(session.UncommittedEvents);
    }

    [Fact]
    public void ConfrontSaloonPersonOfInterestRejectsAnUnknownWantedIdentityWithoutConsumingTheObservedPersonOrFiningThePlayer()
    {
        var session = CreateArmedWantedSession();
        var suspectId = new SuspectId("suspect-1");
        session.ForceDevSaloonOverride(DevSaloonOverride.ForSuspect(suspectId));
        session.MarkEventsCommitted();
        Assert.True(session.LookAroundSaloon().Success);
        session.MarkEventsCommitted();
        var eventsBeforeAttempt = session.AllEvents.Count;

        var result = session.ConfrontSaloonPersonOfInterest("made-up-warrant");

        Assert.False(result.Success);
        Assert.Equal(SaloonPersonOfInterestConfrontationOutcome.Rejected, result.Outcome);
        Assert.False(result.SessionChanged);
        Assert.Equal(25m, session.Player.Wallet.Cash);
        Assert.Equal(suspectId, session.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestId);
        Assert.Equal(WantedSuspectPresenceState.Unavailable, session.GetWantedSuspectPresenceState(suspectId));
        Assert.Equal(eventsBeforeAttempt, session.AllEvents.Count);
        Assert.Empty(session.UncommittedEvents);
    }

    private static GameSession CreateArmedWantedSession(
        bool includeSecondaryWarrantInKnown = true,
        bool includePrimaryWarrantInKnown = true)
    {
        var currentTown = new Town(new TownId("current"), "Current Town");
        var connectedTown = new Town(new TownId("connected"), "Connected Town");
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

        var miraWarrant = new Warrant(
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
            "Wanted for a stage robbery.");
        var renoWarrant = new Warrant(
            new WarrantId("warrant-public-2"),
            "Reno Pike",
            new WarrantTerms(
                WarrantDisposition.DeadOrAlive,
                1800m,
                new[] { "Black Reno" },
                new[] { "a black duster" },
                "Silver Creek Marshal",
                InvestigationTargetKind.GangMember,
                Array.Empty<OutlawGangId>(),
                null),
            "Wanted for a train robbery.");
        var publicWarrants = new[] { miraWarrant, renoWarrant };
        var knownWarrants = new List<Warrant>();
        if (includePrimaryWarrantInKnown)
        {
            knownWarrants.Add(miraWarrant);
        }

        if (includeSecondaryWarrantInKnown)
        {
            knownWarrants.Add(renoWarrant);
        }

        var caseFile = new CaseFile(
            accusation: null,
            suspects,
            trueCulpritId: new SuspectId("suspect-2"),
            openingLead: CaseOpeningLead.Create("Follow the public leads and look for a signature mark."),
            knownClues: Array.Empty<Clue>(),
            knownWarrants: knownWarrants,
            publicWarrants: publicWarrants);

        var inventory = new DomainInventory(
            new[]
            {
                new InventoryItem(ItemKind.Revolver, 1),
                new InventoryItem(ItemKind.RevolverAmmo, 2)
            });

        return TestSessionFactory.StartGameCanonical("Ranger Vale", world, caseFile, currentTown.Id, wallet: null, inventory: inventory, gameDifficulty: GameDifficulty.Standard);
    }

    private static GameSession CreateUnarmedWantedSession()
    {
        var currentTown = new Town(new TownId("current"), "Current Town");
        var connectedTown = new Town(new TownId("connected"), "Connected Town");
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

    private static GameSession CreateSession()
    {
        var currentTown = new Town(new TownId("current"), "Current Town");
        var connectedTown = new Town(new TownId("connected"), "Connected Town");
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
                    new WarrantId("warrant-1"),
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

    private static GameSession CreateCitizenSession(Wallet? wallet = null)
    {
        var currentTown = new Town(new TownId("current"), "Current Town");
        var connectedTown = new Town(new TownId("connected"), "Connected Town");
        var world = new DomainWorld(
            new[] { currentTown, connectedTown },
            new[] { new Trail(new TrailId("trail-1"), currentTown.Id, connectedTown.Id, TrailRisk.Low) });

        var caseFile = new CaseFile(
            accusation: null,
            Array.Empty<Suspect>(),
            trueCulpritId: new SuspectId("suspect-2"),
            openingLead: CaseOpeningLead.Create("Follow the public leads and look for a signature mark."),
            knownClues: Array.Empty<Clue>(),
            knownWarrants: Array.Empty<Warrant>());

        return TestSessionFactory.StartGameCanonical("Ranger Vale", world, caseFile, currentTown.Id, wallet ?? Wallet.Starting(25m), inventory: null, gameDifficulty: GameDifficulty.Standard);
    }

    private static GameSession CreateSessionWithoutKnownWarrants()
    {
        var currentTown = new Town(new TownId("current"), "Current Town");
        var connectedTown = new Town(new TownId("connected"), "Connected Town");
        var world = new DomainWorld(
            new[] { currentTown, connectedTown },
            new[] { new Trail(new TrailId("trail-1"), currentTown.Id, connectedTown.Id, TrailRisk.Low) });

        var suspects = new[]
        {
            new Suspect(
                new SuspectId("suspect-1"),
                "Mira Cline",
                new SuspectProfile(
                    Array.Empty<SuspectAlias>(),
                    new[] { new SuspectIdentityFact(FeatureLanguage.Raw("Has a scar on the left cheek.", "a scar on the left cheek", "has a scar on the left cheek")) }),
                SuspectTraits.Empty,
                SuspectStatus.AtLarge),
            new Suspect(new SuspectId("suspect-2"), "Reno Pike", SuspectTraits.Empty, SuspectStatus.AtLarge)
        };

        var caseFile = new CaseFile(
            accusation: null,
            suspects,
            trueCulpritId: new SuspectId("suspect-2"),
            openingLead: CaseOpeningLead.Create("Follow the public leads and look for a signature mark."),
            knownClues: Array.Empty<Clue>(),
            knownWarrants: Array.Empty<Warrant>());

        return TestSessionFactory.StartGameCanonical("Ranger Vale", world, caseFile, currentTown.Id, gameDifficulty: GameDifficulty.Standard);
    }

    private static GameSession CreateSessionWithPublicDescriptor()
    {
        var currentTown = new Town(new TownId("current"), "Current Town");
        var connectedTown = new Town(new TownId("connected"), "Connected Town");
        var world = new DomainWorld(
            new[] { currentTown, connectedTown },
            new[] { new Trail(new TrailId("trail-1"), currentTown.Id, connectedTown.Id, TrailRisk.Low) });

        var suspects = new[]
        {
            new Suspect(
                new SuspectId("suspect-1"),
                "Mira Cline",
                new SuspectProfile(
                    Array.Empty<SuspectAlias>(),
                    new[] { new SuspectIdentityFact(FeatureLanguage.Raw("Has a scar on the left cheek.", "a scar on the left cheek", "has a scar on the left cheek")) }),
                SuspectTraits.Empty,
                SuspectStatus.AtLarge),
            new Suspect(new SuspectId("suspect-2"), "Reno Pike", SuspectTraits.Empty, SuspectStatus.AtLarge)
        };

        var caseFile = new CaseFile(
            accusation: null,
            suspects,
            trueCulpritId: new SuspectId("suspect-2"),
            openingLead: CaseOpeningLead.Create("Follow the public leads and look for a signature mark."),
            knownClues: Array.Empty<Clue>(),
            knownWarrants: Array.Empty<Warrant>());

        return TestSessionFactory.StartGameCanonical("Ranger Vale", world, caseFile, currentTown.Id, gameDifficulty: GameDifficulty.Standard);
    }
}
