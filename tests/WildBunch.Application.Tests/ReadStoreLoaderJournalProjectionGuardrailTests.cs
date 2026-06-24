namespace WildBunch.Application.Tests;

/// <summary>
/// Source-inspection guardrail proving the BUNCH-84/BUNCH-86 read-path and command-load
/// switches both landed and the GameSessionLogEntries table is fully removed.
/// - GameSessionReadStoreLoader (journal/session read-model read path) must NOT query
///   GameSessionLogEntries; it derives LogEntries from StoredEvents via JournalLogProjector.
/// - EfGameSessionRepository (command-load path) must NOT query GameSessionLogEntries
///   after BUNCH-86; it also derives LogEntries from StoredEvents via JournalLogProjector.
/// See ADR-0028, BUNCH-84, and BUNCH-86.
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
    public void EfGameSessionRepository_NoLongerQueriesGameSessionLogEntriesTable()
    {
        var repoRoot = FindRepoRoot();
        var repoPath = Path.Combine(repoRoot, "src", "WildBunch.Persistence", "GameSessions", "EfGameSessionRepository.cs");
        Assert.True(File.Exists(repoPath), $"Could not find EfGameSessionRepository.cs at {repoPath}.");

        var source = File.ReadAllText(repoPath);

        // After BUNCH-86, the command-load path derives LogEntries from the event
        // stream via JournalLogProjector, matching the read-store loader. The
        // GameSessionLogEntries table is fully removed.
        Assert.DoesNotContain("GameSessionLogEntries", source);
        Assert.Contains("JournalLogProjector", source);
    }
}
