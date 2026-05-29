using WildBunch.Api;
using WildBunch.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddWildBunchServices(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.Services.EnsureWildBunchDatabase();
    app.MapOpenApi();
}

app.MapWildBunchApi();

app.Run();

public partial class Program { }
