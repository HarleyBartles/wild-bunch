using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using WildBunch.Api;
using WildBunch.Persistence;
using WildBunch.Integration.Tests.TestInfrastructure;

namespace WildBunch.Integration.Tests.Deployment;

internal sealed class DeploymentApiFactory : WebApplicationFactory<Program>
{
    public DeploymentApiFactory(string connectionString, string environment = "Production")
    {
        ConnectionString = connectionString;
        Environment = environment;
    }

    public string ConnectionString { get; }

    public string Environment { get; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environment);
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["ConnectionStrings:WildBunchPostgresDb"] = ConnectionString
            }));
    }
}
