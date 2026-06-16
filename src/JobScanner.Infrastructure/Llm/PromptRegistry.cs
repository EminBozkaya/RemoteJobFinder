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
          "requiresWorkAuth": true|false|null,        // needs local work authorization / right to work
          "allowedCountries": ["..."]|null,            // countries/regions explicitly allowed (e.g. "Worldwide","EU","USA","Turkey")
          "requiresCitizenship": true|false|null,      // citizenship explicitly required
          "allowsB2BContractor": true|false|null,      // accepts B2B / independent contractor engagement
          "engagementType": "Unknown|Employee|Contractor|B2B|Freelance|EmployeeViaEor",
          "mentionsEor": true|false|null,              // mentions Employer of Record / EOR platform
          "eorPlatform": "..."|null,                   // EOR/platform name if any (Deel, Remote.com, ...)
          "dataBoundary": "..."|null,                  // data residency constraint (e.g. "EU only","US only") or null
          "timezoneRequirementRaw": "..."|null,        // raw timezone/overlap requirement (e.g. "EST 9-5","CET overlap") or null
          "isRecruiterAgency": true|false|null,        // posting is by a recruiting agency
          "confidence": 0.0                            // 0..1 your confidence in the extraction
        }

        Use null when the posting does not state the fact. Do not guess beyond the text.
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
