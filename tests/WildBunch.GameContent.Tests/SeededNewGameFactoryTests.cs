using WildBunch.GameContent.NewGame;
using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;

namespace WildBunch.GameContent.Tests;

public sealed class SeededNewGameFactoryTests
{
    [Fact]
    public void CreatesRicherSeedWorldAndCase()
    {
        var factory = new SeededNewGameFactory();

        var session = factory.Create("Ranger Vale");

        Assert.Equal("Ranger Vale", session.Player.Name);
        Assert.Equal(new WildBunch.Domain.World.TownId("pinecross"), session.Player.CurrentTownId);
        Assert.Equal(WildBunch.Domain.Travel.TravelDifficulty.Normal, session.TravelDifficulty);
        Assert.Equal(25m, session.Player.Wallet.Cash);
        Assert.Equal(8, session.Player.Inventory.Items.Count);
        Assert.Equal(HorseTravelState.Healthy, session.Player.Inventory.GetHorseState());
        var capabilities = new InventoryCapabilityResolver().Resolve(session.Player.Inventory);
        Assert.True(capabilities.MountedTravelAvailable);
        Assert.True(capabilities.HorseUpkeepRequired);
        Assert.True(capabilities.NormalRouteWaterSecure);
        Assert.True(capabilities.TrailUtility);
        Assert.True(capabilities.CloseThreatAvailable);
        Assert.True(capabilities.FirearmThreatAvailable);
        Assert.True(capabilities.GunfightCapable);
        Assert.True(capabilities.RevolverUsable);
        Assert.False(capabilities.RifleUsable);
        Assert.Equal(6, session.World.Towns.Count);
        Assert.Equal(7, session.World.Trails.Count);
        Assert.Contains(session.World.Trails, trail => trail.Connects(new WildBunch.Domain.World.TownId("pinecross"), new WildBunch.Domain.World.TownId("redmesa")));
        Assert.DoesNotContain(session.World.Trails, trail => trail.Connects(new WildBunch.Domain.World.TownId("pinecross"), new WildBunch.Domain.World.TownId("dryfork")));
        Assert.Equal(7, session.CaseFile.Suspects.Count);
        Assert.Single(session.CaseFile.Suspects, suspect => suspect.Id.Equals(session.CaseFile.TrueCulpritId));
        Assert.Equal(5, session.CaseFile.KillerReleaseThreshold);
        Assert.Equal("The culprit has a scar on his left cheek.", session.CaseFile.OpeningLead.Description);
        Assert.False(session.CaseFile.KillerReleaseState.IsReleased);
        Assert.Equal(0, session.CaseFile.KillerReleaseState.Progress);
        Assert.Equal(5, session.CaseFile.KillerReleaseState.RequiredPublicClues);
        Assert.Equal(new[]
        {
            "Butch Cassidy",
            "Sundance Kid",
            "Elzy Lay",
            "Kid Curry",
            "Laura Bullion",
            "Bill Doolin",
            "Roy Daugherty"
        }, session.CaseFile.Suspects.Select(suspect => suspect.Name).ToArray());
        Assert.Contains(session.CaseFile.Suspects[0].Profile.Aliases, alias => alias.Name == "Grey Jay");
        Assert.NotEmpty(session.CaseFile.Suspects[3].Profile.IdentifyingFacts);
        var culpritOpeningFeature = Assert.Single(CaseSuspectFeaturePool.FeaturePool, feature => feature.OpeningLeadText == session.CaseFile.OpeningLead.Description);
        Assert.True(culpritOpeningFeature.HasTag(CaseSuspectFeatureTags.OpeningLeadCapable));
        Assert.True(culpritOpeningFeature.HasTag(CaseSuspectFeatureTags.ClassicNod));
        Assert.Equal(culpritOpeningFeature.Description, session.CaseFile.Suspects[3].Profile.IdentifyingFacts[0].Description);
        Assert.All(session.CaseFile.Suspects[3].Profile.IdentifyingFacts, fact => Assert.Contains(CaseSuspectFeaturePool.FeaturePool, feature => feature.Description == fact.Description));
        Assert.Contains(session.CaseFile.KnownClues, clue =>
            clue.Kind == ClueKind.CulpritTrail
            && clue.TargetKind == InvestigationTargetKind.TrueCulprit
            && clue.Description == session.CaseFile.OpeningLead.Description);
        Assert.Single(session.CaseFile.KnownClues);
        Assert.Equal(6, session.CaseFile.PublicClues.Count);
        Assert.Contains(session.CaseFile.PublicClues, clue => clue.Description.StartsWith("A witness tied the rider to ", StringComparison.Ordinal));
        Assert.Contains(session.CaseFile.PublicClues, clue => clue.Description.StartsWith("Boot prints and a waystation note place the rider on the Red Mesa road after dusk.", StringComparison.Ordinal));
        Assert.Equal(new[] { new SuspectId("suspect-1") }, session.CaseFile.PublicClues[0].LinkedSuspectIds);
        Assert.Equal(new[] { new SuspectId("suspect-2") }, session.CaseFile.PublicClues[1].LinkedSuspectIds);
        Assert.Contains(session.CaseFile.PublicClues, clue => clue.SourceKind == InvestigationSourceKind.TelegraphLead);
        Assert.Contains(session.CaseFile.PublicClues, clue => clue.SourceKind == InvestigationSourceKind.LocalGossip);
        Assert.Contains(session.CaseFile.PublicClues, clue => clue.Description.StartsWith("A poster links the alias ", StringComparison.Ordinal));
        Assert.Contains(session.CaseFile.PublicClues, clue => clue.Description.StartsWith("A public notice describes an unnamed rider who ", StringComparison.Ordinal));
        Assert.Contains(session.CaseFile.PublicClues, clue => clue.Description.StartsWith("A telegraph clerk filed the alias ", StringComparison.Ordinal));
        Assert.Contains(
            session.CaseFile.PublicClues,
            clue => clue.Description.StartsWith("Local gossip out of ", StringComparison.Ordinal)
                && clue.Description.Contains(" a rider who ", StringComparison.OrdinalIgnoreCase)
                && clue.Description.Contains("kept to the rail spur after dark", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(session.CaseFile.KnownClues, clue => clue.Description.StartsWith("A witness tied the rider to ", StringComparison.Ordinal));
        Assert.DoesNotContain(session.CaseFile.KnownClues, clue => clue.Description.StartsWith("Boot prints and a waystation note place the rider on the Red Mesa road after dusk.", StringComparison.Ordinal));
        Assert.All(session.CaseFile.KnownClues.Concat(session.CaseFile.PublicClues), clue => Assert.True(clue.Anchors.HasAnchors));
        Assert.All(
            session.CaseFile.KnownClues.Concat(session.CaseFile.PublicClues),
            clue =>
            {
                Assert.DoesNotContain("mentions has", clue.Description, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("describes is missing", clue.Description, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("tied the rider to Has", clue.Description, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("..", clue.Description, StringComparison.Ordinal);
            });
        var sightingClue = Assert.Single(session.CaseFile.PublicClues, clue => clue.Description.StartsWith("Local gossip out of ", StringComparison.Ordinal));
        var sightingSuspectId = Assert.Single(sightingClue.LinkedSuspectIds);
        var sightingTown = session.World.GetTown(
            Assert.Single(session.CaseFile.SuspectTurfAssignments, assignment => assignment.SuspectId.Equals(sightingSuspectId)).TurfTownId);
        Assert.NotEmpty(sightingClue.Anchors.Locations);
        Assert.Equal(sightingTown.Name, sightingClue.Anchors.Locations[0].Label);
        Assert.Equal("rail spur", sightingClue.Anchors.Locations[0].Route);
        Assert.NotEmpty(sightingClue.Anchors.Times);
        Assert.Equal(ClueRecency.Recent, sightingClue.Anchors.Times[0].Recency);
        Assert.NotEmpty(sightingClue.Anchors.Directions);
        Assert.Contains("rail spur", sightingClue.Anchors.Directions[0].Movement, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, session.CaseFile.PublicWarrants.Count);
        Assert.Equal("Kid Curry", session.CaseFile.PublicWarrants[0].TargetName);
        Assert.Equal(WarrantDisposition.DeadOrAlive, session.CaseFile.PublicWarrants[0].Terms.Disposition);
        Assert.Equal(2500m, session.CaseFile.PublicWarrants[0].Terms.BountyAmount);
        Assert.Contains("Red Wren", session.CaseFile.PublicWarrants[0].Terms.KnownAliases);
        Assert.Equal(InvestigationTargetKind.TrueCulprit, session.CaseFile.PublicWarrants[0].Terms.TargetKind);
        Assert.Equal(new[] { OutlawGangIds.WildBunch }, session.CaseFile.PublicWarrants[0].Terms.GangAffiliations);
        Assert.Equal(OutlawGangIds.WildBunch, session.CaseFile.PublicWarrants[0].Terms.AdvancesGangPressureFor);
        Assert.Equal("Reno Pike", session.CaseFile.PublicWarrants[1].TargetName);
        Assert.Equal(WarrantDisposition.AliveOnly, session.CaseFile.PublicWarrants[1].Terms.Disposition);
        Assert.Empty(session.CaseFile.PublicWarrants[1].Terms.GangAffiliations);
        Assert.Null(session.CaseFile.PublicWarrants[1].Terms.AdvancesGangPressureFor);
        Assert.Equal(new SuspectId("suspect-2"), session.CaseFile.Accusation);
        Assert.Equal(7, session.CaseFile.SuspectTurfAssignments.Count);
        Assert.All(session.CaseFile.SuspectTurfAssignments, assignment => Assert.Contains(session.World.Towns, town => town.Id.Equals(assignment.TurfTownId)));
        Assert.All(session.CaseFile.SuspectTurfAssignments, assignment => Assert.Contains(session.CaseFile.Suspects, suspect => suspect.Id.Equals(assignment.SuspectId)));
    }

    [Fact]
    public void SameSeedKeepsTheRosterStableWhileDifferentEntropyCanChangeIt()
    {
        var factory = new SeededNewGameFactory();

        var seedA = CreateSeedCode(11);
        var seedASame = CreateSeedCode(11);
        var seedB = FindVaryingSeed(seedA, factory);

        var first = factory.Create("Ranger Vale", TravelDifficulty.Normal, seedA);
        var firstAgain = factory.Create("Ranger Vale", TravelDifficulty.Normal, seedASame);
        var second = factory.Create("Ranger Vale", TravelDifficulty.Normal, seedB);

        Assert.Equal(RosterSignature(first), RosterSignature(firstAgain));
        Assert.Equal(WarrantSignature(first), WarrantSignature(firstAgain));
        Assert.Equal(TurfSignature(first), TurfSignature(firstAgain));
        Assert.True(
            RosterSignature(first) != RosterSignature(second)
            || WarrantSignature(first) != WarrantSignature(second),
            "Different entropy should change at least one roster or warrant surface.");
    }

    [Fact]
    public void RandomizedNoHorseLightLoadoutSeedCreatesNoHorseOrSaddle()
    {
        var options = new GameSetupOptionsV1(false, StartingLoadoutProfile.Light);
        var seed = GameSetupSeedCodec.GenerateRandom(options, TravelDifficulty.Easy);
        var seedCode = GameSetupSeedCodec.Encode(seed);
        var factory = new SeededNewGameFactory();

        var session = factory.Create("Ranger Vale", TravelDifficulty.Normal, seedCode);

        Assert.Equal(TravelDifficulty.Easy, session.TravelDifficulty);
        Assert.Null(session.Player.Inventory.GetHorseState());
        Assert.DoesNotContain(session.Player.Inventory.Items, item => item.Kind == ItemKind.Horse);
        Assert.DoesNotContain(session.Player.Inventory.Items, item => item.Kind == ItemKind.Saddle);
        Assert.Equal(3, session.Player.Inventory.GetQuantity(ItemKind.Food));
        Assert.Equal(2, session.Player.Inventory.GetQuantity(ItemKind.HorseFeed));
        Assert.Equal(4, session.Player.Inventory.GetQuantity(ItemKind.RevolverAmmo));
    }

    [Fact]
    public void DefaultJourneyRandomnessStaysRuntimeSaltedAndDeterministicSetupOptionCanOptOut()
    {
        var factory = new SeededNewGameFactory();

        var runtimeFirst = factory.Create("Ranger Vale");
        var runtimeSecond = factory.Create("Ranger Vale");

        Assert.Equal(TravelRandomnessMode.RuntimeSalted, runtimeFirst.TravelRandomness.Mode);
        Assert.Equal(TravelRandomnessMode.RuntimeSalted, runtimeSecond.TravelRandomness.Mode);
        Assert.NotEqual(runtimeFirst.TravelRandomness.Salt, runtimeSecond.TravelRandomness.Salt);

        var deterministicSeed = GameSetupSeedCodec.Encode(
            GameSetupSeedCodec.WithOption(
                GameSetupSeedCodec.WithDifficulty(GameSetupSeedCodec.CreateCanonicalSeed(), TravelDifficulty.Normal),
                GameSetupOption.JourneyRandomness,
                1));

        var deterministicFirst = factory.Create("Ranger Vale", setupSeedCode: deterministicSeed);
        var deterministicSecond = factory.Create("Ranger Vale", setupSeedCode: deterministicSeed);

        Assert.Equal(TravelRandomnessMode.Deterministic, deterministicFirst.TravelRandomness.Mode);
        Assert.Equal(TravelRandomnessMode.Deterministic, deterministicSecond.TravelRandomness.Mode);
        Assert.Equal(deterministicFirst.TravelRandomness.Salt, deterministicSecond.TravelRandomness.Salt);
    }

    private static string CreateSeedCode(ulong entropy)
        => GameSetupSeedCodec.Encode(
            new GameSetupSeed(
                GameSetupSeedCodec.CurrentGeneratorVersion,
                TravelDifficulty.Normal,
                GameSetupOptionsV1.Default,
                entropy));

    private static string FindVaryingSeed(string baselineSeedCode, SeededNewGameFactory factory)
    {
        var baseline = factory.Create("Ranger Vale", TravelDifficulty.Normal, baselineSeedCode);
        var baselineRoster = RosterSignature(baseline);
        var baselineWarrants = WarrantSignature(baseline);

        for (ulong entropy = 12; entropy < 200; entropy++)
        {
            var candidateSeed = CreateSeedCode(entropy);
            var candidate = factory.Create("Ranger Vale", TravelDifficulty.Normal, candidateSeed);

            if (RosterSignature(candidate) != baselineRoster || WarrantSignature(candidate) != baselineWarrants)
            {
                return candidateSeed;
            }
        }

        throw new InvalidOperationException("Could not find a noncanonical seed that changed the roster or warrant selection.");
    }

    private static string RosterSignature(WildBunch.Domain.Game.GameSession session)
        => string.Join("|", session.CaseFile.Suspects.Select(suspect => $"{suspect.Id.Value}:{suspect.Name}"));

    private static string WarrantSignature(WildBunch.Domain.Game.GameSession session)
        => string.Join("|", session.CaseFile.PublicWarrants.Select(warrant => $"{warrant.Id.Value}:{warrant.TargetName}:{warrant.Terms.TargetKind}:{string.Join("/", warrant.Terms.GangAffiliations.Select(gang => gang.Value))}:{warrant.Terms.AdvancesGangPressureFor?.Value ?? string.Empty}"));

    private static string TurfSignature(WildBunch.Domain.Game.GameSession session)
        => string.Join("|", session.CaseFile.SuspectTurfAssignments.Select(assignment => $"{assignment.SuspectId.Value}:{assignment.TurfTownId.Value}"));
}
