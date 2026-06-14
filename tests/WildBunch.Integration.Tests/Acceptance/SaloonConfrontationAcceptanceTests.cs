using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using WildBunch.Application.Abstractions;
using WildBunch.Application.Games.Mapping;
using WildBunch.Application.Games.Models;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using WildBunch.Integration.Tests.TestInfrastructure;

namespace WildBunch.Integration.Tests.Acceptance;

public sealed class SaloonConfrontationAcceptanceTests
{
    [Fact]
    public async Task PostSaloonLookAroundThenConfrontFleesTheSurfacedWantedSuspect()
    {
        using var factory = new PostgreSqlApiFactory();
        using var client = factory.CreateAuthenticatedClient();

        var createdSession = await SeedSessionWithAvailableSaloonSuspectAsync(factory);

        var lookAroundResponse = await client.PostAsync($"/api/games/{createdSession.Id}/investigations/saloon/look-around", content: null);

        Assert.Equal(HttpStatusCode.OK, lookAroundResponse.StatusCode);

        var surfacedSession = await LoadDomainSessionAsync(factory, createdSession.Id);
        Assert.Equal(new SuspectId("suspect-1"), surfacedSession.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestId);

        var confrontationResponse = await client.PostAsync($"/api/games/{createdSession.Id}/investigations/saloon/confront", content: null);

        Assert.Equal(HttpStatusCode.OK, confrontationResponse.StatusCode);

        var result = await confrontationResponse.Content.ReadFromJsonAsync<WantedSuspectConfrontationResultDto>();
        Assert.NotNull(result);
        Assert.True(result!.Success);
        Assert.Equal("Mira Cline", result.TargetName);
        Assert.Equal(WantedSuspectConfrontationOutcome.Fled, result.Outcome);
        Assert.True(result.IsAlive);
        Assert.False(result.IsSecured);
        Assert.True(result.SessionChanged);
        Assert.Null(result.CurrentSession.ActiveSaloonWantedSuspect);

        var reloadedSession = await LoadDomainSessionAsync(factory, createdSession.Id);
        Assert.Equal(WantedSuspectPresenceState.GoneToGround, reloadedSession.GetWantedSuspectPresenceState(new SuspectId("suspect-1")));
        Assert.Null(reloadedSession.CurrentTownVisit.CurrentTownState.ActiveSaloonPersonOfInterestId);
        Assert.Single(reloadedSession.CaseFile.WantedSuspectConfrontations);
    }

    private static async Task<GameSessionDto> SeedSessionWithAvailableSaloonSuspectAsync(PostgreSqlApiFactory factory)
    {
        var session = CreateSession();
        session.SetWantedSuspectPresenceState(new SuspectId("suspect-1"), WantedSuspectPresenceState.AvailableInTown);

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

    private static GameSession CreateSession()
    {
        var currentTown = new Town(new TownId("current"), "Current Town", TownServices.NoticeBoard);
        var connectedTown = new Town(new TownId("connected"), "Connected Town", TownServices.None);
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

        return GameSession.StartNew("Ranger Vale", world, caseFile, currentTown.Id);
    }
}
