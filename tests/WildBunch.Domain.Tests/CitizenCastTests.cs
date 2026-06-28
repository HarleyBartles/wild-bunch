using WildBunch.Domain.Game;
using WildBunch.Domain.World;

namespace WildBunch.Domain.Tests;

public sealed class CitizenCastTests
{
    private static readonly IReadOnlyList<string> SampleFeatures =
    [
        "Has a limp in the left leg.",
        "Wears a distinctive earring in the left ear.",
        "Has a scar on the left cheek.",
        "Wears an eyepatch over the left eye.",
        "Prefers a sand-colored hat with the brim stitched flat."
    ];

    [Fact]
    public void CitizenCast_HasAtLeastTwelveRoles()
    {
        Assert.True(CitizenCast.Roles.Count >= 12);
    }

    [Fact]
    public void CitizenCast_NoDuplicateRoleKeys()
    {
        var keys = CitizenCast.Roles.Select(r => r.Key).ToList();
        Assert.Equal(keys.Count, keys.Distinct().Count());
    }

    [Fact]
    public void CitizenCast_NoDuplicateRoleDisplayNames()
    {
        var names = CitizenCast.Roles.Select(r => r.DisplayName).ToList();
        Assert.Equal(names.Count, names.Distinct().Count());
    }

    [Fact]
    public void CitizenCast_SelectIsDeterministic()
    {
        var townId = new TownId("t-abelene");

        var first = CitizenCast.Select(townId, 1, 1, 1, SampleFeatures);
        var second = CitizenCast.Select(townId, 1, 1, 1, SampleFeatures);

        Assert.Equal(first.Role.Key, second.Role.Key);
        Assert.Equal(first.FeatureDescription, second.FeatureDescription);
    }

    [Fact]
    public void CitizenCast_SelectDifferentInputsProduceVariedEncounters()
    {
        var townId = new TownId("t-abelene");
        var distinctRoles = new HashSet<string>();

        for (var day = 1; day <= 5; day++)
        {
            for (var turn = 1; turn <= 4; turn++)
            {
                for (var visit = 1; visit <= 3; visit++)
                {
                    var encounter = CitizenCast.Select(townId, day, turn, visit, SampleFeatures);
                    distinctRoles.Add(encounter.Role.Key);
                }
            }
        }

        Assert.True(distinctRoles.Count >= 5,
            $"Expected at least 5 distinct role picks across varied inputs, got {distinctRoles.Count}.");
    }

    [Fact]
    public void CitizenCast_Select_PicksFeatureFromProvidedDescriptions()
    {
        var townId = new TownId("t-abelene");
        var encounter = CitizenCast.Select(townId, 2, 3, 1, SampleFeatures);

        Assert.NotNull(encounter.FeatureDescription);
        Assert.Contains(encounter.FeatureDescription, SampleFeatures);
    }

    [Fact]
    public void CitizenCast_Select_WithEmptyFeatureDescriptions_FallsBackGracefully()
    {
        var townId = new TownId("t-abelene");
        var encounter = CitizenCast.Select(townId, 1, 1, 1, Array.Empty<string>());

        Assert.Null(encounter.FeatureDescription);
        Assert.Equal("an unfamiliar face", CitizenCast.ResolveDescriptor(encounter));
    }

    [Fact]
    public void CitizenCast_SelectByRoleKey_ResolvesCorrectly()
    {
        foreach (var role in CitizenCast.Roles)
        {
            var encounter = CitizenCast.SelectByRoleKey(role.Key, SampleFeatures);
            Assert.Equal(role.Key, encounter.Role.Key);
            Assert.Equal(role.DisplayName, encounter.Role.DisplayName);
            Assert.Contains(encounter.FeatureDescription, SampleFeatures);
        }
    }

    [Fact]
    public void CitizenCast_SelectByRoleKey_ThrowsForUnknownKey()
    {
        Assert.Throws<ArgumentException>(() =>
            CitizenCast.SelectByRoleKey("nonexistent-role", SampleFeatures));
    }

    [Fact]
    public void CitizenCast_GetRoleByKey_ResolvesCorrectly()
    {
        foreach (var role in CitizenCast.Roles)
        {
            var resolved = CitizenCast.GetRoleByKey(role.Key);
            Assert.Equal(role.Key, resolved.Key);
            Assert.Equal(role.DisplayName, resolved.DisplayName);
            Assert.Equal(role.ShortName, resolved.ShortName);
        }
    }

    [Fact]
    public void CitizenCast_GetRoleByKey_ThrowsForUnknownKey()
    {
        Assert.Throws<ArgumentException>(() => CitizenCast.GetRoleByKey("nonexistent-role"));
    }

    [Fact]
    public void CitizenCast_GetRoleByKey_DoesNotRequireFeatureDescriptions()
    {
        // GetRoleByKey takes only a role key — no featureDescriptions parameter.
        // This proves the confrontation reveal path can resolve the display name
        // without re-selecting a feature.
        var role = CitizenCast.GetRoleByKey("butcher");
        Assert.Equal("the town butcher", role.DisplayName);
    }

    [Fact]
    public void CitizenCast_ResolveDescriptor_ProducesConcealmentFormat()
    {
        var encounter = CitizenCast.SelectByRoleKey("butcher", SampleFeatures);
        var descriptor = CitizenCast.ResolveDescriptor(encounter);

        Assert.StartsWith("a stranger with ", descriptor);
        Assert.DoesNotContain(encounter.Role.DisplayName, descriptor);
        Assert.DoesNotContain(encounter.Role.ShortName, descriptor);
    }

    [Fact]
    public void CitizenCast_ResolveRevealName_ProducesRoleDisplayName()
    {
        var encounter = CitizenCast.SelectByRoleKey("butcher", SampleFeatures);
        Assert.Equal("the town butcher", CitizenCast.ResolveRevealName(encounter));
    }

    [Fact]
    public void CitizenCast_ResolveRevealNarration_ContainsRoleAndFine()
    {
        var encounter = CitizenCast.SelectByRoleKey("butcher", SampleFeatures);
        var narration = CitizenCast.ResolveRevealNarration(encounter, 5.00m);

        Assert.Contains("sheriff identifies them as the town butcher", narration);
        Assert.Contains("$5.00", narration);
        Assert.Contains("releases them", narration);
    }
}
