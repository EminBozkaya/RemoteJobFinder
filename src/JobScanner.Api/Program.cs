using JobScanner.Application;
using JobScanner.Application.Abstractions;
using JobScanner.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Faz 2 minimal görünüm: read-only. Apply/dismiss butonları + auth Faz 3.
app.MapGet("/", () => Results.Ok(new { service = "JobScanner.Api", phase = 2, endpoints = new[] { "/health", "/matches" } }));

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/matches", async (
    IUserMatchRepository matches,
    long? profileId,
    double? minScore,
    int? take,
    CancellationToken ct) =>
{
    var result = await matches.GetRankedAsync(profileId, minScore ?? 0, Math.Clamp(take ?? 50, 1, 200), ct);
    return Results.Ok(result);
});

app.Run();
