using System.Text;
using JobScanner.Domain.Jobs;

namespace JobScanner.Infrastructure.Llm;

/// <summary>
/// Prompt kayıt defteri. Fact-extraction prompt'unu sürüm (+ ileride model) bazında üretir.
/// LLM yalnız GERÇEK çıkarır; KARAR vermez (karar saf C#'ta).
/// </summary>
public sealed class PromptRegistry
{
    private const int MaxDescriptionChars = 6000;

    public (string System, string User) GetExtractionPrompt(string promptVersion, string model, JobPosting job)
    {
        // Şimdilik tek sürüm; model varyantları gerektiğinde buraya switch eklenir.
        return promptVersion switch
        {
            _ => (SystemV1(), UserV1(job)),
        };
    }

    private static string SystemV1() =>
        """
        You are a precise information extractor for remote job postings. You DO NOT make hiring
        or eligibility decisions. You ONLY extract facts and return a single JSON object.

        Return ONLY valid JSON (no markdown, no commentary) with EXACTLY these fields:
        {
          "requiresWorkAuth": true|false|null,        // true ONLY if it explicitly requires local right-to-work / sponsorship cannot be provided
          "requiresRelocation": true|false|null,      // true ONLY if it explicitly requires relocating / moving / becoming onsite
          "backgroundCheckCountry": "..."|null,        // if a background/criminal-record check or clearance tied to a SPECIFIC country is required, name that country (e.g. "UK","US"); null if generic or not required
          "allowedCountries": ["..."]|null,            // countries/regions explicitly allowed (e.g. "Worldwide","EU","USA","Turkey"); null if not stated
          "requiresCitizenship": true|false|null,      // true ONLY if citizenship is explicitly required
          "allowsB2BContractor": true|false|null,      // true if B2B/contractor explicitly accepted; false ONLY if explicitly rejected; null if not mentioned
          "engagementType": "Unknown|Employee|Contractor|B2B|Freelance|EmployeeViaEor",
          "mentionsEor": true|false|null,              // true if it mentions Employer of Record / EOR / hiring via a local entity
          "eorPlatform": "..."|null,                   // EOR/platform name if any (Deel, Remote.com, Oyster, ...)
          "dataBoundary": "..."|null,                  // data residency constraint (e.g. "EU only","US only") or null
          "timezoneRequirementRaw": "..."|null,        // raw timezone/overlap requirement (e.g. "EST 9-5","CET overlap") or null
          "isRecruiterAgency": true|false|null,        // posting is by a recruiting agency
          "confidence": 0.0                            // 0..1 your confidence in the extraction
        }

        CRITICAL: Use null when the posting does NOT state a fact. Do NOT infer false from silence —
        only use true/false when the text explicitly states it. Do not guess beyond the text.
        """;

    private static string UserV1(JobPosting job)
    {
        var sb = new StringBuilder();
        sb.Append("Title: ").AppendLine(job.Title);
        sb.Append("Company: ").AppendLine(job.Company);
        sb.AppendLine("Description:");
        sb.AppendLine(Truncate(job.DescriptionText, MaxDescriptionChars));
        return sb.ToString();
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max];
}
