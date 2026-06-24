namespace WildBunch.Application.Tests;

/// <summary>
/// Source-inspection guardrail proving the BUNCH-84 read-path switch landed and the
/// command-load compatibility read was left untouched.
/// - GameSessionReadStoreLoader (journal/session read-model read path) must NOT query
///   GameSessionLogEntries after BUNCH-84; it derives LogEntries from StoredEvents via
///   JournalLogProjector.
/// - EfGameSessionRepository (command-load path) MUST still query GameSessionLogEntries
///   as bounded compatibility surface; its table read is deferred to the write-path-removal
///   follow-up and must not be removed in this slice.
/// See ADR-0028 and BUNCH-84.
/// </summary>
public sealed class ReadStoreLoaderJournalProjectionGuardrailTests
{
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
    public void ReadStoreLoader_NoLongerQueriesGameSessionLogEntriesTable()
    {
        var repoRoot = FindRepoRoot();
        var loaderPath = Path.Combine(repoRoot, "src", "WildBunch.Persistence", "GameSessions", "GameSessionReadStoreLoader.cs");
        Assert.True(File.Exists(loaderPath), $"Could not find GameSessionReadStoreLoader.cs at {loaderPath}.");

        var source = File.ReadAllText(loaderPath);

        // After BUNCH-84 the read-store loader derives LogEntries from StoredEvents via
        // JournalLogProjector and must not query the GameSessionLogEntries table.
        // Literal source-string check (not regex) so the guardrail reliably catches the
        // exact EF query reference if it is reintroduced.
        Assert.DoesNotContain("dbContext.GameSessionLogEntries", source);
        // It must reference the projector and StoredEvents to prove the switch landed.
        Assert.Contains("JournalLogProjector", source);
        Assert.Contains("StoredEvents", source);
    }

    [Fact]
    public void EfGameSessionRepository_StillQueriesGameSessionLogEntriesTable_AsBoundedCompatibilitySurface()
    {
        var repoRoot = FindRepoRoot();
        var repoPath = Path.Combine(repoRoot, "src", "WildBunch.Persistence", "GameSessions", "EfGameSessionRepository.cs");
        Assert.True(File.Exists(repoPath), $"Could not find EfGameSessionRepository.cs at {repoPath}.");

        var source = File.ReadAllText(repoPath);

        // The command-load path intentionally retains the GameSessionLogEntries table read
        // as bounded compatibility surface (deferred to the write-path-removal follow-up).
        // If this assertion fails, the command-load compatibility read was removed outside
        // BUNCH-84 scope — investigate before updating this test.
        // Literal source-string check (not regex) for consistency with the read-loader guardrail.
        Assert.Contains("GameSessionLogEntries", source);
    }
}
