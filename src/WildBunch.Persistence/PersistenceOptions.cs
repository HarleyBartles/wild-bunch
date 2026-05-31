namespace WildBunch.Persistence;

public sealed class PersistenceOptions
{
    public const string SectionName = "Persistence";

    public PersistenceProvider Provider { get; set; } = PersistenceProvider.Sqlite;
}

public enum PersistenceProvider
{
    Sqlite,
    PostgreSql
}

internal static class PersistenceConnectionStrings
{
    internal const string Sqlite = "WildBunchDb";
    internal const string PostgreSql = "WildBunchPostgresDb";
}
