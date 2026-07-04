using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using WildBunch.Api.Games;
using WildBunch.Application.Games.Models;
using WildBunch.Domain.Actions;
using WildBunch.Domain.Economy;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using WildBunch.GameContent.NewGame;
using Xunit;
using Xunit.Sdk;

namespace WildBunch.Integration.Tests.TestInfrastructure;

internal static class ScenarioSeedCatalog
{
    private static readonly string CanonicalMountedSeedCode = SeedWorldResolver.FormatSeedCode(
        SeedWorldResolver.CreateCanonicalSeedCode());

    // BUNCH-107 transitional: NoHorseLightEasy previously specified no-horse + light loadout
    // via seed-derived fields. These fields are now difficulty-owned (DifficultyEnvelope).
    // All difficulties get horse+saddle+Standard loadout as transitional defaults.
    // This fixture now uses the canonical seed with Easy difficulty + Boring entropy.
    // BUNCH-94 will restore no-horse variety via difficulty-owned envelopes.
    // The shape signature has been updated to reflect transitional defaults (horse=present).
    private static readonly string NoHorseLightEasySeedCode = CanonicalMountedSeedCode;

    private static readonly ScenarioSeedDescriptor CanonicalMountedStandardDescriptor = ScenarioSeedDescriptor.Create("CanonicalMountedStandard")
        .WithCodecVersion(ScenarioSeedCodecVersion.Current)
        .WithEntropy(GameEntropy.Boring)
        .WithStartingTownRole(ScenarioStartingTownRole.DefaultPlayableStart)
        .WithHorse(HorseCondition.Healthy)
        .WithSaddle(SaddleState.Present)
        .WithWallet(25m)
        .WithItemCount(8)
        .WithTownCount(8)
        .WithPreview(ScenarioPreviewExpectation.Mounted(2, 2));

    private static readonly ScenarioSeedDescriptor CanonicalStartingTownServicesDescriptor = ScenarioSeedDescriptor.Create("CanonicalStartingTownServices")
        .WithCodecVersion(ScenarioSeedCodecVersion.Current)
        .WithEntropy(GameEntropy.Boring)
        .WithStartingTownRole(ScenarioStartingTownRole.DefaultPlayableStart)
        .WithHorse(HorseCondition.Healthy)
        .WithSaddle(SaddleState.Present)
        .WithWallet(25m)
        .WithItemCount(8)
        .WithTownCount(8)
        .WithServicesOnStartingTown()
        .WithPreview(ScenarioPreviewExpectation.Mounted(2, 2));

    private static readonly ScenarioSeedDescriptor HighRiskFoeInterruptRouteDescriptor = ScenarioSeedDescriptor.Create("HighRiskFoeInterruptRoute")
        .WithCodecVersion(ScenarioSeedCodecVersion.Current)
        .WithEntropy(GameEntropy.Boring)
        .WithStartingTownRole(ScenarioStartingTownRole.DefaultPlayableStart)
        .WithHorse(HorseCondition.Healthy)
        .WithSaddle(SaddleState.Present)
        .WithWallet(25m)
        .WithItemCount(8)
        .WithTownCount(8)
        .WithConnectedTownCount(2)
        .WithPreview(ScenarioPreviewExpectation.Missing());

    private static readonly ScenarioSeedDescriptor NoHorseLightEasyDescriptor = ScenarioSeedDescriptor.Create("NoHorseLightEasy")
        .WithCodecVersion(ScenarioSeedCodecVersion.Current)
        .WithEntropy(GameEntropy.Boring)
        .WithDifficulty(GameDifficulty.Easy)
        .WithHorse(HorseCondition.Healthy)
        .WithSaddle(SaddleState.Present)
        .WithHealth(1250)
        .WithTownCount(8)
        .WithTravelMode(TravelMode.Mounted)
        .WithPreview(ScenarioPreviewExpectation.Mounted(2, 2));

    public static readonly ScenarioSeedFixture CanonicalMountedStandard = new(
        Name: "CanonicalMountedStandard",
        SeedCode: CanonicalMountedSeedCode,
        GameDifficulty: GameDifficulty.Standard,
        GameEntropy: GameEntropy.Boring,
        Contract: CanonicalMountedStandardDescriptor,
        DescribeShapeSignature: DescribeCanonicalMountedShape,
        AssertCreatedSessionContract: session => AssertCanonicalMountedStartState("CanonicalMountedStandard", session),
        AssertTravelPreviewContract: (session, destinationTownId, preview) => AssertCanonicalMountedTravelPreview("CanonicalMountedStandard", session, destinationTownId, preview));

    public static readonly ScenarioSeedFixture CanonicalStartingTownServices = new(
        Name: "CanonicalStartingTownServices",
        SeedCode: CanonicalMountedStandard.SeedCode,
        GameDifficulty: GameDifficulty.Standard,
        GameEntropy: GameEntropy.Boring,
        Contract: CanonicalStartingTownServicesDescriptor,
        DescribeShapeSignature: DescribeCanonicalStartingTownServicesShape,
        AssertCreatedSessionContract: session =>
        {
            AssertCanonicalMountedStartState("CanonicalStartingTownServices", session);

            RequireEqual("CanonicalStartingTownServices", "start-game.inventory.food.quantity", 4, RequireItem("CanonicalStartingTownServices", session, ItemKind.Food).Quantity);
            RequireEqual("CanonicalStartingTownServices", "start-game.inventory.horseFeed.quantity", 3, RequireItem("CanonicalStartingTownServices", session, ItemKind.HorseFeed).Quantity);
        },
        AssertTravelPreviewContract: (session, destinationTownId, preview) => AssertCanonicalMountedTravelPreview("CanonicalStartingTownServices", session, destinationTownId, preview));

    public static readonly ScenarioSeedFixture HighRiskFoeInterruptRoute = new(
        Name: "HighRiskFoeInterruptRoute",
        SeedCode: CanonicalMountedStandard.SeedCode,
        GameDifficulty: GameDifficulty.Standard,
        GameEntropy: GameEntropy.Boring,
        Contract: HighRiskFoeInterruptRouteDescriptor,
        DescribeShapeSignature: DescribeHighRiskFoeInterruptRouteShape,
        AssertCreatedSessionContract: session => AssertCanonicalMountedStartState("HighRiskFoeInterruptRoute", session));

    // BUNCH-107 transitional: NoHorseLightEasy now gets horse+saddle (transitional defaults).
    // The fixture name is retained for continuity but the shape has changed.
    // BUNCH-94 will restore no-horse variety via difficulty-owned envelopes.
    public static readonly ScenarioSeedFixture NoHorseLightEasy = new(
        Name: "NoHorseLightEasy",
        SeedCode: NoHorseLightEasySeedCode,
        GameDifficulty: GameDifficulty.Easy,
        GameEntropy: GameEntropy.Boring,
        Contract: NoHorseLightEasyDescriptor,
        DescribeShapeSignature: DescribeNoHorseLightEasyShape,
        AssertCreatedSessionContract: session =>
        {
            RequireEqual("NoHorseLightEasy", "start-game.GameDifficulty", GameDifficulty.Easy, session.GameDifficulty);
            RequireEqual("NoHorseLightEasy", "start-game.entropy", GameEntropy.Boring, session.GameEntropy);
            RequireEqual("NoHorseLightEasy", "start-game.health", 1250, session.Player.Health);
            // Transitional: all difficulties now get horse+saddle.
            Require("NoHorseLightEasy", "start-game.inventory.horseItem", session.Inventory.Items.Any(item => item.Kind == ItemKind.Horse), "expected the starting inventory to include a horse (transitional default).");
            Require("NoHorseLightEasy", "start-game.inventory.saddleItem", session.Inventory.Items.Any(item => item.Kind == ItemKind.Saddle), "expected the starting inventory to include a saddle (transitional default).");
        },
        AssertTravelPreviewContract: (session, destinationTownId, preview) =>
        {
            RequireEqual("NoHorseLightEasy", "travel-preview.success", true, preview.Success);
            RequireEqual("NoHorseLightEasy", "travel-preview.destinationTownId", destinationTownId, preview.Preview?.DestinationTownId);
            // Transitional: mounted travel is now available for all difficulties.
            RequireEqual("NoHorseLightEasy", "travel-preview.travelMode", TravelMode.Mounted, preview.Preview?.TravelMode);
            RequireEqual("NoHorseLightEasy", "travel-preview.mountedTravelAvailable", true, preview.Preview?.MountedTravelAvailable);
        },
        AssertTravelTurnContract: (session, destinationTownId, preview, turn) =>
        {
            RequireEqual("NoHorseLightEasy", "travel-turn.success", true, turn.Success);
            RequireEqual("NoHorseLightEasy", "travel-turn.destinationTownId", destinationTownId, turn.CurrentSession.Journey?.DestinationTownId);
            RequireEqual("NoHorseLightEasy", "travel-turn.travelMode", TravelMode.Mounted, turn.CurrentSession.Journey?.TravelMode);
            RequireEqual("NoHorseLightEasy", "travel-turn.baselineRideDays", preview.Preview?.BaselineRideDays, turn.CurrentSession.Journey?.BaselineRideDays);
            RequireEqual("NoHorseLightEasy", "travel-turn.expectedDays", preview.Preview?.ExpectedDays, turn.CurrentSession.Journey?.ExpectedDays);
            RequireEqual("NoHorseLightEasy", "travel-turn.daysTravelled", 0, turn.CurrentSession.Journey?.DaysTravelled);
        });

    public static IReadOnlyList<ScenarioSeedFixture> All { get; } =
        new[]
        {
            CanonicalMountedStandard,
            CanonicalStartingTownServices,
            HighRiskFoeInterruptRoute,
            NoHorseLightEasy
        };

    public static void AssertCatalogContractsCurrent()
    {
        foreach (var fixture in All)
        {
            fixture.AssertCachedFixtureContract();
        }
    }

    public static async Task AssertStartingTownServices(this ScenarioSeedFixture fixture, HttpClient client, Guid gameId, GameSessionDto session)
    {
        RequireEqual("CanonicalStartingTownServices", "scenario.name", "CanonicalStartingTownServices", fixture.Name);

        fixture.AssertCreatedSession(session);
        AssertStartingTownConnectedTownAssumptions(session);

        var actionsResponse = await client.GetAsync($"/api/games/{gameId}/actions");
        RequireEqual("CanonicalStartingTownServices", "actions.statusCode", HttpStatusCode.OK, actionsResponse.StatusCode);

        var actions = await actionsResponse.Content.ReadFromJsonAsync<AvailableActionDto[]>();
        Require("CanonicalStartingTownServices", "actions.payload", actions is not null, "expected available actions to deserialize.");
        AssertStartingTownActionAvailability(actions!);

        var storeOffersResponse = await client.GetAsync($"/api/games/{gameId}/towns/{session.Player.CurrentTownId}/store-offers");
        RequireEqual("CanonicalStartingTownServices", "store-offers.statusCode", HttpStatusCode.OK, storeOffersResponse.StatusCode);

        var storeOffers = await storeOffersResponse.Content.ReadFromJsonAsync<TownStoreOffersDto>();
        Require("CanonicalStartingTownServices", "store-offers.payload", storeOffers is not null, "expected town store offers to deserialize.");
        AssertStartingTownStoreAvailability(storeOffers!, session.Player.CurrentTownId);
    }

    // BUNCH-107 transitional: AssertSecondLegRoute renamed to AssertDryRoute transitively.
    // The route to dryfork is now mounted (transitional horse default).
    // BUNCH-94 will restore no-horse variety via difficulty-owned envelopes.
    public static void AssertSecondLegRoute(this ScenarioSeedFixture fixture, GameSessionDto session, string destinationTownId, TravelPreviewResultDto preview)
    {
        RequireEqual("NoHorseLightEasy", "scenario.name", "NoHorseLightEasy", fixture.Name);

        fixture.AssertTravelPreview(session, destinationTownId, preview);

        // Transitional: travel is now mounted for all difficulties.
        RequireEqual("NoHorseLightEasy", "travel-preview.travelMode", TravelMode.Mounted, preview.Preview?.TravelMode);
        RequireEqual("NoHorseLightEasy", "travel-preview.mountedTravelAvailable", true, preview.Preview?.MountedTravelAvailable);
        RequireEqual("NoHorseLightEasy", "travel-preview.waterSecure", true, preview.Preview?.WaterSecure);
        RequireEqual("NoHorseLightEasy", "travel-preview.routeProfile.waterFeature", WaterFeature.Creek, preview.Preview?.RouteProfile.WaterFeature);
    }

    public static void AssertSecondLegRoute(this ScenarioSeedFixture fixture, GameSessionDto session, string destinationTownId, GameTurnResultDto turn, TravelPreviewResultDto preview)
    {
        RequireEqual("NoHorseLightEasy", "scenario.name", "NoHorseLightEasy", fixture.Name);

        fixture.AssertTravelTurn(session, destinationTownId, turn, preview);

        // Transitional: travel is now mounted for all difficulties.
        RequireEqual("NoHorseLightEasy", "travel-turn.travelMode", TravelMode.Mounted, turn.CurrentSession.Journey?.TravelMode);
        RequireEqual("NoHorseLightEasy", "travel-turn.routeProfile.waterFeature", WaterFeature.Creek, turn.CurrentSession.Journey?.RouteProfile.WaterFeature);
        RequireEqual("NoHorseLightEasy", "travel-turn.waterSecure", true, turn.CurrentSession.Journey?.WaterSecure);
    }

    public static void AssertHighRiskFoeInterruptRoute(
        this ScenarioSeedFixture fixture,
        GameSessionDto session,
        GameTurnResultDto secondLegTravel,
        GameTurnResultDto blockedAdvance,
        GameTurnResultDto resolved,
        GameTurnResultDto resumeAdvance)
    {
        RequireEqual("HighRiskFoeInterruptRoute", "scenario.name", "HighRiskFoeInterruptRoute", fixture.Name);

        fixture.AssertCreatedSession(session);

        // Discover the destination town dynamically from the journey (no hardcoded town names)
        var destinationTownId = secondLegTravel.CurrentSession.Journey?.DestinationTownId
            ?? throw new XunitException("HighRiskFoeInterruptRoute: expected the journey to have a destination town.");
        var destinationTown = session.World.Towns.FirstOrDefault(t => t.Id == destinationTownId)
            ?? throw new XunitException($"HighRiskFoeInterruptRoute: destination town '{destinationTownId}' not found in world.");
        var destinationTownName = destinationTown.Name;

        Require("HighRiskFoeInterruptRoute", "travel-turn.success", secondLegTravel.Success, "expected the journey to start successfully.");
        RequireEqual("HighRiskFoeInterruptRoute", "travel-turn.journeyStatus", JourneyStatus.Active, secondLegTravel.JourneyStatus);
        Require("HighRiskFoeInterruptRoute", "travel-turn.noEncounter", secondLegTravel.Journey is null || secondLegTravel.Journey.PendingEncounter is null, "expected no pending encounter on journey start.");
        RequireEqual("HighRiskFoeInterruptRoute", "travel-turn.daysTravelled", 0, secondLegTravel.CurrentSession.Journey?.DaysTravelled);

        Require("HighRiskFoeInterruptRoute", "travel-turn.blockedAdvance.success", !blockedAdvance.Success, "expected the first advance to interrupt due to encounter.");
        RequireEqual("HighRiskFoeInterruptRoute", "travel-turn.blockedAdvance.journeyStatus", JourneyStatus.Interrupted, blockedAdvance.JourneyStatus);
        Require("HighRiskFoeInterruptRoute", "travel-turn.blockedAdvance.pendingEncounter", blockedAdvance.Journey is not null && blockedAdvance.Journey.PendingEncounter is not null, "expected a pending public encounter.");
        RequireEqual("HighRiskFoeInterruptRoute", "travel-turn.blockedAdvance.pendingEncounter.kind", "npc", blockedAdvance.Journey!.PendingEncounter!.Kind);
        RequireEqual("HighRiskFoeInterruptRoute", "travel-turn.blockedAdvance.pendingEncounter.choices", 3, blockedAdvance.Journey.PendingEncounter.Choices.Count);
        RequireEqual("HighRiskFoeInterruptRoute", "travel-turn.blockedAdvance.pendingEncounter.choiceIds", "run,fight,bribe", string.Join(",", blockedAdvance.Journey.PendingEncounter.Choices.Select(choice => choice.Id)));
        RequireEqual("HighRiskFoeInterruptRoute", "travel-turn.blockedAdvance.clock.day", secondLegTravel.CurrentSession.Clock.Day + 1, blockedAdvance.CurrentSession.Clock.Day);
        RequireEqual("HighRiskFoeInterruptRoute", "travel-turn.blockedAdvance.clock.turn", 0, blockedAdvance.CurrentSession.Clock.Turn);
        Require("HighRiskFoeInterruptRoute", "travel-turn.blockedAdvance.travelDiary", blockedAdvance.TravelDiary is not null && blockedAdvance.TravelDiary.Days.Count == 1, "expected one diary day for the interrupted first day.");

        // Check diary names the destination town (whatever it is)
        var openingNarration = blockedAdvance.TravelDiary!.Days[0].OpeningNarration;
        Require("HighRiskFoeInterruptRoute", "travel-turn.blockedAdvance.openingNarration",
            openingNarration is not null && openingNarration.Contains(destinationTownName, StringComparison.OrdinalIgnoreCase),
            $"expected the diary to name the destination town '{destinationTownName}'.");
        Require("HighRiskFoeInterruptRoute", "travel-turn.blockedAdvance.openingNarration",
            openingNarration is not null && openingNarration.Contains("by mounted travel", StringComparison.OrdinalIgnoreCase),
            "expected the diary to reflect mounted travel before the interruption.");

        Require("HighRiskFoeInterruptRoute", "travel-turn.resolved.success", resolved.Success, "expected the public encounter resolution to succeed.");
        RequireEqual("HighRiskFoeInterruptRoute", "travel-turn.resolved.journeyStatus", JourneyStatus.Active, resolved.JourneyStatus);
        Require("HighRiskFoeInterruptRoute", "travel-turn.resolved.pendingEncounter", resolved.CurrentSession.Journey is not null && resolved.CurrentSession.Journey.PendingEncounter is null, "expected the pending encounter to clear after resolution.");
        RequireEqual("HighRiskFoeInterruptRoute", "travel-turn.resolved.clock.day", blockedAdvance.CurrentSession.Clock.Day, resolved.CurrentSession.Clock.Day);
        RequireEqual("HighRiskFoeInterruptRoute", "travel-turn.resolved.clock.turn", 0, resolved.CurrentSession.Clock.Turn);
        Require("HighRiskFoeInterruptRoute", "travel-turn.resolved.logEntries", resolved.CurrentSession.LogEntries.Count > secondLegTravel.CurrentSession.LogEntries.Count, "expected the resolution to add durable log state.");

        Require("HighRiskFoeInterruptRoute", "travel-turn.resume.journeyRemains", resumeAdvance.CurrentSession.Journey is not null, "expected the journey to remain after resuming.");
        RequireEqual("HighRiskFoeInterruptRoute", "travel-turn.resume.currentTownId", destinationTownId, resumeAdvance.CurrentSession.Player.CurrentTownId);
        RequireEqual("HighRiskFoeInterruptRoute", "travel-turn.resume.clock.day", blockedAdvance.CurrentSession.Clock.Day + 1, resumeAdvance.CurrentSession.Clock.Day);
        RequireEqual("HighRiskFoeInterruptRoute", "travel-turn.resume.clock.turn", 0, resumeAdvance.CurrentSession.Clock.Turn);
    }

    public static SetupGameRequest CreateRequest(this ScenarioSeedFixture fixture, string playerName)
        => new(
            playerName,
            fixture.GameDifficulty,
            fixture.SeedCode,
            fixture.GameEntropy);

    public static void AssertCreatedSession(this ScenarioSeedFixture fixture, GameSessionDto session)
        => fixture.AssertCreatedSessionContract(session);

    public static void AssertTravelPreview(this ScenarioSeedFixture fixture, GameSessionDto session, string destinationTownId, TravelPreviewResultDto preview)
    {
        if (fixture.AssertTravelPreviewContract is null)
        {
            throw new InvalidOperationException($"Scenario '{fixture.Name}' does not define a travel preview contract.");
        }

        fixture.AssertTravelPreviewContract(session, destinationTownId, preview);
    }

    public static void AssertTravelTurn(this ScenarioSeedFixture fixture, GameSessionDto session, string destinationTownId, GameTurnResultDto turn, TravelPreviewResultDto preview)
    {
        if (fixture.AssertTravelTurnContract is null)
        {
            throw new InvalidOperationException($"Scenario '{fixture.Name}' does not define a travel turn contract.");
        }

        fixture.AssertTravelTurnContract(session, destinationTownId, preview, turn);
    }

    private static void AssertCanonicalMountedStartState(string scenarioName, GameSessionDto session)
    {
        RequireEqual(scenarioName, "start-game.GameDifficulty", GameDifficulty.Standard, session.GameDifficulty);
        RequireEqual(scenarioName, "start-game.entropy", GameEntropy.Boring, session.GameEntropy);

        // Starting town is whatever StartingTownPolicy resolved — don't assert on the name.
        // Assert that it's one of the world's towns.
        Require(scenarioName, "start-game.currentTownId.inWorld",
            session.World.Towns.Any(t => t.Id == session.Player.CurrentTownId),
            $"expected current town {session.Player.CurrentTownId} to be in the world");

        RequireEqual(scenarioName, "start-game.health", 1000, session.Player.Health);
        RequireEqual(scenarioName, "start-game.wallet.cash", 25m, session.Inventory.Wallet.Cash);
        Require(scenarioName, "start-game.world.towns", session.World.Towns.Count >= 5 && session.World.Towns.Count <= 10, $"expected town count 5-10, got {session.World.Towns.Count}");
        Require(scenarioName, "start-game.world.trails", session.World.Trails.Count > 0, "expected at least one trail");

        // Graph-property assertions: connected, positive coordinates, 2-6 day distances.
        AssertWorldGraphProperties(scenarioName, session);

        // Case file opening lead is game content — don't assert on its text.
        RequireEqual(scenarioName, "start-game.caseFile.discoveredSuspects", 0, session.CaseFile.DiscoveredSuspects.Count);
        RequireEqual(scenarioName, "start-game.inventory.items.count", 8, session.Inventory.Items.Count);
        Require(scenarioName, "start-game.inventory.horseState", session.Inventory.HorseState is not null, "expected the player to start mounted.");
        Require(scenarioName, "start-game.capabilities.mountedTravelAvailable", session.Inventory.Capabilities.MountedTravelAvailable, "expected mounted travel to be available.");
        Require(scenarioName, "start-game.capabilities.gunfightCapable", session.Inventory.Capabilities.GunfightCapable, "expected gunfight capability to be available.");
        Require(scenarioName, "start-game.capabilities.rifleUsable", !session.Inventory.Capabilities.RifleUsable, "expected rifles to stay unusable at start.");
        Require(scenarioName, "start-game.logEntries", session.LogEntries.Count > 0, "expected the new game log to be populated.");
    }

    private static void AssertWorldGraphProperties(string scenarioName, GameSessionDto session)
    {
        // All towns have positive coordinates (clustered placement, not placeholder zeros)
        foreach (var town in session.World.Towns)
        {
            Require(scenarioName, $"start-game.world.towns.{town.Id}.mapX", town.MapX > 0, $"expected positive MapX for {town.Name}, got {town.MapX}");
            Require(scenarioName, $"start-game.world.towns.{town.Id}.mapY", town.MapY > 0, $"expected positive MapY for {town.Name}, got {town.MapY}");
        }

        // All trails have ride-day distances in 2-8 day range (honest 25px/day scale:
        // 50px min separation → 2 days, 200px top clamp → 8 days, outlier trail = 6 days)
        foreach (var trail in session.World.Trails)
        {
            Require(scenarioName, $"start-game.world.trails.{trail.Id}.rideDayDistance",
                trail.RideDayDistance >= 2m && trail.RideDayDistance <= 8m,
                $"expected ride-day distance 2-8 for trail {trail.Id}, got {trail.RideDayDistance}");
        }

        // Trail graph is connected (BFS from starting town reaches all towns)
        var adjacency = new Dictionary<string, HashSet<string>>();
        foreach (var town in session.World.Towns)
        {
            adjacency[town.Id] = new HashSet<string>();
        }
        foreach (var trail in session.World.Trails)
        {
            adjacency[trail.FromTownId].Add(trail.ToTownId);
            adjacency[trail.ToTownId].Add(trail.FromTownId);
        }
        var visited = new HashSet<string>();
        var queue = new Queue<string>();
        var startTown = session.Player.CurrentTownId;
        queue.Enqueue(startTown);
        visited.Add(startTown);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var neighbor in adjacency[current])
            {
                if (visited.Add(neighbor))
                {
                    queue.Enqueue(neighbor);
                }
            }
        }
        Require(scenarioName, "start-game.world.graph.connected",
            visited.Count == session.World.Towns.Count,
            $"expected all {session.World.Towns.Count} towns reachable from {startTown}, only {visited.Count} reached");

        // Starting town has at least 2 connected towns (not an isolated node)
        var startConnected = adjacency[startTown].Count;
        Require(scenarioName, "start-game.world.graph.startConnected",
            startConnected >= 2,
            $"expected starting town {startTown} to have at least 2 connected towns, got {startConnected}");
    }

    private static void AssertCanonicalMountedTravelPreview(string scenarioName, GameSessionDto session, string destinationTownId, TravelPreviewResultDto preview)
    {
        RequireEqual(scenarioName, "travel-preview.success", true, preview.Success);
        RequireEqual(scenarioName, "travel-preview.destinationTownId", destinationTownId, preview.Preview?.DestinationTownId);
        RequireEqual(scenarioName, "travel-preview.travelMode", TravelMode.Mounted, preview.Preview?.TravelMode);
        RequireEqual(scenarioName, "travel-preview.mountedTravelAvailable", true, preview.Preview?.MountedTravelAvailable);
    }

    private static void AssertStartingTownConnectedTownAssumptions(GameSessionDto session)
    {
        var connectedTownIds = session.World.Trails
            .Where(trail => trail.FromTownId == session.Player.CurrentTownId || trail.ToTownId == session.Player.CurrentTownId)
            .Select(trail => trail.FromTownId == session.Player.CurrentTownId ? trail.ToTownId : trail.FromTownId)
            .Distinct()
            .ToArray();

        Require("CanonicalStartingTownServices", "start-game.connectedTownIds.count",
            connectedTownIds.Length >= 2,
            $"expected at least 2 connected towns from {session.Player.CurrentTownId}, got {connectedTownIds.Length}");
    }

    private static void AssertStartingTownActionAvailability(AvailableActionDto[] actions)
    {
        Require("CanonicalStartingTownServices", "actions.travel", actions.Any(action => action.Kind == AvailableActionKind.Travel), "expected Travel to be available.");
        Require("CanonicalStartingTownServices", "actions.viewMap", actions.Any(action => action.Kind == AvailableActionKind.ViewMap), "expected ViewMap to be available.");
        Require("CanonicalStartingTownServices", "actions.viewJournal", actions.Any(action => action.Kind == AvailableActionKind.ViewJournal), "expected ViewJournal to be available.");
        Require("CanonicalStartingTownServices", "actions.buySupplies", actions.Any(action => action.Kind == AvailableActionKind.BuySupplies), "expected BuySupplies to be available.");
        Require("CanonicalStartingTownServices", "actions.readWantedPosters", actions.Any(action => action.Kind == AvailableActionKind.ReadWantedPosters), "expected ReadWantedPosters to be available.");
        Require("CanonicalStartingTownServices", "actions.inspectNoticeBoard", actions.Any(action => action.Kind == AvailableActionKind.InspectNoticeBoard), "expected InspectNoticeBoard to be available.");
        Require("CanonicalStartingTownServices", "actions.checkLocalRecords", actions.Any(action => action.Kind == AvailableActionKind.CheckSheriffRecords), "expected CheckSheriffRecords to be available.");
        Require("CanonicalStartingTownServices", "actions.gatherLocalGossip", actions.Any(action => action.Kind == AvailableActionKind.GatherLocalGossip), "expected GatherLocalGossip to be available.");
    }

    private static void AssertStartingTownStoreAvailability(TownStoreOffersDto storeOffers, string currentTownId)
    {
        RequireEqual("CanonicalStartingTownServices", "store-offers.available", true, storeOffers.Available);
        RequireEqual("CanonicalStartingTownServices", "store-offers.townId", currentTownId, storeOffers.TownId);
        Require("CanonicalStartingTownServices", "store-offers.generalStore", storeOffers.Offers.Any(offer => offer.VendorType == StoreVendorType.GeneralStore), "expected the starting town to expose a general store.");
        Require("CanonicalStartingTownServices", "store-offers.stable", storeOffers.Offers.Any(offer => offer.VendorType == StoreVendorType.Stable), "expected the starting town to expose a stable.");
    }

    private static string DescribeCanonicalMountedShape(GameSessionDto session, TravelPreviewResultDto? preview)
        => string.Join(
            "|",
            ScenarioSeedCodecVersion.Current.Value,
            "CanonicalMountedStandard",
            $"entropy={session.GameEntropy}",
            "start=default-playable-start",
            $"horse={DescribeHorseState(session.Inventory.HorseState)}",
            $"saddle={DescribePresence(session.Inventory.Items.Any(item => item.Kind == ItemKind.Saddle))}",
            $"wallet={session.Inventory.Wallet.Cash.ToString(CultureInfo.InvariantCulture)}",
            $"items={session.Inventory.Items.Count}",
            $"towns={session.World.Towns.Count}",
            $"preview={DescribeMountedPreview(preview)}");

    private static string DescribeCanonicalStartingTownServicesShape(GameSessionDto session, TravelPreviewResultDto? preview)
        => string.Join(
            "|",
            ScenarioSeedCodecVersion.Current.Value,
            "CanonicalStartingTownServices",
            $"entropy={session.GameEntropy}",
            "start=default-playable-start",
            $"horse={DescribeHorseState(session.Inventory.HorseState)}",
            $"saddle={DescribePresence(session.Inventory.Items.Any(item => item.Kind == ItemKind.Saddle))}",
            $"wallet={session.Inventory.Wallet.Cash.ToString(CultureInfo.InvariantCulture)}",
            $"items={session.Inventory.Items.Count}",
            $"towns={session.World.Towns.Count}",
            "services=starting-town",
            $"preview={DescribeMountedPreview(preview)}");

    private static string DescribeHighRiskFoeInterruptRouteShape(GameSessionDto session, TravelPreviewResultDto? preview)
    {
        var connectedCount = session.World.Trails
            .Where(trail => trail.FromTownId == session.Player.CurrentTownId || trail.ToTownId == session.Player.CurrentTownId)
            .Select(trail => trail.FromTownId == session.Player.CurrentTownId ? trail.ToTownId : trail.FromTownId)
            .Distinct()
            .Count();

        return string.Join(
            "|",
            ScenarioSeedCodecVersion.Current.Value,
            "HighRiskFoeInterruptRoute",
            $"entropy={session.GameEntropy}",
            "start=default-playable-start",
            $"horse={DescribeHorseState(session.Inventory.HorseState)}",
            $"saddle={DescribePresence(session.Inventory.Items.Any(item => item.Kind == ItemKind.Saddle))}",
            $"wallet={session.Inventory.Wallet.Cash.ToString(CultureInfo.InvariantCulture)}",
            $"items={session.Inventory.Items.Count}",
            $"towns={session.World.Towns.Count}",
            $"routes=count={connectedCount}",
            $"preview={DescribeMountedPreview(preview)}");
    }

    private static string DescribeNoHorseLightEasyShape(GameSessionDto session, TravelPreviewResultDto? preview)
        => string.Join(
            "|",
            ScenarioSeedCodecVersion.Current.Value,
            "NoHorseLightEasy",
            $"entropy={session.GameEntropy}",
            $"difficulty={session.GameDifficulty}",
            $"horse={DescribeHorseState(session.Inventory.HorseState)}",
            $"saddle={DescribePresence(session.Inventory.Items.Any(item => item.Kind == ItemKind.Saddle))}",
            $"health={session.Player.Health}",
            $"towns={session.World.Towns.Count}",
            $"travel={preview?.Preview?.TravelMode.ToString().ToLowerInvariant() ?? "missing"}",
            $"preview={DescribeMountedPreview(preview)}");

    private static string DescribeHorseState(HorseTravelStateDto? horseState)
        => horseState is null ? "absent" : horseState.CanProvideMountedTravel ? "healthy" : "degraded";

    private static string DescribePresence(bool present)
        => present ? "present" : "absent";

    private static string DescribeMountedPreview(TravelPreviewResultDto? preview)
        // Encode only the travel mode, not the specific day counts. Day counts
        // depend on map generation (trail distances, town spacing) and change
        // when the map generator is tuned. The travel preview contract validates
        // success and travel mode — those are the stable invariants.
        => preview?.Preview is null
            ? "missing"
            : $"{preview.Preview.TravelMode.ToString().ToLowerInvariant()}";

    private static dynamic RequireItem(string scenarioName, GameSessionDto session, ItemKind kind)
    {
        var item = session.Inventory.Items.SingleOrDefault(entry => entry.Kind == kind);
        Require(scenarioName, $"start-game.inventory.{kind.ToString().ToLowerInvariant()}", item is not null, $"expected the starting inventory to include {kind}.");
        return item!;
    }

    private static void Require(string scenarioName, string contractName, bool condition, string detail)
        => Assert.True(condition, $"Scenario '{scenarioName}' violated contract '{contractName}': {detail}");

    private static void RequireEqual<T>(string scenarioName, string contractName, T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new XunitException($"Scenario '{scenarioName}' violated contract '{contractName}': expected '{expected}', got '{actual}'.");
        }
    }
}
