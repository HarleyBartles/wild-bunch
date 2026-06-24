using System.Text.RegularExpressions;

namespace WildBunch.Application.Tests;

public sealed class AddLogEntryGuardrailTests
{
    // Known count of AddLogEntry references in GameSession.cs after BUNCH-86.
    // This includes the method definition itself (private void AddLogEntry(...))
    // plus 5 call sites: Apply(GameStarted), Apply(StoreItemPurchased),
    // RecordTravelUpdate (called from travel Apply methods), RecordCaseUpdate
    // (called from investigation Apply methods), and CompleteCase (dead stub —
    // no production callers). BUNCH-86 moved the purchase AddLogEntry from
    // Purchase() into Apply(StoreItemPurchased) — a move, not a removal, so the
    // count stays at 6. Task 11 will remove CompleteCase, dropping the count to 5.
    // Do not increase this number without explicit architecture approval.
    // AddLogEntry is [Obsolete] projection-legacy per ADR-0028.
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
