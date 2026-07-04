using WildBunch.Application.Games.Mapping;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Economy;
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
    public void LookAroundSaloonSurfacesAnActivePersonOfInterestAndRepeatLookAroundShowsNobodyElseOfInterest()
    {
        var session = CreateSessionWithoutKnownWarrants();
        var suspectId = new SuspectId("suspect-1");
        session.ForceDevSaloonOverride(DevSaloonOverride.ForSuspect(suspectId));
        session.MarkEventsCommitted();

        var lookAround = session.LookAroundSaloon();
        Assert.Equal(suspectId, session.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestId);
        var logCountBeforeConfront = GameSessionLogProjection.Project(session).Count;

        var confrontation = session.ConfrontSaloonPersonOfInterest();
        var logCountAfterConfront = GameSessionLogProjection.Project(session).Count;
        var repeatLookAround = session.LookAroundSaloon();

        Assert.True(lookAround.Success);
        Assert.Equal("You look around the saloon and spot a stranger with a scar on the left cheek.", lookAround.Message);

        Assert.False(confrontation.Success);
        Assert.Equal(SaloonPersonOfInterestConfrontationOutcome.Rejected, confrontation.Outcome);
        Assert.Contains("wanted identity", confrontation.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("passed", confrontation.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Null(session.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestId);
        Assert.False(session.CaseFile.TryGetWantedSuspectConfrontationState(suspectId, out _));
        Assert.Empty(session.CaseFile.SheriffTurnInSettlements);
        Assert.Equal(logCountBeforeConfront, logCountAfterConfront);

        Assert.True(repeatLookAround.Success);
        Assert.Equal("You look around the saloon again, but nobody of interest is here.", repeatLookAround.Message);
        Assert.Null(session.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestId);
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
    public void ConfrontSaloonPersonOfInterestRejectsWhenNoKnownWantedIdentityCanBeDeclared()
    {
        var session = CreateSessionWithPublicDescriptor();
        var activePersonOfInterest = new SuspectId("suspect-1");
        var declaredWantedIdentityHandle = "public-warrant-99";
        session.ForceDevSaloonOverride(DevSaloonOverride.ForSuspect(activePersonOfInterest));
        session.MarkEventsCommitted();

        var lookAround = session.LookAroundSaloon();

        Assert.True(lookAround.Success);
        Assert.Equal(activePersonOfInterest, session.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestId);
        var logCountBeforeConfront = GameSessionLogProjection.Project(session).Count;

        var result = session.ConfrontSaloonPersonOfInterest(declaredWantedIdentityHandle);

        Assert.False(result.Success);
        Assert.Equal(SaloonPersonOfInterestConfrontationOutcome.Rejected, result.Outcome);
        Assert.Equal(declaredWantedIdentityHandle, result.DeclaredWantedIdentityHandle);
        Assert.Contains("wanted identity", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("passed", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Null(session.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestId);
        Assert.Empty(session.CaseFile.WantedSuspectConfrontations);
        Assert.Empty(session.CaseFile.SheriffTurnInSettlements);
        Assert.Equal(logCountBeforeConfront, GameSessionLogProjection.Project(session).Count);
        Assert.False(session.CaseFile.TryGetWantedSuspectConfrontationState(activePersonOfInterest, out _));
        Assert.False(session.CaseFile.TryGetWantedSuspectConfrontationState(new SuspectId("suspect-2"), out _));
    }

    [Fact]
    public void ConfrontSaloonPersonOfInterestPaysTheWantedBountyOnceWhenArmedAndThePublicWantedIdentityMatches()
    {
        var session = CreateArmedWantedSession();
        var suspectId = new SuspectId("suspect-1");
        var capabilityResolver = new InventoryCapabilityResolver();

        Assert.True(capabilityResolver.Resolve(session.Player.Inventory).FirearmThreatAvailable);

        session.SetWantedSuspectPresenceState(suspectId, WantedSuspectPresenceState.AvailableInTown);
        session.ForceDevSaloonOverride(DevSaloonOverride.ForSuspect(suspectId));
        session.MarkEventsCommitted();
        var lookAround = session.LookAroundSaloon();
        Assert.True(lookAround.Success);
        Assert.Equal(suspectId, session.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestId);

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
    public void ConfrontSaloonPersonOfInterestFleesBeforeSheriffWhenTheCorrectWantedIdentityIsDeclaredWithoutAFirearm()
    {
        var session = CreateUnarmedWantedSession();
        var suspectId = new SuspectId("suspect-1");
        var capabilityResolver = new InventoryCapabilityResolver();

        Assert.False(capabilityResolver.Resolve(session.Player.Inventory).FirearmThreatAvailable);

        session.SetWantedSuspectPresenceState(suspectId, WantedSuspectPresenceState.AvailableInTown);
        session.ForceDevSaloonOverride(DevSaloonOverride.ForSuspect(suspectId));
        session.MarkEventsCommitted();
        var lookAround = session.LookAroundSaloon();
        Assert.True(lookAround.Success);
        Assert.Equal(suspectId, session.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestId);

        var result = session.ConfrontSaloonPersonOfInterest("warrant-public-1");
        var payload = System.Text.Json.JsonSerializer.Serialize(result);

        Assert.True(result.Success);
        Assert.Equal(SaloonPersonOfInterestConfrontationOutcome.Fled, result.Outcome);
        Assert.Equal("warrant-public-1", result.DeclaredWantedIdentityHandle);
        Assert.Equal("Mira Cline", result.TargetName);
        Assert.True(result.IsAlive);
        Assert.False(result.IsSecured);
        Assert.False(result.IsCitizen);
        Assert.Equal(SaloonPersonOfInterestKind.WantedSuspect, result.PersonOfInterestKind);
        Assert.Null(result.FineAmount);
        Assert.Null(result.WalletBefore);
        Assert.Null(result.WalletAfter);
        Assert.Contains("get away", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("sheriff", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Reno Pike", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"trueCulpritId\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"killerReleaseState\"", payload, StringComparison.OrdinalIgnoreCase);
        Assert.Null(session.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestId);
        Assert.Empty(session.CaseFile.SheriffTurnInSettlements);
        Assert.Single(session.CaseFile.WantedSuspectConfrontations);
        Assert.True(session.CaseFile.TryGetWantedSuspectConfrontationState(suspectId, out var confrontationState));
        Assert.Equal(WantedSuspectConfrontationOutcome.Fled, confrontationState.Outcome);
        Assert.True(confrontationState.IsAlive);
        Assert.False(confrontationState.IsSecured);
        Assert.Equal(25m, session.Player.Wallet.Cash);
        Assert.False(session.CaseFile.TryGetSheriffTurnInSettlementState(suspectId, out _));
    }

    [Fact]
    public void LookAroundSaloonCanSurfaceATownCitizenAndWrongDeclarationCapsTheFineAtTheAvailableWallet()
    {
        // Use dev override to force a citizen POI — the proper test seam per BUNCH-106 realignment.
        // Use a session with suspects that have identifying facts so the shared feature vocabulary is non-empty.
        var session = TestSessionFactory.CreateWithConfrontableSaloonSuspect();
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
        Assert.Equal(SaloonPersonOfInterestConfrontationOutcome.WrongWantedDeclaration, result.Outcome);
        Assert.Contains("sheriff identifies them as", result.Message);
        Assert.Contains("releases them", result.Message);
        Assert.Contains("$4.00", result.Message);
        Assert.DoesNotContain("town clerk", result.Message);
        Assert.True(result.IsCitizen);
        Assert.Equal(4m, result.FineAmount);
        Assert.Equal(4m, result.WalletBefore);
        Assert.Equal(0m, result.WalletAfter);
        Assert.Null(result.Disposition);
        Assert.Null(result.IsAlive);
        Assert.Null(result.IsSecured);
        Assert.Equal(SaloonPersonOfInterestKind.Citizen, result.PersonOfInterestKind);
        Assert.Equal(initialLogCount, GameSessionLogProjection.Project(session).Count);
        Assert.Equal(0m, session.Player.Wallet.Cash);
        Assert.Null(session.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestId);
        Assert.Null(session.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestKind);

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
        Assert.Equal(initialLogCount, GameSessionLogProjection.Project(session).Count);
    }

    [Fact]
    public void ConfrontSaloonPersonOfInterestRejectsAWrongWantedDeclarationWithoutRevealingTheWantedIdentity()
    {
        var session = CreateArmedWantedSession();
        session.Player.AdjustCash(4m - session.Player.Wallet.Cash);
        var suspectId = new SuspectId("suspect-1");
        session.SetWantedSuspectPresenceState(suspectId, WantedSuspectPresenceState.AvailableInTown);
        session.ForceDevSaloonOverride(DevSaloonOverride.ForSuspect(suspectId));
        session.MarkEventsCommitted();

        var lookAround = session.LookAroundSaloon();

        Assert.True(lookAround.Success);
        Assert.Equal("You look around the saloon and spot a stranger with Raven-feather pin.", lookAround.Message);
        Assert.Equal(suspectId, session.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestId);

        var result = session.ConfrontSaloonPersonOfInterest("warrant-99");

        Assert.True(result.Success);
        Assert.Equal(SaloonPersonOfInterestConfrontationOutcome.WrongWantedDeclaration, result.Outcome);
        Assert.Equal("a stranger with Raven-feather pin", result.TargetName);
        Assert.False(result.IsCitizen);
        Assert.Equal(SaloonPersonOfInterestKind.WantedSuspect, result.PersonOfInterestKind);
        Assert.True(result.IsAlive);
        Assert.False(result.IsSecured);
        Assert.Equal(4m, result.FineAmount);
        Assert.Equal(4m, result.WalletBefore);
        Assert.Equal(0m, result.WalletAfter);
        Assert.Contains("declaration is wrong", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Mira Cline", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Null(session.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestId);
        Assert.Empty(session.CaseFile.WantedSuspectConfrontations);
        Assert.Empty(session.CaseFile.SheriffTurnInSettlements);
        Assert.Equal(0m, session.Player.Wallet.Cash);
        Assert.False(session.CaseFile.TryGetWantedSuspectConfrontationState(suspectId, out _));
    }

    [Fact]
    public void ConfrontSaloonPersonOfInterestDoesNotClassifyABlankDeclarationAsWrongWantedDeclaration()
    {
        var session = CreateArmedWantedSession();
        session.Player.AdjustCash(4m - session.Player.Wallet.Cash);
        var suspectId = new SuspectId("suspect-1");
        session.SetWantedSuspectPresenceState(suspectId, WantedSuspectPresenceState.AvailableInTown);
        session.ForceDevSaloonOverride(DevSaloonOverride.ForSuspect(suspectId));
        session.MarkEventsCommitted();

        var lookAround = session.LookAroundSaloon();

        Assert.True(lookAround.Success);
        Assert.Equal("You look around the saloon and spot a stranger with Raven-feather pin.", lookAround.Message);

        var result = session.ConfrontSaloonPersonOfInterest(string.Empty);

        Assert.True(result.Success);
        Assert.NotEqual(SaloonPersonOfInterestConfrontationOutcome.WrongWantedDeclaration, result.Outcome);
        Assert.Equal(SaloonPersonOfInterestConfrontationOutcome.Fled, result.Outcome);
        Assert.False(result.IsCitizen);
        Assert.True(result.IsAlive);
        Assert.False(result.IsSecured);
        Assert.Null(result.FineAmount);
        Assert.Equal(4m, session.Player.Wallet.Cash);
        Assert.Null(session.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestId);
        Assert.Single(session.CaseFile.WantedSuspectConfrontations);
        Assert.Empty(session.CaseFile.SheriffTurnInSettlements);
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

    private static GameSession CreateSession()
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
        var currentTown = new Town(new TownId("current"), "Current Town", TownServices.None);
        var connectedTown = new Town(new TownId("connected"), "Connected Town", TownServices.None);
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
        var currentTown = new Town(new TownId("current"), "Current Town", TownServices.None);
        var connectedTown = new Town(new TownId("connected"), "Connected Town", TownServices.None);
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
        var currentTown = new Town(new TownId("current"), "Current Town", TownServices.None);
        var connectedTown = new Town(new TownId("connected"), "Connected Town", TownServices.None);
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
