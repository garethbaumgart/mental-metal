using MentalMetal.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseStaticFiles();
app.MapFallbackToFile("index.html");

app.Run();
