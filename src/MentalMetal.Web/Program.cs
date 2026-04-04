using MentalMetal.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseStaticFiles();

// API endpoints will be mapped here as features are added.
// They must be registered before the SPA fallback.

// Return 404 for unmatched /api requests instead of serving the SPA shell.
app.MapFallback("/api/{**catch-all}", () => Results.NotFound());

app.MapFallbackToFile("index.html");

app.Run();
