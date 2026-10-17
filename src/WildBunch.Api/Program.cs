using WildBunch.Api;
using WildBunch.Api.Health;
using WildBunch.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddWildBunchServices(builder.Configuration);
builder.Services.AddHealthChecks().AddCheck<DatabaseReadinessCheck>("database-readiness");

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.Services.ApplyWildBunchMigrations();
}

app.MapGet("/health", () => Results.Ok());
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Name == "database-readiness",
    ResponseWriter = static (context, report) => context.Response.WriteAsJsonAsync(
        new { status = report.Status == Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy ? "healthy" : "unhealthy" },
        context.RequestAborted)
});

if (app.Environment.IsDevelopment())
{
    app.UseCors("ViteDevClient");
    app.MapOpenApi();
}

app.MapWildBunchApi();

app.Run();

public partial class Program { }
