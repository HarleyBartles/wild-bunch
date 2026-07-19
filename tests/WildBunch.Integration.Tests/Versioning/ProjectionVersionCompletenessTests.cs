using WildBunch.Persistence.GameSessions;
using WildBunch.Persistence.Versioning;

namespace WildBunch.Integration.Tests.Versioning;

/// <summary>
/// Build-time test: asserts every projection type has a version declared
/// in ProjectionVersions. No silent missing version declarations.
/// See the event sourcing integrity policy.
/// </summary>
public sealed class ProjectionVersionCompletenessTests
{
    [Fact]
    public void DiaryDayVersion_IsDeclared()
    {
        Assert.Equal(1, ProjectionVersions.DiaryDay);
    }

    [Fact]
    public void AllComponentNames_HaveVersionDeclared()
    {
        // Every component name in GameSessionComponentNames should return a
        // valid version from ProjectionVersions.ForComponent. Since all
        // components share the same version today, this asserts that
        // ForComponent returns 1 for every known component name.
        var componentNames = new[]
        {
            GameSessionComponentNames.Player,
            GameSessionComponentNames.World,
            GameSessionComponentNames.CaseFile,
            GameSessionComponentNames.Clock,
            GameSessionComponentNames.PursuitState,
            GameSessionComponentNames.Setup,
            GameSessionComponentNames.SaltSource,
            GameSessionComponentNames.TownVisitState,
            GameSessionComponentNames.Journey,
            GameSessionComponentNames.CompletedJourneyHistory,
            GameSessionComponentNames.WantedSuspectPresenceLedger,
            GameSessionComponentNames.CurrentActionContext,
            GameSessionComponentNames.PendingDevTravelOverride,
            GameSessionComponentNames.PendingDevSaloonOverride,
            GameSessionComponentNames.DevLayoutSalts,
            GameSessionComponentNames.UnrelatedCriminalLedger,
        };

        foreach (var name in componentNames)
        {
            var version = ProjectionVersions.ForComponent(name);
            Assert.True(version >= 1, $"Component '{name}' has version {version} — must be >= 1.");
        }
    }
}
