using System.Text;
using JobScanner.Domain.Jobs;
using JobScanner.Domain.Users;

namespace JobScanner.Infrastructure.Llm;

/// <summary>
/// Başvuru materyali (cover letter + uyarlanmış CV) prompt kayıt defteri. Sürümlü.
/// Çıktı dili ilan diline uydurulur; CV uydurma değil, ana CV'den vurgulama/yeniden düzenleme yapar.
/// </summary>
public sealed class MaterialPromptRegistry
{
    private const int MaxDescriptionChars = 5000;
    private const int MaxCvChars = 8000;

    public (string System, string User) GetMaterialPrompt(
        string promptVersion, JobPosting job, CriteriaProfile profile, string baseCvMarkdown) =>
        promptVersion switch
        {
            _ => (SystemV1(), UserV1(job, profile, baseCvMarkdown)),
        };

    private static string SystemV1() =>
        """
        You are an expert career assistant. You prepare job-application materials for a candidate.
        You DO NOT submit anything; you only draft materials the candidate will review.

        Return ONLY a single valid JSON object (no markdown fences, no commentary) with EXACTLY:
        {
          "language": "en",                 // ISO 639-1 code of the JOB POSTING's language
          "coverLetter": "...",             // a tailored cover letter, in the job posting's language
          "tailoredCvMarkdown": "..."       // the candidate's CV re-emphasized for THIS job, markdown, same language
        }

        Rules:
        - Detect the language of the JOB POSTING and write BOTH outputs in that language. Set "language" to its ISO code.
        - TRUTHFULNESS IS MANDATORY: build only from the candidate's base CV. You may reorder, re-emphasize,
          and rephrase to match the job, but NEVER invent employers, titles, dates, degrees, or skills not present.
        - Cover letter: concise (3-5 short paragraphs), specific to the company/role, highlights the most relevant
          experience, professional tone. No placeholders like "[Your Name]" — use the candidate's real details from the CV.
        - tailoredCvMarkdown: a clean markdown CV reordered so the most job-relevant experience/skills come first.
        - The candidate is based in Turkey and works fully remote (open to B2B/contractor/EOR/employee-via-EOR).
          Do not claim local work authorization the candidate does not have.
        - Escape the strings properly so the whole response is valid JSON.
        """;

    private static string UserV1(JobPosting job, CriteriaProfile profile, string baseCvMarkdown)
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== JOB POSTING ===");
        sb.Append("Title: ").AppendLine(job.Title);
        sb.Append("Company: ").AppendLine(job.Company);
        sb.AppendLine("Description:");
        sb.AppendLine(Truncate(job.DescriptionText, MaxDescriptionChars));
        sb.AppendLine();
        sb.AppendLine("=== CANDIDATE CONTEXT ===");
        sb.Append("Residence country: ").AppendLine(profile.ResidenceCountry);
        sb.AppendLine("Engagement: fully remote, open to B2B / contractor / EOR / employee-via-EOR.");
        sb.AppendLine();
        sb.AppendLine("=== CANDIDATE BASE CV (markdown) ===");
        sb.AppendLine(Truncate(baseCvMarkdown, MaxCvChars));
        return sb.ToString();
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max];
}
