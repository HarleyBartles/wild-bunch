using Microsoft.Data.Sqlite;

namespace WildBunch.Persistence;

internal static class SqliteConnectionStringResolver
{
    private const string DefaultRelativeDataSource = ".local/wildbunch.db";
    private static readonly string RepoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    public static string Resolve(string? connectionString)
    {
        var builder = new SqliteConnectionStringBuilder(
            string.IsNullOrWhiteSpace(connectionString)
                ? $"Data Source={DefaultRelativeDataSource}"
                : connectionString);

        if (string.IsNullOrWhiteSpace(builder.DataSource))
        {
            builder.DataSource = DefaultRelativeDataSource;
        }

        if (!Path.IsPathRooted(builder.DataSource))
        {
            builder.DataSource = Path.GetFullPath(Path.Combine(RepoRoot, builder.DataSource));
        }

        var directory = Path.GetDirectoryName(builder.DataSource);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        return builder.ToString();
    }
}
