using System.Text.RegularExpressions;

namespace WildBunch.Application.Tests;

public sealed class AddLogEntryGuardrailTests
{
    // Known count of AddLogEntry references in GameSession.cs after BUNCH-83.
    // This includes the method definition itself (private void AddLogEntry(...))
    // plus 5 call sites. The 5 migrated investigation methods now use RecordCaseUpdate
    // (which calls AddLogEntry internally) via Apply(InvestigationPerformed), and the 6
    // travel methods now use RecordTravelUpdate via Apply for their 6 typed events.
    // Do not increase this number without explicit architecture approval. AddLogEntry is
    // [Obsolete] projection-legacy per ADR-0028.
    private const int KnownLegacyAddLogEntryCallSiteCount = 6;

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "AGENTS.md")))
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }
        throw new InvalidOperationException(
            $"Could not find repo root (AGENTS.md sentinel) starting from {AppContext.BaseDirectory}");
    }

    [Fact]
    public void GameSessionDoesNotAddNewAddLogEntryCallSites()
    {
        var repoRoot = FindRepoRoot();
        var gameSessionPath = Path.Combine(repoRoot, "src", "WildBunch.Domain", "Game", "GameSession.cs");
        Assert.True(File.Exists(gameSessionPath),
            $"Could not find GameSession.cs at {gameSessionPath}. " +
            $"Repo root resolved to {repoRoot}. " +
            $"Test output base directory was {AppContext.BaseDirectory}.");

        var source = File.ReadAllText(gameSessionPath);
        var matches = Regex.Matches(source, @"\bAddLogEntry\s*\(");

        Assert.True(matches.Count <= KnownLegacyAddLogEntryCallSiteCount,
            $"AddLogEntry call site count increased to {matches.Count} (expected at most {KnownLegacyAddLogEntryCallSiteCount}). " +
            "AddLogEntry is [Obsolete] projection-legacy per ADR-0028. " +
            "New domain code must use typed domain events instead. " +
            "If this increase is intentional and approved, update KnownLegacyAddLogEntryCallSiteCount.");
    }
}
