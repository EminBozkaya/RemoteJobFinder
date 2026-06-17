using JobScanner.Application.Abstractions;
using JobScanner.Domain.Eligibility;
using JobScanner.Domain.Enums;
using JobScanner.Domain.Jobs;
using JobScanner.Domain.Users;

namespace JobScanner.Application.Scoring;

/// <summary>
/// Saf C# puanlama (PLAN §7): MatchPercent*5 + TimezoneFit(0-3) + Freshness(0-2),
/// 0-10'a clamp'lenir. Açıklanabilir döküm üretir. ML yok; ağırlıklı toplam.
/// </summary>
public sealed class ScoringEngine : IScoringEngine
{
    private const double HalfLifeDays = 7.0;
    private const double ResidenceUtcOffset = 3.0; // Türkiye, UTC+3
    private const double TitleWeight = 1.0;
    private const double BodyWeight = 0.6;

    private readonly TimeProvider _clock;

    public ScoringEngine(TimeProvider clock) => _clock = clock;

    public JobScore Score(JobPosting job, EligibilityFacts facts, CriteriaProfile profile)
    {
        var matchPercent = MatchPercent(job, profile, out var matchDetail);
        var timezoneFit = TimezoneFit(facts.TimezoneRequirementRaw, profile.TimezoneToleranceHours, out var tzDetail);
        var freshness = Freshness(job.PostedAt ?? job.FirstSeenAt);
        var engagementFit = EngagementFit(facts, out var engDetail);

        var raw = matchPercent * 5.0 + timezoneFit + freshness + engagementFit;
        var final = Math.Clamp(raw, 0, 10);

        var breakdown = new[]
        {
            new ScoreContribution($"MatchPercent ({matchDetail})", Math.Round(matchPercent * 5.0, 2)),
            new ScoreContribution($"TimezoneFit ({tzDetail})", Math.Round(timezoneFit, 2)),
            new ScoreContribution("Freshness", Math.Round(freshness, 2)),
            new ScoreContribution($"EngagementFit ({engDetail})", Math.Round(engagementFit, 2)),
        };

        return new JobScore(Math.Round(final, 2), breakdown);
    }

    /// <summary>
    /// 0-1: çalışma türü tercihi. Kullanıcının şirketi yok → EOR (TR'de yasal çalışan) en değerli;
    /// B2B/contractor uygun ama şirket/fatura masrafı gerektirir; diğerleri nötr.
    /// </summary>
    private static double EngagementFit(EligibilityFacts facts, out string detail)
    {
        if (facts.MentionsEor == true || facts.EngagementType == EngagementType.EmployeeViaEor)
        {
            detail = "EOR (ideal)";
            return 1.0;
        }
        if (facts.EngagementType is EngagementType.Contractor or EngagementType.B2B or EngagementType.Freelance)
        {
            detail = $"{facts.EngagementType} (şirket gerekir)";
            return 0.4;
        }
        detail = "nötr";
        return 0.0;
    }

    /// <summary>must/nice/negative keyword eşleşmesi (başlık ağırlıklı), [0,1].</summary>
    private static double MatchPercent(JobPosting job, CriteriaProfile profile, out string detail)
    {
        var title = job.Title;
        var body = job.DescriptionText;

        double requiredScore;
        if (profile.RequiredKeywords.Count == 0)
        {
            requiredScore = 1.0; // kısıt yoksa tam
        }
        else
        {
            var sum = profile.RequiredKeywords.Sum(k => KeywordWeight(k, title, body));
            requiredScore = sum / profile.RequiredKeywords.Count;
        }

        var niceBonus = profile.NiceKeywords.Count == 0
            ? 0.0
            : 0.3 * profile.NiceKeywords.Sum(k => KeywordWeight(k, title, body)) / profile.NiceKeywords.Count;

        var negativePenalty = profile.ForbiddenKeywords.Any(k => Contains(title, k) || Contains(body, k)) ? 0.5 : 0.0;

        var pct = Math.Clamp(requiredScore + niceBonus - negativePenalty, 0, 1);
        detail = $"req={requiredScore:0.00} nice=+{niceBonus:0.00} neg=-{negativePenalty:0.00}";
        return pct;
    }

    /// <summary>Başlıkta tam, gövdede kısmi kredi; bulunmazsa 0.</summary>
    private static double KeywordWeight(string keyword, string title, string body)
    {
        if (Contains(title, keyword)) return TitleWeight;
        if (Contains(body, keyword)) return BodyWeight;
        return 0.0;
    }

    /// <summary>0-3: ikamet (UTC+3) ofsetine fark, kullanıcı toleransına göre.</summary>
    private static double TimezoneFit(string? requirementRaw, int toleranceHours, out string detail)
    {
        if (string.IsNullOrWhiteSpace(requirementRaw))
        {
            detail = "kısıt yok";
            return 3.0;
        }

        var offset = TimezoneParser.TryParseUtcOffset(requirementRaw);
        if (offset is null)
        {
            detail = "çözülemedi";
            return 1.5; // gereksinim var ama belirsiz → nötr
        }

        var diff = Math.Abs(offset.Value - ResidenceUtcOffset);
        detail = $"Δ{diff:0.#}s, tol={toleranceHours}";
        if (diff <= toleranceHours) return 3.0;
        if (diff <= toleranceHours + 2) return 1.5;
        return 0.0;
    }

    /// <summary>0-2: ~7 günlük yarı-ömür.</summary>
    private double Freshness(DateTimeOffset postedAt)
    {
        var ageDays = Math.Max(0, (_clock.GetUtcNow() - postedAt).TotalDays);
        return 2.0 * Math.Pow(0.5, ageDays / HalfLifeDays);
    }

    private static bool Contains(string haystack, string needle) =>
        !string.IsNullOrWhiteSpace(needle) && haystack.Contains(needle, StringComparison.OrdinalIgnoreCase);
}
