using System.Text.Json.Serialization;

namespace JobScanner.Infrastructure.Llm;

/// <summary>LLM'in döndürdüğü ham JSON gerçekleri. Toleranslı parse edilir.</summary>
internal sealed record ExtractionResult(
    [property: JsonPropertyName("requiresWorkAuth")] bool? RequiresWorkAuth,
    [property: JsonPropertyName("requiresRelocation")] bool? RequiresRelocation,
    [property: JsonPropertyName("backgroundCheckCountry")] string? BackgroundCheckCountry,
    [property: JsonPropertyName("allowedCountries")] List<string>? AllowedCountries,
    [property: JsonPropertyName("requiresCitizenship")] bool? RequiresCitizenship,
    [property: JsonPropertyName("allowsB2BContractor")] bool? AllowsB2BContractor,
    [property: JsonPropertyName("engagementType")] string? EngagementType,
    [property: JsonPropertyName("mentionsEor")] bool? MentionsEor,
    [property: JsonPropertyName("eorPlatform")] string? EorPlatform,
    [property: JsonPropertyName("dataBoundary")] string? DataBoundary,
    [property: JsonPropertyName("timezoneRequirementRaw")] string? TimezoneRequirementRaw,
    [property: JsonPropertyName("isRecruiterAgency")] bool? IsRecruiterAgency,
    [property: JsonPropertyName("isLikelyGhost")] bool? IsLikelyGhost,
    [property: JsonPropertyName("confidence")] double? Confidence);
