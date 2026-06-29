using WildBunch.Domain.Cases;
using WildBunch.Domain.Game;

namespace WildBunch.Domain.Tests;

public sealed class WantedPosterResolverTests
{
    [Fact]
    public void BoringMode_SameTownSameVisit_ReturnsSameWarrant()
    {
        var caseFile = BuildTestCaseFile();
        var resolver = new WantedPosterResolver();

        var first = resolver.Resolve(caseFile, townSlotIndex: 2, visitCount: 0, salt: null);
        var second = resolver.Resolve(caseFile, townSlotIndex: 2, visitCount: 0, salt: null);

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(first!.Id, second!.Id);
    }

    [Fact]
    public void BoringMode_DifferentTowns_ReturnDifferentWarrants()
    {
        var caseFile = BuildTestCaseFile();
        var resolver = new WantedPosterResolver();

        var town0 = resolver.Resolve(caseFile, townSlotIndex: 0, visitCount: 0, salt: null);
        var town1 = resolver.Resolve(caseFile, townSlotIndex: 1, visitCount: 0, salt: null);

        Assert.NotNull(town0);
        Assert.NotNull(town1);
        Assert.NotEqual(town0!.Id, town1!.Id);
    }

    [Fact]
    public void BoringMode_CulpritWarrantNotSurfacesUntilReleased()
    {
        var caseFile = BuildTestCaseFile(killerReleased: false);
        var resolver = new WantedPosterResolver();

        // Try all town/visit combos — culprit warrant should never surface.
        for (int town = 0; town < 8; town++)
        {
            for (int visit = 0; visit < 5; visit++)
            {
                var warrant = resolver.Resolve(caseFile, town, visit, salt: null);
                Assert.NotNull(warrant);
                Assert.NotEqual(InvestigationTargetKind.TrueCulprit, warrant!.Terms.TargetKind);
            }
        }
    }

    [Fact]
    public void BoringMode_AfterKillerReleased_CulpritWarrantCanSurface()
    {
        var caseFile = BuildTestCaseFile(killerReleased: true);
        var resolver = new WantedPosterResolver();

        // With the killer released, the culprit warrant is eligible and must be
        // reachable across the full town/visit space.
        var sawCulprit = false;
        for (int town = 0; town < 8 && !sawCulprit; town++)
        {
            for (int visit = 0; visit < 8 && !sawCulprit; visit++)
            {
                var warrant = resolver.Resolve(caseFile, town, visit, salt: null);
                Assert.NotNull(warrant);
                if (warrant!.Terms.TargetKind == InvestigationTargetKind.TrueCulprit)
                {
                    sawCulprit = true;
                }
            }
        }

        Assert.True(sawCulprit, "Culprit warrant should become eligible once the killer is released.");
    }

    [Fact]
    public void BoringMode_AlreadyKnownWarrantsAreSkipped()
    {
        var caseFile = BuildTestCaseFile();
        var resolver = new WantedPosterResolver();

        var first = resolver.Resolve(caseFile, townSlotIndex: 0, visitCount: 0, salt: null);
        Assert.NotNull(first);
        caseFile.RevealWarrant(first!);

        var next = resolver.Resolve(caseFile, townSlotIndex: 0, visitCount: 0, salt: null);
        Assert.NotNull(next);
        Assert.NotEqual(first!.Id, next!.Id);
    }

    [Fact]
    public void BoringMode_ReturnsNullWhenPoolExhausted()
    {
        var caseFile = BuildTestCaseFile();
        var resolver = new WantedPosterResolver();

        // Reveal every non-culprit warrant (the only eligible ones while locked).
        for (int i = 0; i < 7; i++)
        {
            var warrant = resolver.Resolve(caseFile, townSlotIndex: i, visitCount: 0, salt: null);
            Assert.NotNull(warrant);
            caseFile.RevealWarrant(warrant!);
        }

        var exhausted = resolver.Resolve(caseFile, townSlotIndex: 0, visitCount: 0, salt: null);
        Assert.Null(exhausted);
    }

    [Fact]
    public void SaltMode_SameInputsSameSalt_ReturnsSameWarrant()
    {
        var caseFile = BuildTestCaseFile();
        var resolver = new WantedPosterResolver();
        var salt = SaltSource.CreateFixed("42");

        var first = resolver.Resolve(caseFile, townSlotIndex: 3, visitCount: 1, salt: salt);
        var second = resolver.Resolve(caseFile, townSlotIndex: 3, visitCount: 1, salt: salt);

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(first!.Id, second!.Id);
    }

    [Fact]
    public void SaltMode_DifferentSalt_ReturnsDifferentWarrant()
    {
        var caseFile = BuildTestCaseFile();
        var resolver = new WantedPosterResolver();

        var saltA = SaltSource.CreateFixed("42");
        var saltB = SaltSource.CreateFixed("99");

        var first = resolver.Resolve(caseFile, townSlotIndex: 3, visitCount: 1, salt: saltA);
        var second = resolver.Resolve(caseFile, townSlotIndex: 3, visitCount: 1, salt: saltB);

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.NotEqual(first!.Id, second!.Id);
    }

    [Fact]
    public void SaltMode_CulpritWarrantNotSurfacesUntilReleased()
    {
        var caseFile = BuildTestCaseFile(killerReleased: false);
        var resolver = new WantedPosterResolver();
        var salt = SaltSource.CreateFixed("deadbeef");

        for (int town = 0; town < 8; town++)
        {
            for (int visit = 0; visit < 5; visit++)
            {
                var warrant = resolver.Resolve(caseFile, town, visit, salt: salt);
                Assert.NotNull(warrant);
                Assert.NotEqual(InvestigationTargetKind.TrueCulprit, warrant!.Terms.TargetKind);
            }
        }
    }

    [Fact]
    public void Resolve_NullCaseFile_Throws()
    {
        var resolver = new WantedPosterResolver();
        Action act = () => resolver.Resolve(null!, 0, 0, salt: null);
        Assert.Throws<ArgumentNullException>(act);
    }

    [Fact]
    public void BoringMode_RetiredWarrantsDoNotSurface()
    {
        var caseFile = BuildTestCaseFile();
        var resolver = new WantedPosterResolver();

        // Find the warrant that would surface at slot 0, visit 0.
        var first = resolver.Resolve(caseFile, townSlotIndex: 0, visitCount: 0, salt: null, retiredWarrantIds: null);
        Assert.NotNull(first);

        // Retire that warrant — it must not surface again.
        var retired = new HashSet<WarrantId> { first!.Id };
        var second = resolver.Resolve(caseFile, townSlotIndex: 0, visitCount: 0, salt: null, retiredWarrantIds: retired);

        Assert.NotNull(second);
        Assert.NotEqual(first.Id, second!.Id);
    }

    [Fact]
    public void BoringMode_AllNonCulpritRetired_ReturnsNull()
    {
        var caseFile = BuildTestCaseFile();
        var resolver = new WantedPosterResolver();

        // Retire all 7 unrelated warrants (the only eligible ones while killer is locked).
        var allUnrelated = caseFile.PublicWarrants
            .Where(w => w.Terms.TargetKind == InvestigationTargetKind.UnrelatedWantedCriminal)
            .Select(w => w.Id)
            .ToHashSet();

        var result = resolver.Resolve(caseFile, townSlotIndex: 0, visitCount: 0, salt: null, retiredWarrantIds: allUnrelated);
        Assert.Null(result);
    }

    /// <summary>
    /// Builds a CaseFile with a pool of 8 public warrants: one TrueCulprit warrant
    /// plus seven UnrelatedWantedCriminal warrants. The culprit warrant is only
    /// eligible once the killer trail is released.
    /// </summary>
    private static CaseFile BuildTestCaseFile(bool killerReleased = false)
    {
        var suspects = new[]
        {
            new Suspect(new SuspectId("suspect-1"), "Ira Flint", SuspectTraits.FromTags(SuspectTraitTags.Local, SuspectTraitTags.Desperate), SuspectStatus.AtLarge),
            new Suspect(new SuspectId("suspect-2"), "Mira Cline", SuspectTraits.Empty, SuspectStatus.AtLarge)
        };

        var publicWarrants = new List<Warrant>
        {
            new(
                new WarrantId("warrant-culprit"),
                "Mira Cline",
                new WarrantTerms(
                    WarrantDisposition.DeadOrAlive,
                    2500m,
                    new[] { "Red Wren" },
                    new[] { "Pale scar across the left cheek" },
                    "Dodge City Marshal",
                    InvestigationTargetKind.TrueCulprit,
                    new[] { OutlawGangIds.WildBunch },
                    OutlawGangIds.WildBunch,
                    InvestigationSourceKind.SheriffWarrants),
                "Wanted for the Wild Bunch robbery."),
        };

        for (var i = 1; i <= 7; i++)
        {
            publicWarrants.Add(new Warrant(
                new WarrantId($"warrant-public-{i}"),
                $"Outlaw {i}",
                new WarrantTerms(
                    WarrantDisposition.AliveOnly,
                    300m + i,
                    new[] { $"Alias {i}" },
                    new[] { $"Feature {i}" },
                    $"Sheriff {i}",
                    InvestigationTargetKind.UnrelatedWantedCriminal,
                    Array.Empty<OutlawGangId>(),
                    null,
                    InvestigationSourceKind.SheriffWarrants),
                $"Wanted for crime {i}."));
        }

        return new CaseFile(
            accusation: null,
            suspects,
            trueCulpritId: new SuspectId("suspect-2"),
            openingLead: CaseOpeningLead.Create("A pale scar cuts across the left cheek."),
            knownClues: Array.Empty<Clue>(),
            killerReleaseThreshold: 2,
            killerReleaseProgress: killerReleased ? 2 : 0,
            publicWarrants: publicWarrants);
    }
}
