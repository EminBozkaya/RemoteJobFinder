using JobScanner.Application;
using JobScanner.Application.Abstractions;
using JobScanner.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.MapGet("/", () => Results.Ok(new
{
    service = "JobScanner.Api",
    phase = 3,
    endpoints = new[]
    {
        "GET /health",
        "GET /matches",
        "POST /matches/{profileId}/{jobId}/save",
        "POST /matches/{profileId}/{jobId}/open",
        "POST /matches/{profileId}/{jobId}/apply",
        "POST /matches/{profileId}/{jobId}/dismiss",
    },
}));

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

// --- Durum makinesi mutasyonları (Faz 3.1) ---
// Saf domain metotları repo aracılığıyla tetiklenir; idempotenttir.
// Eşleşme yoksa 404; varsa ve mutasyon uygulandıysa 204.

app.MapPost("/matches/{profileId:long}/{jobId:long}/save", async (
    long profileId, long jobId, IUserMatchRepository matches, CancellationToken ct) =>
{
    var found = await matches.WithMatchAsync(profileId, jobId, m => m.Save(), ct);
    return found ? Results.NoContent() : Results.NotFound();
});

app.MapPost("/matches/{profileId:long}/{jobId:long}/open", async (
    long profileId, long jobId, IUserMatchRepository matches, TimeProvider clock, CancellationToken ct) =>
{
    var now = clock.GetUtcNow();
    var found = await matches.WithMatchAsync(profileId, jobId, m => m.Open(now), ct);
    return found ? Results.NoContent() : Results.NotFound();
});

app.MapPost("/matches/{profileId:long}/{jobId:long}/apply", async (
    long profileId, long jobId, IUserMatchRepository matches, TimeProvider clock, CancellationToken ct) =>
{
    var now = clock.GetUtcNow();
    var found = await matches.WithMatchAsync(profileId, jobId, m => m.Apply(now), ct);
    return found ? Results.NoContent() : Results.NotFound();
});

app.MapPost("/matches/{profileId:long}/{jobId:long}/dismiss", async (
    long profileId, long jobId, IUserMatchRepository matches, CancellationToken ct) =>
{
    var found = await matches.WithMatchAsync(profileId, jobId, m => m.Dismiss(), ct);
    return found ? Results.NoContent() : Results.NotFound();
});

app.Run();
