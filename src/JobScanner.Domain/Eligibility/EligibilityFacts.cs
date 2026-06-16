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
    IReadOnlyList<string>? AllowedCountries,
    bool? RequiresCitizenship,
    bool? AllowsB2BContractor,
    EngagementType EngagementType,
    bool? MentionsEor,
    string? EorPlatform,
    string? DataBoundary,
    string? TimezoneRequirementRaw,            // ör. "EST 9-5", "CET core hours"
    bool? IsRecruiterAgency,
    double Confidence,
    DateTimeOffset ExtractedAt,
    string RawJson);
