using Npgsql;

namespace WildBunch.Integration.Tests.TestInfrastructure;

public sealed class PostgreSqlTestDatabase : IDisposable
{
    private const string ConnectionStringEnvironmentVariable = "ConnectionStrings__WildBunchPostgresDb";
    private readonly string _adminConnectionString;
    private bool _disposed;

    public PostgreSqlTestDatabase()
    {
        var baseConnectionString = Environment.GetEnvironmentVariable(ConnectionStringEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(baseConnectionString))
        {
            throw new InvalidOperationException($"Set {ConnectionStringEnvironmentVariable} to run the PostgreSQL test lane.");
        }

        var builder = new NpgsqlConnectionStringBuilder(baseConnectionString);
        if (string.IsNullOrWhiteSpace(builder.Database))
        {
            throw new InvalidOperationException($"The PostgreSQL test connection string must include a database name in {ConnectionStringEnvironmentVariable}.");
        }

        _adminConnectionString = builder.ConnectionString;
        DatabaseName = $"wildbunch_{Guid.NewGuid():N}";

        builder.Database = DatabaseName;
        ConnectionString = builder.ConnectionString;

        CreateDatabase();
    }

    public string ConnectionString { get; }

    public string DatabaseName { get; }

    private void CreateDatabase()
    {
        var builder = new NpgsqlConnectionStringBuilder(_adminConnectionString);
        builder.Database = "postgres";
        var adminConnectionString = builder.ConnectionString;

        using var adminConnection = new NpgsqlConnection(adminConnectionString);
        adminConnection.Open();

        using var command = adminConnection.CreateCommand();
        command.CommandText = $"CREATE DATABASE \"{DatabaseName}\"";
        command.ExecuteNonQuery();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        var builder = new NpgsqlConnectionStringBuilder(_adminConnectionString);
        builder.Database = "postgres";
        var adminConnectionString = builder.ConnectionString;

        using var adminConnection = new NpgsqlConnection(adminConnectionString);
        adminConnection.Open();

        using (var terminateCommand = adminConnection.CreateCommand())
        {
            terminateCommand.CommandText = """
                SELECT pg_terminate_backend(pid)
                FROM pg_stat_activity
                WHERE datname = @databaseName
                  AND pid <> pg_backend_pid();
                """;
            terminateCommand.Parameters.AddWithValue("databaseName", DatabaseName);
            terminateCommand.ExecuteNonQuery();
        }

        using var dropCommand = adminConnection.CreateCommand();
        dropCommand.CommandText = $"DROP DATABASE IF EXISTS \"{DatabaseName}\"";
        dropCommand.ExecuteNonQuery();
    }
}
