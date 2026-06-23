using JobScanner.Domain.Enums;

namespace JobScanner.Domain.Eligibility;

/// <summary>
/// LLM'in ilandan çıkardığı HAM GERÇEKLER (karar değil). Cache anahtarı:
/// JobId + PromptVersion + ModelVersion + VersionHash. Uygunluk kararı bu gerçeklerden
/// saf C#'ta (IEligibilityDecider) hesaplanır.
/// </summary>
public sealed record EligibilityFacts(
    long JobId,
    string PromptVersion,
    string ModelVersion,
    string VersionHash,
    bool? RequiresWorkAuth,
    bool? RequiresRelocation,                   // açıkça taşınma/onsite'a geçiş şartı (TR'den çalışmayı engeller)
    string? BackgroundCheckCountry,             // belirli bir ülkeye ait adli sicil/clearance şartı (ör. "UK","US"); genel/yoksa null
    IReadOnlyList<string>? AllowedCountries,
    bool? RequiresCitizenship,
    bool? AllowsB2BContractor,
    EngagementType EngagementType,
    bool? MentionsEor,
    string? EorPlatform,
    string? DataBoundary,
    string? TimezoneRequirementRaw,            // ör. "EST 9-5", "CET core hours"
    bool? IsRecruiterAgency,
    bool? IsLikelyGhost,                        // "always hiring"/"talent pool"/aşırı jenerik JD sinyali (ghost/evergreen)
    double Confidence,
    DateTimeOffset ExtractedAt,
    string RawJson,
    IReadOnlyList<SkillRequirement>? RequiredExperience = null);  // Faz 5b: ilanın "X için min N yıl" şartları
