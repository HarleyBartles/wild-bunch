using WildBunch.Api;
using WildBunch.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddWildBunchServices(builder.Configuration);

var app = builder.Build();

app.Services.ApplyWildBunchMigrations();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapWildBunchApi();

app.Run();

public partial class Program { }
