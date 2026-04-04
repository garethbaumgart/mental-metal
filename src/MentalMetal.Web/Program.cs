using MentalMetal.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseStaticFiles();

// API endpoints will be mapped here as features are added.
// They must be registered before the SPA fallback.

app.MapFallbackToFile("index.html");

app.Run();
