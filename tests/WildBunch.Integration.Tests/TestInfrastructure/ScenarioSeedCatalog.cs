using System.Collections.Generic;
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

    private static readonly string EasyDifficultyMountedSeedCode = CanonicalMountedSeedCode;

    public static readonly ScenarioSeedFixture CanonicalMountedStandard = new(
        Name: "CanonicalMountedStandard",
        SeedCode: CanonicalMountedSeedCode,
        GameDifficulty: GameDifficulty.Standard,
        GameEntropy: GameEntropy.Boring,
        AssertCreatedSessionContract: session => AssertCanonicalMountedStartState("CanonicalMountedStandard", session),
        AssertTravelPreviewContract: (session, destinationTownId, preview) => AssertCanonicalMountedTravelPreview("CanonicalMountedStandard", session, destinationTownId, preview));

    public static readonly ScenarioSeedFixture CanonicalStartingTown = new(
        Name: "CanonicalStartingTown",
        SeedCode: CanonicalMountedStandard.SeedCode,
        GameDifficulty: GameDifficulty.Standard,
        GameEntropy: GameEntropy.Boring,
        AssertCreatedSessionContract: session =>
        {
            AssertCanonicalMountedStartState("CanonicalStartingTown", session);

            RequireEqual("CanonicalStartingTown", "start-game.inventory.food.quantity", 4, RequireItem("CanonicalStartingTown", session, ItemKind.Food).Quantity);
            RequireEqual("CanonicalStartingTown", "start-game.inventory.horseFeed.quantity", 3, RequireItem("CanonicalStartingTown", session, ItemKind.HorseFeed).Quantity);
        },
        AssertTravelPreviewContract: (session, destinationTownId, preview) => AssertCanonicalMountedTravelPreview("CanonicalStartingTown", session, destinationTownId, preview));

    public static readonly ScenarioSeedFixture HighRiskFoeInterruptRoute = new(
        Name: "HighRiskFoeInterruptRoute",
        SeedCode: CanonicalMountedStandard.SeedCode,
        GameDifficulty: GameDifficulty.Standard,
        GameEntropy: GameEntropy.Boring,
        AssertCreatedSessionContract: session => AssertCanonicalMountedStartState("HighRiskFoeInterruptRoute", session));

    public static readonly ScenarioSeedFixture EasyDifficultyMounted = new(
        Name: "EasyDifficultyMounted",
        SeedCode: EasyDifficultyMountedSeedCode,
        GameDifficulty: GameDifficulty.Easy,
        GameEntropy: GameEntropy.Boring,
        AssertCreatedSessionContract: session =>
        {
            RequireEqual("EasyDifficultyMounted", "start-game.GameDifficulty", GameDifficulty.Easy, session.GameDifficulty);
            RequireEqual("EasyDifficultyMounted", "start-game.entropy", GameEntropy.Boring, session.GameEntropy);
            RequireEqual("EasyDifficultyMounted", "start-game.health", 1250, session.Player.Health);
            Require("EasyDifficultyMounted", "start-game.inventory.horseItem", session.Inventory!.Items.Any(item => item.Kind == ItemKind.Horse), "expected the starting inventory to include a horse.");
            Require("EasyDifficultyMounted", "start-game.inventory.saddleItem", session.Inventory!.Items.Any(item => item.Kind == ItemKind.Saddle), "expected the starting inventory to include a saddle.");
        },
        AssertTravelPreviewContract: (session, destinationTownId, preview) =>
        {
            RequireEqual("EasyDifficultyMounted", "travel-preview.success", true, preview.Success);
            RequireEqual("EasyDifficultyMounted", "travel-preview.destinationTownId", destinationTownId, preview.Preview?.DestinationTownId);
            RequireEqual("EasyDifficultyMounted", "travel-preview.travelMode", TravelMode.Mounted, preview.Preview?.TravelMode);
            RequireEqual("EasyDifficultyMounted", "travel-preview.mountedTravelAvailable", true, preview.Preview?.MountedTravelAvailable);
        },
        AssertTravelTurnContract: (session, destinationTownId, preview, turn) =>
        {
            RequireEqual("EasyDifficultyMounted", "travel-turn.success", true, turn.Success);
            RequireEqual("EasyDifficultyMounted", "travel-turn.destinationTownId", destinationTownId, turn.CurrentSession.Journey?.DestinationTownId);
            RequireEqual("EasyDifficultyMounted", "travel-turn.travelMode", TravelMode.Mounted, turn.CurrentSession.Journey?.TravelMode);
            RequireEqual("EasyDifficultyMounted", "travel-turn.baselineRideDays", preview.Preview?.BaselineRideDays, turn.CurrentSession.Journey?.BaselineRideDays);
            RequireEqual("EasyDifficultyMounted", "travel-turn.expectedDays", preview.Preview?.ExpectedDays, turn.CurrentSession.Journey?.ExpectedDays);
            RequireEqual("EasyDifficultyMounted", "travel-turn.daysTravelled", 0, turn.CurrentSession.Journey?.DaysTravelled);
        });

    public static async Task AssertStartingTownReady(this ScenarioSeedFixture fixture, HttpClient client, Guid gameId, GameSessionDto session)
    {
        fixture.AssertCreatedSession(session);
        AssertStartingTownConnectedTownAssumptions(session);

        var actionsResponse = await client.GetAsync($"/api/games/{gameId}/actions");
        RequireEqual("CanonicalStartingTown", "actions.statusCode", HttpStatusCode.OK, actionsResponse.StatusCode);

        var actions = await actionsResponse.Content.ReadFromJsonAsync<AvailableActionDto[]>();
        Require("CanonicalStartingTown", "actions.payload", actions is not null, "expected available actions to deserialize.");
        AssertStartingTownActionAvailability(actions!);

        var storeOffersResponse = await client.GetAsync($"/api/games/{gameId}/towns/{session.Player.CurrentTownId}/store-offers");
        RequireEqual("CanonicalStartingTown", "store-offers.statusCode", HttpStatusCode.OK, storeOffersResponse.StatusCode);

        var storeOffers = await storeOffersResponse.Content.ReadFromJsonAsync<TownStoreOffersDto>();
        Require("CanonicalStartingTown", "store-offers.payload", storeOffers is not null, "expected town store offers to deserialize.");
        AssertStartingTownStoreAvailability(storeOffers!, session.Player.CurrentTownId!);
    }

    public static void AssertSecondLegRoute(this ScenarioSeedFixture fixture, GameSessionDto session, string destinationTownId, TravelPreviewResultDto preview)
    {
        fixture.AssertTravelPreview(session, destinationTownId, preview);

        RequireEqual("EasyDifficultyMounted", "travel-preview.travelMode", TravelMode.Mounted, preview.Preview?.TravelMode);
        RequireEqual("EasyDifficultyMounted", "travel-preview.mountedTravelAvailable", true, preview.Preview?.MountedTravelAvailable);
        RequireEqual("EasyDifficultyMounted", "travel-preview.waterSecure", true, preview.Preview?.WaterSecure);
        RequireEqual("EasyDifficultyMounted", "travel-preview.routeProfile.waterFeature", WaterFeature.Creek, preview.Preview?.RouteProfile.WaterFeature);
    }

    public static void AssertSecondLegRoute(this ScenarioSeedFixture fixture, GameSessionDto session, string destinationTownId, GameTurnResultDto turn, TravelPreviewResultDto preview)
    {
        fixture.AssertTravelTurn(session, destinationTownId, turn, preview);

        RequireEqual("EasyDifficultyMounted", "travel-turn.travelMode", TravelMode.Mounted, turn.CurrentSession.Journey?.TravelMode);
        RequireEqual("EasyDifficultyMounted", "travel-turn.routeProfile.waterFeature", WaterFeature.Creek, turn.CurrentSession.Journey?.RouteProfile.WaterFeature);
        RequireEqual("EasyDifficultyMounted", "travel-turn.waterSecure", true, turn.CurrentSession.Journey?.WaterSecure);
    }

    public static void AssertHighRiskFoeInterruptRoute(
        this ScenarioSeedFixture fixture,
        GameSessionDto session,
        GameTurnResultDto secondLegTravel,
        GameTurnResultDto blockedAdvance,
        GameTurnResultDto resolved,
        GameTurnResultDto resumeAdvance,
        GameTurnResultDto completedSecondLeg)
    {
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
        RequireEqual("HighRiskFoeInterruptRoute", "travel-turn.blockedAdvance.pendingEncounter.kind", "foe", blockedAdvance.Journey!.PendingEncounter!.Kind);
        RequireEqual("HighRiskFoeInterruptRoute", "travel-turn.blockedAdvance.pendingEncounter.choices", 3, blockedAdvance.Journey.PendingEncounter.Choices.Count);
        RequireEqual("HighRiskFoeInterruptRoute", "travel-turn.blockedAdvance.pendingEncounter.choiceIds", "run,fight,bribe", string.Join(",", blockedAdvance.Journey.PendingEncounter.Choices.Select(choice => choice.Id)));
        RequireEqual("HighRiskFoeInterruptRoute", "travel-turn.blockedAdvance.clock.day", secondLegTravel.CurrentSession.Clock.Day + 1, blockedAdvance.CurrentSession.Clock.Day);
        RequireEqual("HighRiskFoeInterruptRoute", "travel-turn.blockedAdvance.clock.turn", 0, blockedAdvance.CurrentSession.Clock.Turn);
        Require("HighRiskFoeInterruptRoute", "travel-turn.blockedAdvance.travelDiary", blockedAdvance.CurrentSession.TravelDiary is not null && blockedAdvance.CurrentSession.TravelDiary.Days.Count == 1, "expected one diary day for the interrupted first day.");

        // Check diary names the destination town (whatever it is)
        var departure = blockedAdvance.CurrentSession.TravelDiary!.JourneyEntries.SingleOrDefault(
            entry => entry.Message.Contains($"toward {destinationTownName}", StringComparison.OrdinalIgnoreCase));
        Require("HighRiskFoeInterruptRoute", "travel-turn.blockedAdvance.departure",
            departure is not null,
            $"expected the diary to name the destination town '{destinationTownName}'.");
        Require("HighRiskFoeInterruptRoute", "travel-turn.blockedAdvance.departure",
            departure is not null && departure.Message.Contains("by mounted travel", StringComparison.OrdinalIgnoreCase),
            "expected the diary to reflect mounted travel before the interruption.");

        Require("HighRiskFoeInterruptRoute", "travel-turn.resolved.success", resolved.Success, "expected the public encounter resolution to succeed.");
        RequireEqual("HighRiskFoeInterruptRoute", "travel-turn.resolved.journeyStatus", JourneyStatus.Active, resolved.JourneyStatus);
        Require("HighRiskFoeInterruptRoute", "travel-turn.resolved.pendingEncounter", resolved.CurrentSession.Journey is not null && resolved.CurrentSession.Journey.PendingEncounter is null, "expected the pending encounter to clear after resolution.");
        RequireEqual("HighRiskFoeInterruptRoute", "travel-turn.resolved.clock.day", blockedAdvance.CurrentSession.Clock.Day, resolved.CurrentSession.Clock.Day);
        RequireEqual("HighRiskFoeInterruptRoute", "travel-turn.resolved.clock.turn", 0, resolved.CurrentSession.Clock.Turn);
        Require("HighRiskFoeInterruptRoute", "travel-turn.resolved.logEntries", resolved.CurrentSession.LogEntries.Count > secondLegTravel.CurrentSession.LogEntries.Count, "expected the resolution to add durable log state.");

        Require("HighRiskFoeInterruptRoute", "travel-turn.resume.journeyRemains", resumeAdvance.CurrentSession.Journey is not null, "expected the journey to remain after resuming.");
        RequireEqual("HighRiskFoeInterruptRoute", "travel-turn.resume.journeyStatus", JourneyStatus.Active, resumeAdvance.JourneyStatus);
        RequireEqual("HighRiskFoeInterruptRoute", "travel-turn.resume.currentTownId", resumeAdvance.CurrentSession.Journey?.OriginTownId, resumeAdvance.CurrentSession.Player.CurrentTownId);
        Require("HighRiskFoeInterruptRoute", "travel-turn.resume.remainingDistance", resumeAdvance.CurrentSession.Journey?.RemainingRideDayDistance > 0, "expected the route to remain incomplete while distance remains.");
        RequireEqual("HighRiskFoeInterruptRoute", "travel-turn.resume.clock.day", blockedAdvance.CurrentSession.Clock.Day + 1, resumeAdvance.CurrentSession.Clock.Day);
        RequireEqual("HighRiskFoeInterruptRoute", "travel-turn.resume.clock.turn", 0, resumeAdvance.CurrentSession.Clock.Turn);
        RequireEqual("HighRiskFoeInterruptRoute", "travel-turn.arrival.journeyStatus", JourneyStatus.Completed, completedSecondLeg.JourneyStatus);
        RequireEqual("HighRiskFoeInterruptRoute", "travel-turn.arrival.currentTownId", destinationTownId, completedSecondLeg.CurrentSession.Player.CurrentTownId);
        Require("HighRiskFoeInterruptRoute", "travel-turn.arrival.afterResume", completedSecondLeg.CurrentSession.Clock.Day > resumeAdvance.CurrentSession.Clock.Day, "expected arrival only after additional route progress.");
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
        RequireEqual(scenarioName, "start-game.wallet.cash", 25m, session.Inventory!.Wallet.Cash);
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
        var startTown = session.Player.CurrentTownId!;
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

        Require("CanonicalStartingTown", "start-game.connectedTownIds.count",
            connectedTownIds.Length >= 2,
            $"expected at least 2 connected towns from {session.Player.CurrentTownId}, got {connectedTownIds.Length}");
    }

    private static void AssertStartingTownActionAvailability(AvailableActionDto[] actions)
    {
        Require("CanonicalStartingTown", "actions.travel", actions.Any(action => action.Kind == AvailableActionKind.Travel), "expected Travel to be available.");
        Require("CanonicalStartingTown", "actions.viewMap", actions.Any(action => action.Kind == AvailableActionKind.ViewMap), "expected ViewMap to be available.");
        Require("CanonicalStartingTown", "actions.viewJournal", actions.Any(action => action.Kind == AvailableActionKind.ViewJournal), "expected ViewJournal to be available.");
        Require("CanonicalStartingTown", "actions.buySupplies", actions.Any(action => action.Kind == AvailableActionKind.BuySupplies), "expected BuySupplies to be available.");
        Require("CanonicalStartingTown", "actions.readWantedPosters", actions.Any(action => action.Kind == AvailableActionKind.ReadWantedPosters), "expected ReadWantedPosters to be available.");
        Require("CanonicalStartingTown", "actions.gatherLocalGossip", actions.Any(action => action.Kind == AvailableActionKind.GatherLocalGossip), "expected GatherLocalGossip to be available.");
    }

    private static void AssertStartingTownStoreAvailability(TownStoreOffersDto storeOffers, string currentTownId)
    {
        RequireEqual("CanonicalStartingTown", "store-offers.townId", currentTownId, storeOffers.TownId);
        Require("CanonicalStartingTown", "store-offers.goods", storeOffers.Offers.Any(offer => offer.ItemKind == WildBunch.Domain.Inventory.ItemKind.Food), "expected the starting town Store to offer food.");
        RequireEqual("CanonicalStartingTown", "store-offers.unique-items", storeOffers.Offers.Count, storeOffers.Offers.Select(offer => offer.ItemKind).Distinct().Count());
    }

    private static dynamic RequireItem(string scenarioName, GameSessionDto session, ItemKind kind)
    {
        var item = session.Inventory!.Items.SingleOrDefault(entry => entry.Kind == kind);
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
