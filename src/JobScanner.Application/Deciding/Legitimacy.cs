using JobScanner.Domain.Eligibility;
using JobScanner.Domain.Enums;
using JobScanner.Domain.Jobs;

namespace JobScanner.Application.Deciding;

/// <summary>
/// Ghost-job / recruiter / uzun süredir açık ilan sinyallerini saf C#'ta toplar.
/// Karar (Decision) ile bağımsız: bir ilan Eligible olabilir ama Suspicious da olabilir.
/// </summary>
public sealed record LegitimacyResult(LegitimacyConfidence Confidence, IReadOnlyList<string> Signals);

public static class Legitimacy
{
    /// <summary>Uzun süredir feed'lerde dolaşan ilan = ghost şüphesi (gün eşiği).</summary>
    public const int StaleAfterDays = 60;

    /// <summary>LLM çok belirsiz çıktı verdiyse → sinyal değil ama 'caution' tetikleyici.</summary>
    public const double LowConfidenceThreshold = 0.5;

    public static LegitimacyResult Evaluate(EligibilityFacts facts, JobPosting job, DateTimeOffset now)
    {
        var signals = new List<string>();

        if (facts.IsLikelyGhost == true) signals.Add("ghost-language");
        if (facts.IsRecruiterAgency == true) signals.Add("recruiter-agency");
        if (facts.Confidence < LowConfidenceThreshold) signals.Add("low-llm-confidence");

        // "Aynı ilan kaynaklarda 60+ gündür dolaşıyor" — ghost/evergreen kuvvetli işareti
        var ageDays = (now - job.FirstSeenAt).TotalDays;
        if (ageDays >= StaleAfterDays) signals.Add($"long-running-{(int)ageDays}d");

        var confidence = signals.Count switch
        {
            0 => LegitimacyConfidence.High,
            1 => LegitimacyConfidence.Caution,
            _ => LegitimacyConfidence.Suspicious,
        };

        return new LegitimacyResult(confidence, signals);
    }
}
