using WildBunch.Domain.Cases;
using WildBunch.Domain.Economy;
using WildBunch.Domain.Inventory;
using WildBunch.Domain.Travel;
using WildBunch.Domain.World;
using WildBunch.GameContent.NewGame;

namespace WildBunch.GameContent.Tests;

public sealed class GameSetupPackageBuilderTests
{
    [Fact]
    public void SameDescriptorProducesTheSameDurableStartingPackage()
    {
        var descriptor = StartingWorldDescriptorResolver.CreateCanonicalDescriptor();

        var packageA = BuildPackage(descriptor);
        var packageB = BuildPackage(descriptor);

        Assert.Equal(BuildPackageSignature(packageA), BuildPackageSignature(packageB));
    }

    [Fact]
    public void DifferentUuidSeedCodesCanChangeAtLeastOneSetupSurface()
    {
        var firstDescriptor = StartingWorldDescriptorResolver.Resolve(CreateSeedCode(1, 0, 0, 0, 1, 0, 1, tail: 11));
        var secondDescriptor = StartingWorldDescriptorResolver.Resolve(CreateSeedCode(3, 2, 1, 1, 6, 8, 2, tail: 42));

        var firstPackage = BuildPackage(firstDescriptor);
        var secondPackage = BuildPackage(secondDescriptor);

        Assert.NotEqual(BuildPackageSignature(firstPackage), BuildPackageSignature(secondPackage));
    }

    [Fact]
    public void DifferentLoadoutProfilesChangeTheStartingLoadoutAndWallet()
    {
        var baseline = StartingWorldDescriptorResolver.CreateCanonicalDescriptor();
        var horseDescriptor = baseline with
        {
            Player = baseline.Player with
            {
                StartWithHorse = true,
                LoadoutProfile = StartingLoadoutProfile.Stocked,
                StartingCash = 30m,
                Loadout = baseline.Player.Loadout with
                {
                    Food = 6,
                    HorseFeed = 4,
                    RevolverAmmo = 8,
                    IncludeHorse = true,
                    IncludeSaddle = true
                }
            }
        };
        var footDescriptor = baseline with
        {
            World = baseline.World with { StartingTownSelectionKey = GameSetupDeterministicLabels.WorldStartingTownFoot },
            Player = baseline.Player with
            {
                StartWithHorse = false,
                LoadoutProfile = StartingLoadoutProfile.Light,
                StartingCash = 18m,
                Loadout = baseline.Player.Loadout with
                {
                    Food = 3,
                    HorseFeed = 2,
                    RevolverAmmo = 4,
                    IncludeHorse = false,
                    IncludeSaddle = false
                }
            }
        };

        var horsePackage = BuildPackage(horseDescriptor);
        var footPackage = BuildPackage(footDescriptor);

        Assert.True(horsePackage.StartingInventory.HasItem(ItemKind.Horse));
        Assert.False(footPackage.StartingInventory.HasItem(ItemKind.Horse));
        Assert.True(horsePackage.StartingInventory.HasItem(ItemKind.Saddle));
        Assert.False(footPackage.StartingInventory.HasItem(ItemKind.Saddle));
        Assert.NotEqual(horsePackage.StartingWallet.Cash, footPackage.StartingWallet.Cash);
    }

    [Fact]
    public void DifferentDifficultyChangesTravelRulesAndStartingCash()
    {
        var easyDescriptor = StartingWorldDescriptorResolver.CreateCanonicalDescriptor(GameDifficulty.Easy);
        var hardDescriptor = StartingWorldDescriptorResolver.CreateCanonicalDescriptor(GameDifficulty.Challenging);

        var easyPackage = BuildPackage(easyDescriptor);
        var hardPackage = BuildPackage(hardDescriptor);

        Assert.NotEqual(easyPackage.TravelRulesProfile, hardPackage.TravelRulesProfile);
        Assert.Equal(30m, easyPackage.StartingWallet.Cash);
        Assert.Equal(20m, hardPackage.StartingWallet.Cash);
    }

    [Fact]
    public void CanonicalDescriptorUsesTheExplicitCanonicalPlan()
    {
        var canonicalDescriptor = StartingWorldDescriptorResolver.CreateCanonicalDescriptor();
        var plan = StartingWorldGenerationPlan.Create(canonicalDescriptor);

        Assert.True(plan.IsCanonical);
        Assert.Equal(SeedWorldVariant.Canonical, plan.WorldVariant);

        var package = BuildPackage(canonicalDescriptor);
        Assert.Equal(new TownId("pinecross"), package.StartingTownId);
        Assert.Equal(25m, package.StartingWallet.Cash);
        Assert.Equal(SeedWorldVariant.Canonical, plan.WorldVariant);
        Assert.Equal(7, package.CaseFile.Suspects.Count);
        Assert.Single(package.CaseFile.Suspects, suspect => suspect.Id.Equals(package.CaseFile.TrueCulpritId));
        Assert.Equal(5, package.CaseFile.KillerReleaseThreshold);
        Assert.Equal("The culprit has a scar on his left cheek.", package.CaseFile.OpeningLead.Description);
        Assert.Equal("Butch Cassidy", package.CaseFile.PublicWarrants[0].TargetName);
        Assert.Equal(InvestigationTargetKind.GangMember, package.CaseFile.PublicWarrants[0].Terms.TargetKind);
        Assert.DoesNotContain(package.CaseFile.PublicWarrants, warrant => warrant.TargetName == package.CaseFile.Suspects[3].Name);
        Assert.DoesNotContain(package.CaseFile.PublicWarrants[0].Terms.KnownFeatures, feature => feature.Contains("scar", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(package.CaseFile.KnownClues, clue =>
            clue.Kind == ClueKind.CulpritTrail
            && clue.TargetKind == InvestigationTargetKind.TrueCulprit);
        Assert.All(package.CaseFile.KnownClues.Concat(package.CaseFile.PublicClues), clue => Assert.True(clue.Anchors.HasAnchors));
        var localGossipClue = Assert.Single(package.CaseFile.PublicClues, clue => clue.Description.StartsWith("Local gossip out of ", StringComparison.Ordinal));
        Assert.NotEmpty(localGossipClue.Anchors.Locations);
        Assert.NotEmpty(localGossipClue.Anchors.Times);
        Assert.NotEmpty(localGossipClue.Anchors.Directions);
        Assert.Equal(7, package.CaseFile.SuspectTurfAssignments.Count);
        Assert.All(package.CaseFile.SuspectTurfAssignments, assignment => Assert.Contains(package.World.Towns, town => town.Id.Equals(assignment.TurfTownId)));
        Assert.All(package.CaseFile.SuspectTurfAssignments, assignment => Assert.Contains(package.CaseFile.Suspects, suspect => suspect.Id.Equals(assignment.SuspectId)));
    }

    [Fact]
    public void DifferentUuidSeedsCanChangeSuspectTurfAssignments()
    {
        var baseDescriptor = StartingWorldDescriptorResolver.Resolve(CreateSeedCode(1, 0, 0, 0, 1, 0, 1, tail: 31));
        var varyingDescriptor = StartingWorldDescriptorResolver.Resolve(CreateSeedCode(1, 0, 0, 0, 1, 0, 1, tail: 63));

        var samePackage = BuildPackage(baseDescriptor);
        var samePackageAgain = BuildPackage(baseDescriptor);
        var varyingPackage = BuildPackage(varyingDescriptor);

        Assert.Equal(TurfSignature(samePackage), TurfSignature(samePackageAgain));
        Assert.NotEqual(TurfSignature(samePackage), TurfSignature(varyingPackage));
    }

    private static GameSetupPackage BuildPackage(StartingWorldDescriptor descriptor)
        => new GameSetupPackageBuilder().Build(descriptor);

    private static string BuildPackageSignature(GameSetupPackage package)
        => string.Join(
            "|",
            package.Descriptor.SeedCode,
            package.Descriptor.GameDifficulty,
            package.Descriptor.GameEntropy,
            package.Descriptor.World.Variant,
            package.Descriptor.Player.StartWithHorse,
            package.Descriptor.Player.LoadoutProfile,
            package.Descriptor.Player.StartingCash,
            package.TravelRulesProfile,
            package.StartingTownId.Value,
            package.StartingWallet.Cash,
            string.Join(",", package.World.Towns.OrderBy(town => town.Id.Value, StringComparer.OrdinalIgnoreCase).Select(town => $"{town.Id.Value}:{town.Name}:{town.Services}")),
            string.Join(",", package.World.Trails.OrderBy(trail => trail.Id.Value, StringComparer.OrdinalIgnoreCase).Select(trail => $"{trail.Id.Value}:{trail.FromTownId.Value}:{trail.ToTownId.Value}:{trail.Risk}:{trail.Terrain}:{trail.WaterFeature}:{trail.RideDayDistance}")),
            string.Join(",", package.StartingInventory.Items.Select(item => $"{item.Kind}:{item.Quantity}:{item.HorseState?.Hunger ?? -1}:{item.HorseState?.Thirst ?? -1}:{item.HorseState?.Exhaustion ?? -1}:{item.CanteenState?.Charges ?? -1}:{item.CanteenState?.Capacity ?? -1}")),
            string.Join(",", package.CaseFile.Suspects.Select(suspect => $"{suspect.Id.Value}:{suspect.Name}:{suspect.Status}")),
            package.CaseFile.TrueCulpritId.Value,
            package.CaseFile.Accusation?.Value ?? string.Empty,
            package.CaseFile.OpeningLead.Description,
            string.Join(",", package.CaseFile.KnownClues.Select(DescribeClue)),
            string.Join(",", package.CaseFile.PublicClues.Select(DescribeClue)),
            string.Join(",", package.CaseFile.PublicWarrants.Select(warrant => $"{warrant.Id.Value}:{warrant.TargetName}:{warrant.Terms.Disposition}:{warrant.Terms.BountyAmount}:{string.Join("/", warrant.Terms.KnownAliases)}:{string.Join("/", warrant.Terms.KnownFeatures)}:{warrant.Terms.IssuingSource}:{warrant.Terms.TargetKind}:{string.Join("/", warrant.Terms.GangAffiliations.Select(gang => gang.Value))}:{warrant.Terms.AdvancesGangPressureFor?.Value ?? string.Empty}:{warrant.Summary}")),
            TurfSignature(package));

    private static string TurfSignature(GameSetupPackage package)
        => string.Join("|", package.CaseFile.SuspectTurfAssignments.Select(assignment => $"{assignment.SuspectId.Value}:{assignment.TurfTownId.Value}"));

    private static string DescribeClue(Clue clue)
        => $"{clue.Id.Value}:{clue.Kind}:{clue.Description}:{clue.TargetKind}:{clue.Source}:{clue.Context}:{string.Join("/", clue.LinkedSuspectIds.Select(id => id.Value))}:{DescribeAnchors(clue.Anchors)}";

    private static string DescribeAnchors(ClueAnchors anchors)
        => string.Join(
            "|",
            $"subjects={string.Join("/", anchors.Subjects.Select(subject => $"{subject.Label}:{subject.Alias ?? string.Empty}:{subject.Feature ?? string.Empty}:{subject.Fact ?? string.Empty}"))}",
            $"locations={string.Join("/", anchors.Locations.Select(location => $"{location.Label}:{location.TownId?.Value ?? string.Empty}:{location.Place ?? string.Empty}:{location.Route ?? string.Empty}"))}",
            $"times={string.Join("/", anchors.Times.Select(time => $"{time.Recency}:{time.Day?.ToString() ?? string.Empty}:{time.Turn?.ToString() ?? string.Empty}"))}",
            $"directions={string.Join("/", anchors.Directions.Select(direction => $"{direction.Label}:{direction.Movement ?? string.Empty}:{direction.DestinationTownId?.Value ?? string.Empty}:{direction.Route ?? string.Empty}"))}");

    private static Guid CreateSeedCode(byte byte0, byte byte1, byte byte2, byte byte3, byte byte4, byte byte5, byte byte6, ulong tail)
        => StartingWorldDescriptorSeedCodeFactory.CreateSeedCode(byte0, byte1, byte2, byte3, byte4, byte5, byte6, tail);
}
