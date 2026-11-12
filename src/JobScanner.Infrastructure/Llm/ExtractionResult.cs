using System.Text.Json;
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
    [property: JsonPropertyName("confidence")] double? Confidence,
    [property: JsonPropertyName("requiredExperience")] List<SkillRequirementDto>? RequiredExperience = null);

/// <summary>
/// requiredExperience ogesinin HAM karsiligi. LLM alanlari atlayabilir ya da null gonderebilir,
/// bu yuzden her ikisi de nullable; temizlik/filtreleme LlmEligibilityExtractor.NormalizeExperience'da yapilir.
/// </summary>
internal sealed record SkillRequirementDto(
    [property: JsonPropertyName("skill")] string? Skill,
    [property: JsonPropertyName("minYears")] JsonElement? MinYears)
{
    /// <summary>minYears'i toleransla int'e cevirir: 3, 2.5, "4", "5+" kabul; null/anlamsiz → null.</summary>
    public int? ParsedMinYears()
    {
        if (MinYears is not { } e) return null;
        if (e.ValueKind == JsonValueKind.Number && e.TryGetDouble(out var d)) return ToYears(d);
        if (e.ValueKind == JsonValueKind.String)
        {
            var digits = new string((e.GetString() ?? "").TakeWhile(c => char.IsDigit(c) || c == '.').ToArray());
            if (double.TryParse(digits, System.Globalization.CultureInfo.InvariantCulture, out var v)) return ToYears(v);
        }
        return null;
    }

    private static int? ToYears(double d) =>
        double.IsNaN(d) || double.IsInfinity(d) ? null : (int)Math.Clamp(Math.Ceiling(d), 0, 1000);
}
