using System.Text.Json.Serialization;
using JobScanner.Application;
using JobScanner.Application.Abstractions;
using JobScanner.Application.Applications;
using JobScanner.Domain.Users;
using JobScanner.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

// Enum'lar JSON'da isimle (string) gidip gelsin (ör. LanguageLevel "Advanced").
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// React SPA dev sunucusundan CORS (Vite varsayilan :5173). Prod'da SPA ayni origin'den
// servis edilebilir; ek origin'ler appsettings.json "Cors:AllowedOrigins" ile genisletilir.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5173"];
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();
app.UseCors();

// --- Opsiyonel bearer token koruması (self-host public deploy için) ---
// Token önceliği: env JOBSCANNER_API_TOKEN > config "Auth:Token". Hiçbiri yoksa middleware OFF
// (lokal dev'i kırmaz). Set edildiğinde /health hariç tüm endpoint'ler Authorization: Bearer <token> ister.
var apiToken = Environment.GetEnvironmentVariable("JOBSCANNER_API_TOKEN")
    ?? builder.Configuration["Auth:Token"];
if (!string.IsNullOrWhiteSpace(apiToken))
{
    app.Use(async (ctx, next) =>
    {
        if (ctx.Request.Path.StartsWithSegments("/health"))
        {
            await next();
            return;
        }
        var header = ctx.Request.Headers.Authorization.ToString();
        if (header.StartsWith("Bearer ", StringComparison.Ordinal) &&
            string.Equals(header["Bearer ".Length..], apiToken, StringComparison.Ordinal))
        {
            await next();
            return;
        }
        ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
        ctx.Response.Headers.WWWAuthenticate = "Bearer";
        await ctx.Response.WriteAsJsonAsync(new { error = "missing_or_invalid_token" });
    });
    app.Logger.LogInformation("API token koruması AÇIK (header gereken endpoint'ler için)");
}

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
        "GET  /matches/{profileId}/{jobId}/materials",
        "POST /matches/{profileId}/{jobId}/materials",
        "GET  /profile",
        "PUT  /profile/{id}",
    },
}));

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/matches", async (
    IUserMatchRepository matches,
    long? profileId,
    double? minScore,
    int? take,
    string? source,
    CancellationToken ct) =>
{
    var result = await matches.GetRankedAsync(
        profileId, minScore ?? 0, Math.Clamp(take ?? 50, 1, 200),
        string.IsNullOrWhiteSpace(source) ? null : source, ct);
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

// --- Başvuru materyali: CV + cover letter (Faz 4, on-demand) ---
// Sistem materyali yalnız HAZIRLAR; göndermez. Üretim LLM gerektirir (Llm:Enabled).

// Saklı materyali getirir (üretmez). Yoksa 404.
app.MapGet("/matches/{profileId:long}/{jobId:long}/materials", async (
    long profileId, long jobId, MaterialService materials, CancellationToken ct) =>
{
    var existing = await materials.GetExistingAsync(profileId, jobId, ct);
    return existing is null ? Results.NotFound() : Results.Ok(ToDto(existing));
});

// Taze saklı materyal varsa döner; yoksa (ya da ?force=true ise) üretir.
app.MapPost("/matches/{profileId:long}/{jobId:long}/materials", async (
    long profileId, long jobId, bool? force, MaterialService materials, CancellationToken ct) =>
{
    var result = await materials.GetOrGenerateAsync(profileId, jobId, force ?? false, ct);
    return result.Outcome switch
    {
        MaterialOutcome.Ready => Results.Ok(ToDto(result.Material!)),
        MaterialOutcome.JobNotFound => Results.NotFound(new { error = "job_not_found" }),
        MaterialOutcome.ProfileNotFound => Results.NotFound(new { error = "profile_not_found" }),
        MaterialOutcome.CvMissing => Results.Conflict(new { error = "cv_missing", message = "Ana CV bulunamadı; data/cv.md oluşturun." }),
        MaterialOutcome.LlmDisabled => Results.Conflict(new { error = "llm_disabled", message = "Materyal üretimi için Llm:Enabled=true gerekli." }),
        _ => Results.Problem("unknown_outcome"),
    };
});

// --- Kriter profili: oku + düzenle (Faz 5a) ---
// Düzenleme sonrası RecomputeService cache'ten saf C# yeniden hesaplar (token harcamaz).

app.MapGet("/profile", async (IProfileRepository profiles, CancellationToken ct) =>
{
    var profile = (await profiles.GetActiveAsync(ct)).FirstOrDefault();
    return profile is null ? Results.NotFound() : Results.Ok(new
    {
        id = profile.Id,
        name = profile.Name,
        residenceCountry = profile.ResidenceCountry,
        forbiddenKeywords = profile.ForbiddenKeywords,
        skills = profile.Skills,
        languages = profile.Languages,
        softSkills = profile.SoftSkills,
        timezoneToleranceHours = profile.TimezoneToleranceHours,
        minScoreToShow = profile.MinScoreToShow,
    });
});

app.MapPut("/profile/{id:long}", async (
    long id, ProfileEdit body, IProfileRepository profiles, RecomputeService recompute, CancellationToken ct) =>
{
    var clean = new ProfileEdit(
        ResidenceCountry: (body.ResidenceCountry ?? "TR").Trim(),
        ForbiddenKeywords: CleanKeywords(body.ForbiddenKeywords),
        Skills: CleanSkills(body.Skills),
        Languages: CleanLanguages(body.Languages),
        SoftSkills: CleanKeywords(body.SoftSkills),
        TimezoneToleranceHours: Math.Clamp(body.TimezoneToleranceHours, 0, 24),
        MinScoreToShow: Math.Clamp(body.MinScoreToShow, 0, 10));

    var updated = await profiles.UpdateAsync(id, clean, ct);
    if (!updated) return Results.NotFound();

    var matched = await recompute.RecomputeAllAsync(ct);
    return Results.Ok(new { recomputed = matched });
});

app.Run();

static IReadOnlyList<string> CleanKeywords(IReadOnlyList<string>? raw) =>
    (raw ?? [])
        .Where(k => !string.IsNullOrWhiteSpace(k))
        .Select(k => k.Trim())
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList();

static IReadOnlyList<SkillCriterion> CleanSkills(IReadOnlyList<SkillCriterion>? raw) =>
    (raw ?? [])
        .Where(s => !string.IsNullOrWhiteSpace(s.Name))
        .GroupBy(s => s.Name.Trim(), StringComparer.OrdinalIgnoreCase)
        .Select(g => g.First())
        .Select(s => new SkillCriterion(s.Name.Trim(), Math.Clamp(s.SelfRating, 1, 10), Math.Clamp(s.Years, 0, 50)))
        .ToList();

static IReadOnlyList<LanguageCriterion> CleanLanguages(IReadOnlyList<LanguageCriterion>? raw) =>
    (raw ?? [])
        .Where(l => !string.IsNullOrWhiteSpace(l.Name))
        .GroupBy(l => l.Name.Trim(), StringComparer.OrdinalIgnoreCase)
        .Select(g => g.First())
        .Select(l => new LanguageCriterion(l.Name.Trim(), l.Level))
        .ToList();

static object ToDto(JobScanner.Domain.Applications.ApplicationMaterial m) => new
{
    profileId = m.ProfileId,
    jobId = m.JobId,
    coverLetter = m.CoverLetter,
    tailoredCvMarkdown = m.TailoredCvMarkdown,
    language = m.Language,
    generatedAt = m.GeneratedAt,
};
