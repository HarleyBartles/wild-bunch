using WildBunch.Api;
using WildBunch.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddWildBunchServices(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();

app.Services.ApplyWildBunchMigrations();

app.MapGet("/health", () => Results.Ok());

if (app.Environment.IsDevelopment())
{
    app.UseCors("ViteDevClient");
    app.MapOpenApi();
}

app.MapWildBunchApi();

app.Run();

public partial class Program { }
