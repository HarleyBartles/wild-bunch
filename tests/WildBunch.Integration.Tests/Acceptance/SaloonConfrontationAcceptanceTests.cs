using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using WildBunch.Application.Abstractions;
using WildBunch.Application.Dev.Models;
using WildBunch.Application.Games.Mapping;
using WildBunch.Application.Games.Models;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Economy;
using WildBunch.Domain.Events;
using WildBunch.Domain.Game;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using WildBunch.Integration.Tests.TestInfrastructure;

namespace WildBunch.Integration.Tests.Acceptance;

public sealed class SaloonConfrontationAcceptanceTests
{
    [Fact]
    public async Task PostSaloonLookAroundThenConfrontWithoutGunRecordsRefusalInsteadOfFlight()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateAuthenticatedClient();

        var createdSession = await SeedSessionWithAvailableSaloonSuspectAsync(factory);

        // Force the suspect POI via the dev override so the probabilistic saloon
        // roll deterministically surfaces suspect-1.
        await client.PostAsJsonAsync(
            $"/api/dev/sessions/{createdSession.Id}/saloon/force-override",
            new ForceSaloonOverrideRequestDto(ForcedKind: "Suspect", ForcedSuspectId: "suspect-1", ForcedCitizenRoleKey: null));

        var lookAroundResponse = await client.PostAsync($"/api/games/{createdSession.Id}/investigations/saloon/look-around", content: null);

        Assert.Equal(HttpStatusCode.OK, lookAroundResponse.StatusCode);

        var surfacedSession = await LoadDomainSessionAsync(factory, createdSession.Id);
        Assert.Equal(new SuspectId("suspect-1"), surfacedSession.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestId);

        var confrontationResponse = await client.PostAsJsonAsync(
            $"/api/games/{createdSession.Id}/investigations/saloon/confront",
            new
            {
                declaredWantedIdentityHandle = "warrant-1"
            });

        Assert.Equal(HttpStatusCode.OK, confrontationResponse.StatusCode);

        var result = await confrontationResponse.Content.ReadFromJsonAsync<SaloonPersonOfInterestConfrontationResultDto>();
        Assert.NotNull(result);
        Assert.True(result!.Success);
        Assert.Equal("warrant-1", result.DeclaredWantedIdentityHandle);
        Assert.Equal("a stranger with Raven-feather pin", result.TargetName);
        Assert.Equal(SaloonPersonOfInterestConfrontationOutcome.Refused, result.Outcome);
        Assert.True(result.IsAlive);
        Assert.False(result.IsSecured);
        Assert.True(result.SessionChanged);
        Assert.Null(result.CurrentSession.ActiveSaloonWantedSuspect);

        var reloadedSession = await LoadDomainSessionAsync(factory, createdSession.Id);
        Assert.Equal(WantedSuspectPresenceState.AvailableInTown, reloadedSession.GetWantedSuspectPresenceState(new SuspectId("suspect-1")));
        Assert.Null(reloadedSession.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestId);
        Assert.Empty(reloadedSession.CaseFile.WantedSuspectConfrontations);
    }

    [Fact]
    public async Task PostSaloonConfrontWithFabricatedIdentityRejectsWithoutConsumingTheObservedPersonOrFiningThePlayer()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateAuthenticatedClient();

        var createdSession = await SeedSessionWithAvailableSaloonSuspectAsync(factory);
        await client.PostAsJsonAsync(
            $"/api/dev/sessions/{createdSession.Id}/saloon/force-override",
            new ForceSaloonOverrideRequestDto(ForcedKind: "Suspect", ForcedSuspectId: "suspect-1", ForcedCitizenRoleKey: null));
        await client.PostAsync($"/api/games/{createdSession.Id}/investigations/saloon/look-around", content: null);

        var response = await client.PostAsJsonAsync(
            $"/api/games/{createdSession.Id}/investigations/saloon/confront",
            new { declaredWantedIdentityHandle = "fabricated-warrant" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<SaloonPersonOfInterestConfrontationResultDto>();
        Assert.NotNull(result);
        Assert.False(result!.Success);
        Assert.Equal(SaloonPersonOfInterestConfrontationOutcome.Rejected, result.Outcome);
        Assert.False(result.SessionChanged);
        Assert.Null(result.FineAmount);

        var reloadedSession = await LoadDomainSessionAsync(factory, createdSession.Id);
        Assert.Equal(new SuspectId("suspect-1"), reloadedSession.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestId);
        Assert.Empty(reloadedSession.AllEvents.OfType<SaloonPersonOfInterestConfronted>());
        Assert.Equal(25m, reloadedSession.Player.Wallet.Cash);
    }

    [Fact]
    public async Task PostSaloonConfrontWithKnownIdentitySettlesTheSelectedWantedPerson()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateAuthenticatedClient();

        var createdSession = await SeedSessionWithAvailableSaloonSuspectAsync(factory, armed: true);

        // Force the suspect POI via the dev override so the probabilistic saloon
        // roll deterministically surfaces suspect-1.
        await client.PostAsJsonAsync(
            $"/api/dev/sessions/{createdSession.Id}/saloon/force-override",
            new ForceSaloonOverrideRequestDto(ForcedKind: "Suspect", ForcedSuspectId: "suspect-1", ForcedCitizenRoleKey: null));

        await client.PostAsync($"/api/games/{createdSession.Id}/investigations/saloon/look-around", content: null);

        var confrontationResponse = await client.PostAsJsonAsync(
            $"/api/games/{createdSession.Id}/investigations/saloon/confront",
            new
            {
                declaredWantedIdentityHandle = "warrant-1"
            });

        Assert.Equal(HttpStatusCode.OK, confrontationResponse.StatusCode);

        var result = await confrontationResponse.Content.ReadFromJsonAsync<SaloonPersonOfInterestConfrontationResultDto>();
        Assert.NotNull(result);
        Assert.True(result!.Success);
        Assert.Equal("warrant-1", result.DeclaredWantedIdentityHandle);
        Assert.Equal("Mira Cline", result.TargetName);
        Assert.Equal(SaloonPersonOfInterestConfrontationOutcome.Surrendered, result.Outcome);
        Assert.True(result.IsSecured);

        var reloadedSession = await LoadDomainSessionAsync(factory, createdSession.Id);
        var settlement = Assert.Single(reloadedSession.CaseFile.SheriffTurnInSettlements);
        Assert.Equal(2500m, settlement.BountyAmount);
    }

    [Fact]
    public async Task PostSaloonLookAroundThenConfrontCitizenRecordsQuietReleaseAndWalletCappedFine()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateAuthenticatedClient();

        var createdSession = await SeedSessionWithCitizenSaloonPersonOfInterestAsync(factory);

        // Force a citizen POI via the dev override so the probabilistic saloon
        // roll deterministically surfaces a citizen instead of nobody. Force the
        // "town-clerk" role for a deterministic encounter.
        await client.PostAsJsonAsync(
            $"/api/dev/sessions/{createdSession.Id}/saloon/force-override",
            new ForceSaloonOverrideRequestDto(ForcedKind: "Citizen", ForcedSuspectId: null, ForcedCitizenRoleKey: "town-clerk"));

        var lookAroundResponse = await client.PostAsync($"/api/games/{createdSession.Id}/investigations/saloon/look-around", content: null);

        Assert.Equal(HttpStatusCode.OK, lookAroundResponse.StatusCode);

        var surfacedSession = await LoadDomainSessionAsync(factory, createdSession.Id);
        // No suspects in the session, so no shared feature vocabulary — the
        // concealment descriptor is the fallback "an unfamiliar face".
        Assert.Equal("an unfamiliar face", surfacedSession.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestDescriptor);
        Assert.Null(surfacedSession.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestId);

        var confrontationResponse = await client.PostAsJsonAsync(
            $"/api/games/{createdSession.Id}/investigations/saloon/confront",
            new
            {
                declaredWantedIdentityHandle = "warrant-1"
            });

        Assert.Equal(HttpStatusCode.OK, confrontationResponse.StatusCode);

        var result = await confrontationResponse.Content.ReadFromJsonAsync<SaloonPersonOfInterestConfrontationResultDto>();
        Assert.NotNull(result);
        Assert.True(result!.Success);
        Assert.True(result.IsCitizen);
        Assert.Equal(SaloonPersonOfInterestConfrontationOutcome.CitizenReleased, result.Outcome);
        Assert.True(result.IsAlive);
        Assert.True(result.IsSecured);
        Assert.Equal(4m, result.FineAmount);
        Assert.Equal(4m, result.WalletBefore);
        Assert.Equal(0m, result.WalletAfter);
        Assert.Equal("an unfamiliar face", result.TargetName);
        Assert.Null(result.CurrentSession.ActiveSaloonPersonOfInterest);
        Assert.Equal(0m, result.CurrentSession.Inventory!.Wallet.Cash);

        var reloadedSession = await LoadDomainSessionAsync(factory, createdSession.Id);
        Assert.Equal(0m, reloadedSession.Player.Wallet.Cash);
        Assert.Null(reloadedSession.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestId);
        Assert.Null(reloadedSession.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestDescriptor);
    }

    private static async Task<GameSessionDto> SeedSessionWithAvailableSaloonSuspectAsync(PostgreSqlApiFactory factory, bool armed = false)
    {
        var session = CreateSession(armed);
        session.SetWantedSuspectPresenceState(new SuspectId("suspect-1"), WantedSuspectPresenceState.AvailableInTown);

        using var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IGameSessionRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IGameSessionUnitOfWork>();
        await repository.StoreAsync(session);
        await unitOfWork.CommitAsync();

        return GameSessionMapper.ToDto(session);
    }

    private static async Task<GameSessionDto> SeedSessionWithCitizenSaloonPersonOfInterestAsync(PostgreSqlApiFactory factory)
    {
        var session = CreateCitizenSession();

        using var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IGameSessionRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IGameSessionUnitOfWork>();
        await repository.StoreAsync(session);
        await unitOfWork.CommitAsync();

        return GameSessionMapper.ToDto(session);
    }

    private static async Task<GameSession> LoadDomainSessionAsync(PostgreSqlApiFactory factory, Guid sessionId)
    {
        using var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IGameSessionRepository>();
        var session = await repository.GetByIdAsync(new GameSessionId(sessionId));

        if (session is null)
        {
            throw new InvalidOperationException($"Session '{sessionId}' was not found in the acceptance test store.");
        }

        return session;
    }

    private static GameSession CreateSession(bool armed = false)
    {
        var currentTown = new Town(new TownId("current"), "Current Town");
        var connectedTown = new Town(new TownId("connected"), "Connected Town");
        var world = new World(
            new[] { currentTown, connectedTown },
            new[] { new Trail(new TrailId("trail-1"), currentTown.Id, connectedTown.Id, TrailRisk.Low) });

        var suspects = new[]
        {
            new Suspect(new SuspectId("suspect-1"), "Mira Cline", SuspectTraits.Empty, SuspectStatus.AtLarge),
            new Suspect(new SuspectId("suspect-2"), "Reno Pike", SuspectTraits.Empty, SuspectStatus.AtLarge)
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

        var session = GameSession.StartSetup(
            "Ranger Vale", world, caseFile,
            GameDifficulty.Standard, GameEntropy.Classic, "test-seed", SaltSource.CreateFixed("test"));
        session.ViewPrologue("test-prologue-descriptor");
        session.SelectStartingTown(currentTown.Id);
        var inventory = armed
            ? new Inventory([new InventoryItem(ItemKind.Revolver, 1)])
            : null;
        session.CompleteGameStart(inventory: inventory);
        return session;
    }

    private static GameSession CreateCitizenSession()
    {
        var currentTown = new Town(new TownId("current"), "Current Town");
        var connectedTown = new Town(new TownId("connected"), "Connected Town");
        var world = new World(
            new[] { currentTown, connectedTown },
            new[] { new Trail(new TrailId("trail-1"), currentTown.Id, connectedTown.Id, TrailRisk.Low) });

        var caseFile = new CaseFile(
            accusation: null,
            Array.Empty<Suspect>(),
            trueCulpritId: new SuspectId("suspect-2"),
            openingLead: CaseOpeningLead.Create("Follow the public leads and look for a signature mark."),
            knownClues: Array.Empty<Clue>(),
            knownWarrants: new[]
            {
                new Warrant(
                    new WarrantId("warrant-1"),
                    "Known wanted identity",
                    new WarrantTerms(
                        WarrantDisposition.DeadOrAlive,
                        100m,
                        Array.Empty<string>(),
                        Array.Empty<string>(),
                        "Current Town Sheriff",
                        InvestigationTargetKind.TrueCulprit,
                        Array.Empty<OutlawGangId>(),
                        null),
                    "Wanted for testing.")
            });

        var session = GameSession.StartSetup(
            "Ranger Vale", world, caseFile,
            GameDifficulty.Standard, GameEntropy.Classic, "test-seed", SaltSource.CreateFixed("test"));
        session.ViewPrologue("test-prologue-descriptor");
        session.SelectStartingTown(currentTown.Id);
        session.CompleteGameStart(Wallet.Starting(4m), inventory: null);
        return session;
    }
}
